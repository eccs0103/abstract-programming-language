using System.Globalization;

using static System.Math;
using static AbstractLanguageModel.Interpreter.Token;

namespace AbstractLanguageModel;

internal partial class Interpreter
{
	private class Locator
	{
		public Locator(in Token[] tokens, uint begin, uint end)
		{
			this.Tokens = tokens;
			this.Begin = begin;
			this.End = end;
			this.LocationChangeCallback += (position) =>
			{
				this.Position = position;
			};
		}
		private readonly Token[] Tokens;
		public readonly uint Begin;
		public readonly uint End;
		public Position Position { get; private set; } = new(0, 0);
		private event Action<Position> LocationChangeCallback;
		public Token? GetToken(uint index)
		{
			if (Max(this.Begin, 0) <= index && index < Min(Tokens.Length, this.End))
			{
				Token token = Tokens[index];
				LocationChangeCallback.Invoke(token.Position);
				return token;
			}
			else return null;
		}
		public Locator GetSublocator(uint begin, uint end)
		{
			Locator locator = new(this.Tokens, begin, end);
			locator.LocationChangeCallback += this.LocationChangeCallback.Invoke;
			return locator;
		}
	}
	public abstract class Node()
	{
	}
	private class ValueNode(double? value): Node()
	{
		public static readonly ValueNode Null = new(null);
		public double? Value { get; } = value;
		public override string ToString()
		{
			return $"{this.Value}";
		}
	}
	private class IdentifierNode(string name): Node()
	{
		public string Name { get; } = name;
		public override string ToString()
		{
			return $"{this.Name}";
		}
	}
	private class InvokationNode(IdentifierNode target, Node[] arguments): Node()
	{
		public IdentifierNode Target { get; } = target;
		public Node[] Arguments { get; } = arguments;
		public override string ToString()
		{
			return $"{this.Target}({string.Join<Node>(", ", this.Arguments)})";
		}
	}
	private abstract class OperatorNode(string @operator): Node()
	{
		public string Operator { get; } = @operator;
		public override string ToString()
		{
			return $"({this.Operator})";
		}
	}
	private class UnaryOperatorNode(string @operator, Node target): OperatorNode(@operator)
	{
		public Node Target { get; set; } = target;
		public override string ToString()
		{
			return $"{this.Operator}({this.Target})";
		}
	}
	private class BinaryOperatorNode(string @operator, Node left, Node right): OperatorNode(@operator)
	{
		public Node Left { get; set; } = left;
		public Node Right { get; set; } = right;
		public override string ToString()
		{
			return $"({this.Left} {this.Operator} {this.Right})";
		}
	}
	private static readonly Dictionary<string, string> Brackets = new()
	{
		{  @"(", @")" },
	};
	private Node InitialParse(in Locator locator, ref uint index)
	{
		return this.Degree1OperatorsParse(locator, ref index);
	}
	private Node Degree1OperatorsParse(in Locator locator, ref uint index)
	{
		Node nodeLeft = this.Degree2OperatorsParse(locator, ref index);
		while (locator.GetToken(index) is Token token)
		{
			if (token is Token { Type: Types.Operator, Value: ":" })
			{
				index++;
				Node nodeRight = this.Degree2OperatorsParse(locator, ref index);
				nodeLeft = new BinaryOperatorNode(token.Value, nodeLeft, nodeRight);
			}
			else break;
		}
		return nodeLeft;
	}
	private Node Degree2OperatorsParse(in Locator locator, ref uint index)
	{
		Node nodeLeft = this.Degree3OperatorsParse(locator, ref index);
		while (locator.GetToken(index) is Token token)
		{
			if (token is Token { Type: Types.Operator, Value: "+" or "-" })
			{
				index++;
				Node nodeRight = this.Degree3OperatorsParse(locator, ref index);
				nodeLeft = new BinaryOperatorNode(token.Value, nodeLeft, nodeRight);
			}
			else break;
		}
		return nodeLeft;
	}
	private Node Degree3OperatorsParse(in Locator locator, ref uint index)
	{
		Node nodeLeft = this.VerticesParse(locator, ref index);
		while (locator.GetToken(index) is Token token)
		{
			if (token is Token { Type: Types.Operator, Value: "*" or "/" })
			{
				index++;
				Node nodeRight = this.VerticesParse(locator, ref index);
				nodeLeft = new BinaryOperatorNode(token.Value, nodeLeft, nodeRight);
			}
			else break;
		}
		return nodeLeft;
	}
	private Node[] ArgumentsParse(in Locator locator, ref uint index)
	{
		List<Node> arguments = [];
		while (true)
		{
			arguments.Add(this.InitialParse(locator, ref index));

			Token? token = locator.GetToken(index);
			if (token is null)
			{
				break;
			}
			else if (token is { Type: Types.Separator, Value: "," })
			{
				index++;
				continue;
			}
			else throw new ArgumentException($"Expected ',' at {locator.Position}");
		}
		return [.. arguments];
	}
	private static Locator GetSublocator(in Locator locator, ref uint index, string bracket)
	{
		uint begin = index;
		if (Brackets.TryGetValue(bracket, out string? pair))
		{
			for (uint edge = locator.End - 1; edge > begin; edge--)
			{
				if (locator.GetToken(edge) is Token { Type: Types.Bracket } subtoken && subtoken.Value == pair)
				{
					index++;
					return locator.GetSublocator(begin + 1, edge);
				}
			}
			throw new ArgumentException($"Expected '{pair}' at {locator.Position}");
		}
		else throw new ArgumentException($"Unable to get pair of {bracket} at {locator.Position}");
	}
	private Node VerticesParse(in Locator locator, ref uint index)
	{
		if (locator.GetToken(index) is Token token)
		{
			switch (token.Type)
			{
			case Types.Number:
				{
					ValueNode nodeValue = new(Convert.ToDouble(token.Value, CultureInfo.GetCultureInfo("en-US")));
					index++;
					return nodeValue;
				}
			case Types.Identifier:
				{
					IdentifierNode nodeIdentifier = new(token.Value);
					index++;
					if (locator.GetToken(index) is Token { Type: Types.Bracket, Value: "(" } subtoken)
					{
						Node[] arguments = this.ArgumentsParse(GetSublocator(locator, ref index, subtoken.Value), ref index);
						index++;
						return new InvokationNode(nodeIdentifier, arguments);
					}
					return nodeIdentifier;
				}
			case Types.Keyword:
				{
					switch (token.Value)
					{
					case "data":
						{
							index++;
							Node nodeTarget = this.VerticesParse(locator, ref index);
							return new UnaryOperatorNode(token.Value, nodeTarget);
						}
					case "null":
						{
							index++;
							return ValueNode.Null;
						}
					default: throw new ArgumentException($"Unidentified keyword '{token.Value}' at {locator.Position}");
					}
				}
			case Types.Operator:
				{
					if (token.Value is "+" or "-")
					{
						index++;
						Node target = this.VerticesParse(locator, ref index);
						return new UnaryOperatorNode(token.Value, target);
					}
					else throw new ArgumentException($"Unidentified operator '{token.Value}' at {locator.Position}");
				}
			case Types.Bracket:
				{
					Node node = this.InitialParse(GetSublocator(locator, ref index, token.Value), ref index);
					index++;
					return node;
				}
			default: throw new ArgumentException($"Unidentified token '{token.Value}' at {locator.Position}");
			}
		}
		else throw new NullReferenceException($"Expected expression at {locator.Position}");
	}
	public IEnumerable<Node> Parse(in Token[] tokens)
	{
		List<Node> trees = [];
		Locator locator = new(tokens, 0, (uint) tokens.Length);
		for (uint index = 0; index < tokens.Length;)
		{
			Node nodeTree = this.InitialParse(locator, ref index);
			if (locator.GetToken(index) is Token { Type: Types.Separator, Value: ";" })
			{
				trees.Add(nodeTree);
				index++;
			}
			else throw new ArgumentException($"Expected ';' at {locator.Position}");
		}
		return trees;
	}
}
