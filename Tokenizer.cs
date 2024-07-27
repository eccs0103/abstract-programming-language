using System.Text.RegularExpressions;

namespace AbstractLanguageModel;

internal partial class Interpreter
{
	public class Token(Token.Types type, string value, Position position)
	{
		public enum Types
		{
			Number,
			Identifier,
			Keyword,
			Operator,
			Bracket,
			Separator
		}
		public Types Type { get; } = type;
		public string Value { get; } = value;
		public Position Position { get; } = position;
		public override string ToString()
		{
			return $"{this.Type switch
			{
				Types.Number => "Number",
				Types.Identifier => "Identifier",
				Types.Keyword => "Keyword",
				Types.Operator => "Operator",
				Types.Bracket => "Bracket",
				Types.Separator => "Separator",
				_ => throw new ArgumentException($"Unidentified token '{this.Type}' type")
			}} '{this.Value}' {this.Position}";
		}
	}
	private static readonly Dictionary<Regex, Token.Types?> Dictionary = new()
	{
		{ WhitespacePattern(), null },
		{ NumberPattern(), Token.Types.Number },
		{ OperatorPattern(), Token.Types.Operator },
		{ IdentifierPattern(), Token.Types.Identifier },
		{ BracketsPattern(), Token.Types.Bracket },
		{ SeparatorPattern(), Token.Types.Separator },
	};
	private static readonly string[] Keywords =
	{
		"data",
		"null"
	};
	public static Token[] Tokenize(in string code)
	{
		uint line = 0, column = 0;
		List<Token> tokens = [];
		for (string text = code; text.Length > 0;)
		{
			bool hasChanges = false;
			foreach ((Regex regex, Token.Types? typified) in Dictionary)
			{
				Match match = regex.Match(text);
				if (match.Success)
				{
					if (typified is Token.Types type)
					{
						if (type == Token.Types.Identifier && Keywords.Contains(match.Value))
						{
							type = Token.Types.Keyword;
						}
						tokens.Add(new Token(type, match.Value, new(line, column)));
					}
					if (match.Value == "\n")
					{
						line++;
						column = 0;
					}
					else
					{
						column += (uint) match.Length;
					}
					hasChanges = true;
					text = text[(match.Index + match.Length)..];
					break;
				}
			}
			if (!hasChanges) throw new FormatException($"Unidentified term '{text.First()}'");
		}
		return [.. tokens];
	}

	[GeneratedRegex(@"^\s+", RegexOptions.Compiled)]
	private static partial Regex WhitespacePattern();

	[GeneratedRegex(@"^\d+(\.\d+)?", RegexOptions.Compiled)]
	private static partial Regex NumberPattern();

	[GeneratedRegex(@"^(\+|-|\*|/|:)", RegexOptions.Compiled)]
	private static partial Regex OperatorPattern();

	[GeneratedRegex(@"^[A-z]\w*", RegexOptions.Compiled)]
	private static partial Regex IdentifierPattern();

	[GeneratedRegex(@"^[()]", RegexOptions.Compiled)]
	private static partial Regex BracketsPattern();

	[GeneratedRegex(@"^[;,]", RegexOptions.Compiled)]
	private static partial Regex SeparatorPattern();
}
