using System.Globalization;

using static System.Math;
using static APL.Interpreter.Token;

namespace APL;

internal partial class Interpreter
{
	private class Walker(in Token[] tokens, in Range<uint> range)
	{
		private static readonly Token DefaultToken = new(Types.Separator, string.Empty, new(new(0, 0), new(0, 0)));
		private readonly Token[] Tokens = tokens;
		public readonly Range<uint> RangeIndex = range;
		private Wrapper<uint> IndexWrapper = new(0);
		public uint Index
		{
			get => this.IndexWrapper.Value;
			set => this.IndexWrapper.Value = value;
		}
		public bool InRange => Max(this.RangeIndex.Begin, 0) <= this.Index && this.Index < Min(this.Tokens.Length, this.RangeIndex.End);
		public Token Token => this.Tokens[this.Index];
		public readonly Range<Position> RangePosition = new(tokens.FirstOrDefault(DefaultToken).RangePosition.Begin, tokens.LastOrDefault(DefaultToken).RangePosition.End);
		public Walker GetSubwalker(uint begin, uint end)
		{
			return new(this.Tokens, new(begin, end))
			{
				IndexWrapper = this.IndexWrapper
			};
		}
	}

	public abstract partial class Node(in Range<Position> range)
	{
		public readonly Range<Position> RangePosition = range;
	}
	private partial class ValueNode(in double? value, in Range<Position> range): Node(range)
	{
		public readonly double? Value = value;
		public override string ToString()
		{
			return $"{this.Value}";
		}
	}
	private partial class IdentifierNode(in string name, in Range<Position> range): Node(range)
	{
		public readonly string Name = name;
		public override string ToString()
		{
			return $"{this.Name}";
		}
	}
	private partial class InvokationNode(in IdentifierNode target, in Node[] arguments, in Range<Position> range): Node(range)
	{
		public readonly IdentifierNode Target = target;
		public readonly Node[] Arguments = arguments;
		public override string ToString()
		{
			return $"{this.Target}({string.Join<Node>(", ", this.Arguments)})";
		}
	}
	private abstract partial class OperatorNode(in string @operator, in Range<Position> range): Node(range)
	{
		public readonly string Operator = @operator;
		public override string ToString()
		{
			return $"({this.Operator})";
		}
	}
	private partial class UnaryOperatorNode(in string @operator, in Node target, in Range<Position> range): OperatorNode(@operator, range)
	{
		public readonly Node Target = target;
		public override string ToString()
		{
			return $"{this.Operator}({this.Target})";
		}
	}
	private partial class BinaryOperatorNode(in string @operator, in Node left, in Node right, in Range<Position> range): OperatorNode(@operator, range)
	{
		public readonly Node Left = left;
		public readonly Node Right = right;
		public override string ToString()
		{
			return $"({this.Left} {this.Operator} {this.Right})";
		}
	}

	private static readonly Dictionary<string, string> Brackets = new()
	{
		{  @"(", @")" },
	};

