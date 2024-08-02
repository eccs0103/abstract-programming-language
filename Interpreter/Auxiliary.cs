namespace APL;

internal partial class Interpreter
{
	public class Position(in uint column, in uint line)
	{
		public readonly uint Column = column;
		public readonly uint Line = line;
		public override string ToString()
		{
			return $"line {this.Line + 1} column {this.Column + 1}";
		}
	}
	private class MutablePosition(uint column, uint line): Position(column, line)
	{
		public MutablePosition(Position position) : this(position.Column, position.Line) { }
		public new uint Column = column;
		public new uint Line = line;
		public MutablePosition IncrementColumn()
		{
			this.Column++;
			return this;
		}
		public MutablePosition IncrementLine()
		{
			this.Line++;
			this.Column = 0;
			return this;
		}
		public MutablePosition IncrementBySymbol(char symbol)
		{
			return symbol == '\n' ? this.IncrementLine() : this.IncrementColumn();
		}
		public Position Seal()
		{
			return new(this.Column, this.Line);
		}
		public override string ToString()
		{
			return $"line {this.Line + 1} column {this.Column + 1}";
		}
	}
	public class Range<T>(in T begin, in T end)
	{
		public readonly T Begin = begin;
		public readonly T End = end;
		public override string ToString()
		{
			return $"from {this.Begin} to {this.End}";
		}
	}
	private class Wrapper<T>(T value)
	{
		public T Value = value;
	}
}

static class Extensions
{
	/*public static string GetDescription(this Types type)
	{
		return type switch
		{
			Types.Number => "Number",
			Types.Identifier => "Identifier",
			Types.Keyword => "qaq",
			Types.Operator => "Operator",
			Types.Bracket => "Bracket",
			Types.Separator => "Separator",
			_ => throw new ArgumentException($"Unidentified token '{type}' type")
		};
	}*/
}