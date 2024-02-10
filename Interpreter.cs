namespace ALM
{
	internal partial class Interpreter
	{
		public class Position(UInt32 line, UInt32 column)
		{
			public UInt32 Line { get; } = line;
			public UInt32 Column { get; } = column;
			public override String ToString()
			{
				return $"line {this.Line + 1} column {this.Column + 1}";
			}
		}
	}
}