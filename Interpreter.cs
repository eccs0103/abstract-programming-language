namespace AbstractLanguageModel;

internal partial class Interpreter
{
	public class Position(uint line, uint column)
	{
		public uint Line { get; } = line;
		public uint Column { get; } = column;
		public override string ToString()
		{
			return $"line {this.Line + 1} column {this.Column + 1}";
		}
	}
}