using System.Globalization;
using System.Text.RegularExpressions;

namespace AdaptiveCore
{
	namespace ANL
	{
		public class Interpreter
		{
			#region Tokenizer
			private class Token
			{
				public enum Types { Number, Identifier, Operator, Brackets }
				public Token(Types type, string value, int line, int column)
				{
					this.Type = type;
					this.Value = value;
					this.Line = line;
					this.Column = column;
				}
				public Types Type { get; }
				public string Value { get; }
				public int Line { get; }
				public int Column { get; }
				public override string ToString()
				{
					return $"{this.Type switch
					{
						Types.Number => "Number",
						Types.Identifier => "Identifier",
						Types.Operator => "Operator",
						Types.Brackets => "Brackets",
						_ => throw new ArgumentException($"Invalid token {this.Type} type")
					}} '{this.Value}' at line {this.Line + 1} column {this.Column + 1}";
				}
			}
			private static readonly Dictionary<Regex, Token.Types?> dictionary = new()
			{
				{ new Regex(@"^\s+", RegexOptions.Compiled), null },
				{ new Regex(@"^\d+(\.\d+)?", RegexOptions.Compiled), Token.Types.Number },
				{ new Regex(@"^[+\-*/]", RegexOptions.Compiled), Token.Types.Operator },
				{ new Regex(@"^[A-z]\w*", RegexOptions.Compiled), Token.Types.Identifier },
				{ new Regex(@"^[()]", RegexOptions.Compiled), Token.Types.Brackets },
			};
			private Token[] Tokenize(in string code)
			{
				int line = 0, column = 0;
				List<Token> tokens = new();
				for (string text = code; text.Length > 0;)
				{
					bool hasChanges = false;
					foreach ((Regex regex, Token.Types? type) in dictionary)
					{
						Match match = regex.Match(text);
						if (match.Success)
						{
							if (type != null)
							{
								tokens.Add(new Token(type.GetValueOrDefault(), match.Value, line, column));
							}
							if (match.Value == "\n")
							{
								line++;
								column = 0;
							}
							else
							{
								column += match.Length;
							}
							hasChanges = true;
							text = text[(match.Index + match.Length)..];
							break;
						}
					}
					if (!hasChanges) throw new FormatException($"Invalid '{text[0]}' term");
				}
				return tokens.ToArray();
			}
			#endregion
			#region Parser
			private abstract class Node { }
			private class DataNode: Node
			{
				public static readonly DataNode Empty = new(double.NaN);
				public DataNode(double value) : base()
				{
					this.Value = value;
				}
				public double Value { get; }
				public override string ToString() => $"{this.Value}";
			}
			private class ReferenceNode: Node
			{
				public ReferenceNode(string path) : base()
				{
					this.Path = path;
				}
				public string Path { get; }
				public override string ToString() => $"{this.Path}";
			}
			private abstract class OperatorNode: Node
			{
				public OperatorNode(string @operator) : base()
				{
					this.Operator = @operator;
				}
				public string Operator { get; }
				public override string ToString() => $"({this.Operator})";
			}
			private class BinaryOperatorNode: OperatorNode
			{
				public BinaryOperatorNode(string @operator, Node left, Node right) : base(@operator)
				{
					this.Left = left;
					this.Right = right;
				}
				public Node Left { get; set; }
				public Node Right { get; set; }
				public override string ToString() => $"({this.Left} {this.Operator} {this.Right})";
			}
			private static readonly Dictionary<string, string> brackets = new()
			{
				{  @"(", @")" },
			};
			private Node Parse(in Token[] tokens)
			{
				static Node Parse1(in Token[] tokens, ref int index)
				{
					Node left = Parse2(tokens, ref index);
					while (index < tokens.Length)
					{
						Token token = tokens[index];
						if (token.Type == Token.Types.Operator && (token.Value == "+" || token.Value == "-"))
						{
							index++;
							Node right = Parse2(tokens, ref index);
							left = new BinaryOperatorNode(token.Value, left, right);
						}
						else break;
					}
					return left;
				}
				static Node Parse2(in Token[] tokens, ref int index)
				{
					Node left = Parse3(tokens, ref index);
					while (index < tokens.Length)
					{
						Token token = tokens[index];
						if (token.Type == Token.Types.Operator && (token.Value == "*" || token.Value == "/"))
						{
							index++;
							Node right = Parse3(tokens, ref index);
							left = new BinaryOperatorNode(token.Value, left, right);
						}
						else break;
					}
					return left;
				}
				static Node Parse3(in Token[] tokens, ref int index)
				{
					if (index < tokens.Length)
					{
						Token token = tokens[index];
						if (token.Type == Token.Types.Number)
						{
							double value = Convert.ToDouble(token.Value, CultureInfo.GetCultureInfo("en-US"));
							Node node = new DataNode(value);
							index++;
							return node;
						}
						else if (token.Type == Token.Types.Identifier)
						{
							string value = token.Value;
							Node node = new ReferenceNode(value);
							index++;
							return node;
						}
						else if (token.Type == Token.Types.Brackets)
						{
							if (brackets.TryGetValue(token.Value, out string? pair))
							{
								for (int index2 = index + 1; index2 < tokens.Length; index2++)
								{
									if (tokens[index2].Value == pair)
									{
										index++;
										Node node = Parse1(new ArraySegment<Token>(tokens, 0, index2).ToArray(), ref index);
										index++;
										return node;
									}
								}
								throw new ArgumentException($"Expected '{pair}'");
							}
							else throw new ArgumentException($"Invalid {token}");
						}
						else throw new ArgumentException($"Invalid {token}");
					}
					else return DataNode.Empty;
				}

				int index = 0;
				return Parse1(tokens, ref index);
			}
			#endregion
			#region Evalutor
			private readonly Dictionary<string, double> memory = new()
			{
				{ "Pi", Math.PI },
				{ "E", Math.E },
			};
			private double Evaluate(in Node node)
			{
				if (node is DataNode nodeData)
				{
					return nodeData.Value;
				}
				else if (node is ReferenceNode nodeReference)
				{
					return this.memory.TryGetValue(nodeReference.Path, out double value)
						? value
						: throw new NullReferenceException($"Identifier '{nodeReference.Path}' does not exist");
				}
				else if (node is BinaryOperatorNode nodeBinaryOperator)
				{
					double left = this.Evaluate(nodeBinaryOperator.Left);// ?? throw new NullReferenceException("Left value is not defined.");
					double right = this.Evaluate(nodeBinaryOperator.Right);// ?? throw new NullReferenceException("Left value is not defined.");
					return nodeBinaryOperator.Operator switch
					{
						"+" => left + right,
						"-" => left - right,
						"*" => left * right,
						"/" => left / right,
						_ => throw new ArgumentException($"Invalid {nodeBinaryOperator.Operator} operator"),
					};
				}
				else throw new ArgumentException($"Invalid node {node.GetType()} type");
			}
			public double Evaluate(in string code) => this.Evaluate(this.Parse(this.Tokenize(code)));
			#endregion
		}
	}
}