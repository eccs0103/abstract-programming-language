using System.Text.RegularExpressions;

namespace ALM
{
	internal partial class Interpreter
	{
		public class Token(Token.Types type, String value, Position position)
		{
			public enum Types { Number, Identifier, Keyword, Operator, Brackets, Semicolon }
			public Types Type { get; } = type;
			public String Value { get; } = value;
			public Position Position { get; } = position;
			public override String ToString()
			{
				return $"{this.Type switch
				{
					Types.Number => "Number",
					Types.Identifier => "Identifier",
					Types.Keyword => "Keyword",
					Types.Operator => "Operator",
					Types.Brackets => "Brackets",
					Types.Semicolon => "Semicolon",
					_ => throw new ArgumentException($"Unidentified token '{this.Type}' type")
				}} '{this.Value}' {this.Position}";
			}
		}
		private static readonly Dictionary<Regex, Token.Types?> dictionary = new()
		{
			{ new Regex(@"^\s+", RegexOptions.Compiled), null },
			{ new Regex(@"^\d+(\.\d+)?", RegexOptions.Compiled), Token.Types.Number },
			{ new Regex(@"^(\+|-|\*|/|:)", RegexOptions.Compiled), Token.Types.Operator },
			{ new Regex(@"^[A-z]\w*", RegexOptions.Compiled), Token.Types.Identifier },
			{ new Regex(@"^[()]", RegexOptions.Compiled), Token.Types.Brackets },
			{ new Regex(@"^;", RegexOptions.Compiled), Token.Types.Semicolon },
		};
		private static readonly String[] keywords =
		{
			"data",
			"print",
			"null"
		};
		public Token[] Tokenize(in String code)
		{
			UInt32 line = 0, column = 0;
			List<Token> tokens = [];
			for (String text = code; text.Length > 0;)
			{
				Boolean hasChanges = false;
				foreach ((Regex regex, Token.Types? typified) in dictionary)
				{
					Match match = regex.Match(text);
					if (match.Success)
					{
						if (typified is Token.Types type)
						{
							if (type == Token.Types.Identifier && keywords.Contains(match.Value))
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
						else column += (UInt32) match.Length;
						hasChanges = true;
						text = text[(match.Index + match.Length)..];
						break;
					}
				}
				if (!hasChanges) throw new FormatException($"Unidentified term '{text.First()}'");
			}
			return [.. tokens];
		}
	}
}
