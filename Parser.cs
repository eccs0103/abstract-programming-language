using System.Globalization;

using static System.Math;

namespace ALM
{
	internal partial class Interpreter
	{
		private class Locator
		{
			public Locator(in Token[] tokens, UInt32 begin, UInt32 end)
			{
				this.tokens = tokens;
				this.Begin = begin;
				this.End = end;
				LocationChangeCallback += (position) =>
				{
					this.Position = position;
				};
			}
			private readonly Token[] tokens;
			public readonly UInt32 Begin;
			public readonly UInt32 End;
			public Position Position { get; private set; } = new(0, 0);
			private event Action<Position> LocationChangeCallback;
			public Token? GetToken(UInt32 index)
			{
				if (Max(this.Begin, 0) <= index && index < Min(tokens.Length, this.End))
				{
					Token token = tokens[index];
					LocationChangeCallback.Invoke(token.Position);
					return token;
				}
				else return null;
			}
			public Locator GetSublocator(UInt32 begin, UInt32 end)
			{
				Locator locator = new(this.tokens, begin, end);
				locator.LocationChangeCallback += this.LocationChangeCallback.Invoke;
				return locator;
			}
		}
		public abstract class Node()
		{
		}
		private class ValueNode(Double? value): Node()
		{
			public static readonly ValueNode Null = new(null);
			public Double? Value { get; } = value;
			public override String ToString()
			{
				return $"{this.Value}";
			}
		}
		private class IdentifierNode(String address): Node()
		{
			public String Address { get; } = address;
			public override String ToString()
			{
				return $"{this.Address}";
			}
		}
		private abstract class OperatorNode(String @operator): Node()
		{
			public String Operator { get; } = @operator;
			public override String ToString()
			{
				return $"({this.Operator})";
			}
		}
		private class UnaryOperatorNode(String @operator, Node target): OperatorNode(@operator)
		{
			public Node Target { get; set; } = target;
			public override String ToString()
			{
				return $"{this.Operator}({this.Target})";
			}
		}
		private class BinaryOperatorNode(String @operator, Node left, Node right): OperatorNode(@operator)
		{
			public Node Left { get; set; } = left;
			public Node Right { get; set; } = right;
			public override String ToString()
			{
				return $"({this.Left} {this.Operator} {this.Right})";
			}
		}
		private static readonly Dictionary<String, String> brackets = new()
		{
			{  @"(", @")" },
		};
		private Node InitialParse(in Locator locator, ref UInt32 index)
		{
			return this.OperatorsGroup1Parse(locator, ref index);
		}
		private Node OperatorsGroup1Parse(in Locator locator, ref UInt32 index)
		{
			Node left = this.OperatorsGroup2Parse(locator, ref index);
			while (locator.GetToken(index) is Token token)
			{
				if (token is Token { Type: Token.Types.Operator, Value: ":" })
				{
					index++;
					Node right = this.OperatorsGroup2Parse(locator, ref index);
					left = new BinaryOperatorNode(token.Value, left, right);
				}
				else break;
			}
			return left;
		}
		private Node OperatorsGroup2Parse(in Locator locator, ref UInt32 index)
		{
			Node left = this.OperatorsGroup3Parse(locator, ref index);
			while (locator.GetToken(index) is Token token)
			{
				if (token is Token { Type: Token.Types.Operator, Value: "+" or "-" })
				{
					index++;
					Node right = this.OperatorsGroup3Parse(locator, ref index);
					left = new BinaryOperatorNode(token.Value, left, right);
				}
				else break;
			}
			return left;
		}
		private Node OperatorsGroup3Parse(in Locator locator, ref UInt32 index)
		{
			Node left = this.VerticesParse(locator, ref index);
			while (locator.GetToken(index) is Token token)
			{
				if (token is Token { Type: Token.Types.Operator, Value: "*" or "/" })
				{
					index++;
					Node right = this.VerticesParse(locator, ref index);
					left = new BinaryOperatorNode(token.Value, left, right);
				}
				else break;
			}
			return left;
		}
		private Node BracketsParse(in Locator locator, ref UInt32 index)
		{
			if (locator.GetToken(index) is Token { Type: Token.Types.Brackets } token)
			{
				UInt32 begin = index;
				if (brackets.TryGetValue(token.Value, out String? pair))
				{
					for (UInt32 edge = locator.End - 1; edge > begin; edge--)
					{
						if (locator.GetToken(edge) is Token subtoken && subtoken.Value == pair)
						{
							index++;
							Node node = this.InitialParse(locator.GetSublocator(begin + 1, edge), ref index);
							index++;
							return node;
						}
					}
					throw new ArgumentException($"Expected '{pair}' at {locator.Position}");
				}
				else throw new ArgumentException($"Unable to get pair of {token.Value} at {locator.Position}");
			}
			else throw new NullReferenceException($"Expected bracket at {locator.Position}");
		}
		private Node VerticesParse(in Locator locator, ref UInt32 index)
		{
			if (locator.GetToken(index) is Token token)
			{
				switch (token.Type)
				{
				case Token.Types.Number:
					{
						Node node = new ValueNode(Convert.ToDouble(token.Value, CultureInfo.GetCultureInfo("en-US")));
						index++;
						return node;
					}
				case Token.Types.Identifier:
					{
						Node node = new IdentifierNode(token.Value);
						index++;
						return node;
					}
				case Token.Types.Keyword:
					{
						switch (token.Value)
						{
						case "data":
							{
								index++;
								Node target = this.VerticesParse(locator, ref index);
								return new UnaryOperatorNode(token.Value, target);
							}
						case "print":
							{
								index++;
								Node target = this.BracketsParse(locator, ref index);
								return new UnaryOperatorNode(token.Value, target);
							}
						case "null":
							{
								index++;
								return ValueNode.Null;
							}
						default: throw new ArgumentException($"Unidentified keyword '{token.Value}' at {locator.Position}");
						}
					}
				case Token.Types.Operator:
					{
						if (token.Value is "+" or "-")
						{
							index++;
							Node target = this.VerticesParse(locator, ref index);
							return new UnaryOperatorNode(token.Value, target);
						}
						else throw new ArgumentException($"Unidentified operator '{token.Value}' at {locator.Position}");
					}
				case Token.Types.Brackets: return this.BracketsParse(locator, ref index);
				default: throw new ArgumentException($"Unidentified token '{token.Value}' at {locator.Position}");
				}
			}
			else throw new NullReferenceException($"Expected expression at {locator.Position}");
		}
		public Node[] Parse(in Token[] tokens)
		{
			List<Node> trees = [];
			Locator locator = new(tokens, 0, (UInt32) tokens.Length);
			for (UInt32 index = 0; index < tokens.Length;)
			{
				Node tree = this.InitialParse(locator, ref index);
				if (locator.GetToken(index) is Token { Type: Token.Types.Semicolon, Value: ";" })
				{
					trees.Add(tree);
					index++;
				}
				else throw new ArgumentException($"Expected ';' at {locator.Position}");
			}
			return [.. trees];
		}
	}
}
