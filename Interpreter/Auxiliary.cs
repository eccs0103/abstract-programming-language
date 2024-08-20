namespace APL;

internal partial class Interpreter(in Interpreter.Initializer initializer)
{
	private class Position(in uint column, in uint line)
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
	private class Range<T>(in T begin, in T end)
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
	private class Error(string message, Position position): Exception($"{message} at {position}")
	{
		public readonly Position Position = position;
	}

	public enum RunModes: byte
	{
		Debug,
		Run,
	}
	public readonly struct Initializer(in RunModes mode)
	{
		public readonly RunModes Mode = mode;
	}
	private RunModes Mode = initializer.Mode;
	public void Run(in string input)
	{
		ConsoleColor foreground = Console.ForegroundColor;
		try
		{
			Console.ForegroundColor = ConsoleColor.Cyan;
			Token[] tokens = this.Tokenize(input);
			if (this.Mode == RunModes.Debug && tokens.Length > 0) Console.WriteLine(string.Join<Token>('\n', tokens));

			Console.ForegroundColor = ConsoleColor.Blue;
			List<Node> trees = this.Parse(tokens);
			if (this.Mode == RunModes.Debug && tokens.Length > 0) Console.WriteLine(string.Join('\n', trees));

			Console.ForegroundColor = ConsoleColor.Yellow;
			this.Evaluate(trees);
		}
		catch (Error error)
		{
			Console.WriteLine(error.Message);
		}
		Console.ForegroundColor = foreground;
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
			_ => throw new Error($"Unidentified token '{type}' type")
		};
	}*/
}