	private Node Degree1OperatorsParse(in Walker walker)
	{
		Node left = this.Degree2OperatorsParse(walker);
		while (walker.InRange)
		{
			Token token = walker.Token;
			if (!token.Match(Types.Operator, ":")) break;
			walker.Index++;
			Node right = this.Degree2OperatorsParse(walker);
			left = new BinaryOperatorNode(token.Value, left, right, new(left.RangePosition.Begin, right.RangePosition.End));
		}
		return left;
	}
	private Node Degree2OperatorsParse(in Walker walker)
	{
		Node left = this.Degree3OperatorsParse(walker);
		while (walker.InRange)
		{
			Token token = walker.Token;
			if (!token.Match(Types.Operator, "+", "-")) break;
			walker.Index++;
			Node right = this.Degree3OperatorsParse(walker);
			left = new BinaryOperatorNode(token.Value, left, right, new(left.RangePosition.Begin, right.RangePosition.End));
		}
		return left;
	}
	private Node Degree3OperatorsParse(in Walker walker)
	{
		Node left = this.VerticesParse(walker);
		while (walker.InRange)
		{
			Token token = walker.Token;
			if (!token.Match(Types.Operator, "*", "/")) break;
			walker.Index++;
			Node right = this.VerticesParse(walker);
			left = new BinaryOperatorNode(token.Value, left, right, new(left.RangePosition.Begin, right.RangePosition.End));
		}
		return left;
	}
	private Node[] ArgumentsParse(in Walker walker)
	{
		List<Node> arguments = [];
		while (true)
		{
			arguments.Add(this.Degree1OperatorsParse(walker));

			if (!walker.InRange) break;
			Token token = walker.Token;
			if (!token.Match(Types.Separator, ",")) throw new ArgumentException($"Expected ',' at {token.RangePosition.Begin}");
			walker.Index++;
		}
		return [.. arguments];
	}
	private static Walker GetSubwalker(in Walker walker, in string bracket)
	{
		if (!Brackets.TryGetValue(bracket, out string? pair)) throw new ArgumentException($"Unable to get pair of {bracket} at {walker.RangePosition.Begin}");
		uint counter = 1;
		uint begin = walker.Index + 1;
		for (walker.Index++; walker.Index < walker.RangeIndex.End; walker.Index++)
		{
			if (!walker.InRange) continue;
			Token token = walker.Token;
			if (token.Match(Types.Bracket, bracket)) counter++;
			else if (token.Match(Types.Bracket, pair)) counter--;
			if (counter != 0) continue;
			uint end = walker.Index;
			walker.Index = begin;
			return walker.GetSubwalker(begin, end);
		}
		throw new ArgumentException($"Expected '{pair}' at {walker.RangePosition.End}");
	}
	private Node VerticesParse(in Walker walker)
	{
		if (!walker.InRange) throw new NullReferenceException($"Expected expression at {walker.RangePosition.Begin}");
		Token token = walker.Token;
		switch (token.Type)
		{
		case Types.Number:
		{
			ValueNode value = new(Convert.ToDouble(token.Value, CultureInfo.GetCultureInfo("en-US")), token.RangePosition);
			walker.Index++;
			return value;
		}
		case Types.Identifier:
		{
			IdentifierNode identifier = new(token.Value, token.RangePosition);
			walker.Index++;
			if (!walker.InRange) return identifier;
			Token subtoken = walker.Token;
			if (!subtoken.Match(Types.Bracket, "(")) return identifier;
			Node[] arguments = this.ArgumentsParse(GetSubwalker(walker, subtoken.Value));
			walker.Index++;
			return new InvokationNode(identifier, arguments, new(identifier.RangePosition.Begin, arguments.LastOrDefault(identifier).RangePosition.End));
		}
		case Types.Keyword:
		{
			switch (token.Value)
			{
			case "data":
			{
				walker.Index++;
				Node target = this.VerticesParse(walker);
				return new UnaryOperatorNode(token.Value, target, new(token.RangePosition.Begin, target.RangePosition.End));
			}
			case "null":
			{
				walker.Index++;
				return new ValueNode(null, token.RangePosition);
			}
			default: throw new ArgumentException($"Unidentified keyword '{token.Value}' at {token.RangePosition.Begin}");
			}
		}
		case Types.Operator:
		{
			if (token.Match("+", "-"))
			{
				walker.Index++;
				Node target = this.VerticesParse(walker);
				return new UnaryOperatorNode(token.Value, target, new(token.RangePosition.Begin, target.RangePosition.End));
			}
			else throw new ArgumentException($"Unidentified operator '{token.Value}' at {token.RangePosition.Begin}");
		}
		case Types.Bracket:
		{
			Node node = this.Degree1OperatorsParse(GetSubwalker(walker, token.Value));
			walker.Index++;
			return node;
		}
		default: throw new ArgumentException($"Unidentified token '{token.Value}' at {token.RangePosition.Begin}");
		}
	}
	public IEnumerable<Node> Parse(in Token[] tokens)
	{
		List<Node> trees = [];
		Walker walker = new(tokens, new(0, Convert.ToUInt32(tokens.Length)));
		while (walker.Index < tokens.Length)
		{
			Node tree = this.Degree1OperatorsParse(walker);
			if (!walker.InRange || !walker.Token.Match(Types.Separator, ";")) throw new ArgumentException($"Expected ';' at {walker.RangePosition.End}");
			trees.Add(tree);
			walker.Index++;
		}
		return trees;
	}
}
