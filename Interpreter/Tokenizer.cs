using System.Text;
using System.Text.RegularExpressions;

namespace APL;

internal partial class Interpreter
{
	public class Token(in Token.Types type, in string value, in Range<Position> range)
	{
		public enum Types
		{
			Number,
			String,
			Identifier,
			Keyword,
			Operator,
			Bracket,
			Separator,
		}
		public readonly Types Type = type;
		public readonly string Value = value;
		public readonly Range<Position> RangePosition = range;
		public override string ToString()
		{
			return $"{this.Type} '{this.Value}' at {this.RangePosition.Begin}";
		}
		public bool Match(params string[] values)
		{
			return values.Any(value => value == this.Value);
		}
		public bool Match(in Types type, params string[] values)
		{
			return type == this.Type && this.Match(values);
		}
	}

	private static readonly Dictionary<Regex, Token.Types?> Dictionary = new()
	{
		{ StringPattern(), Token.Types.String },
		{ WhitespacePattern(), null },
		{ NumberPattern(), Token.Types.Number },
		{ OperatorPattern(), Token.Types.Operator },
		{ IdentifierPattern(), Token.Types.Identifier },
		{ BracketsPattern(), Token.Types.Bracket },
		{ SeparatorPattern(), Token.Types.Separator },
	};
	private static readonly HashSet<string> Keywords = ["data", "null", "import"];

	[GeneratedRegex(@"^\s+", RegexOptions.Compiled)]
	private static partial Regex WhitespacePattern();
	[GeneratedRegex(@"^\d+(\.\d+)?", RegexOptions.Compiled)]
	private static partial Regex NumberPattern();
	[GeneratedRegex(@"^""(.)*?(?<!\\)""", RegexOptions.Compiled)]
	private static partial Regex StringPattern();
	[GeneratedRegex(@"^(\+|-|\*|/|:)", RegexOptions.Compiled)]
	private static partial Regex OperatorPattern();
	[GeneratedRegex(@"^[A-z]\w*", RegexOptions.Compiled)]
	private static partial Regex IdentifierPattern();
	[GeneratedRegex(@"^[()]", RegexOptions.Compiled)]
	private static partial Regex BracketsPattern();
	[GeneratedRegex(@"^[;,]", RegexOptions.Compiled)]
	private static partial Regex SeparatorPattern();

	public static Token[] Tokenize(in string code)
	{
		Position begin = new(0, 0);
		List<Token> tokens = [];
		for (StringBuilder text = new(code); text.Length > 0;)
		{
			bool hasChanges = false;
			foreach ((Regex regex, Token.Types? unknown) in Dictionary)
			{
				Match match = regex.Match(text.ToString());
				if (!match.Success) continue;
				text.Remove(0, match.Length);

				Position end = match.Value.Aggregate(new MutablePosition(begin), (position, symbol) => position.IncrementBySymbol(symbol)).Seal();
				if (unknown is Token.Types type)
				{
					if (type == Token.Types.Identifier && Keywords.Contains(match.Value))
					{
						type = Token.Types.Keyword;
					}
					tokens.Add(new Token(type, match.Value, new(begin, end)));
				}
				begin = end;

				hasChanges = true;
				break;
			}
			if (!hasChanges) throw new FormatException($"Unidentified term '{text[0]}'");
		}
		return [.. tokens];
	}
}
