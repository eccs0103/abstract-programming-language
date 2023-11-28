using System;
using System.Globalization;
using System.Text.RegularExpressions;

using static System.Math;

namespace AdaptiveCore
{
	namespace ANL
	{
		public class Interpreter
		{
			#region Tokenizer
			public class Token
			{
				public enum Types { Number, Identifier, Keyword, Operator, Brackets, Semicolon }
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
						Types.Keyword => "Keyword",
						Types.Operator => "Operator",
						Types.Brackets => "Brackets",
						Types.Semicolon => "Semicolon",
						_ => throw new ArgumentException($"Invalid token {this.Type} type")
					}} '{this.Value}' at line {this.Line + 1} column {this.Column + 1}";
				}
			}
			private static readonly Dictionary<Regex, Token.Types?> dictionary = new()
			{
				{ new Regex(@"^\s+", RegexOptions.Compiled), null },
				{ new Regex(@"^\d+(\.\d+)?", RegexOptions.Compiled), Token.Types.Number },
				{ new Regex(@"^[+\-*/:]", RegexOptions.Compiled), Token.Types.Operator },
				{ new Regex(@"^[A-z]\w*", RegexOptions.Compiled), Token.Types.Identifier },
				{ new Regex(@"^[()]", RegexOptions.Compiled), Token.Types.Brackets },
				{ new Regex(@"^;", RegexOptions.Compiled), Token.Types.Semicolon },
			};
			private static readonly string[] keywords =
			{
				"data",
				"print"
			};
			public Token[] Tokenize(in string code)
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
								tokens.Add(new Token
								(
									type == Token.Types.Identifier && keywords.Contains(match.Value)
										? Token.Types.Keyword
										: type.GetValueOrDefault()
								, match.Value, line, column));
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
			public abstract class Node { }
			private class DataNode: Node
			{
				public static readonly DataNode Null = new(null);
				public DataNode(double? value) : base()
				{
					this.Value = value;
				}
				public double? Value { get; }
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
			private class UnaryOperatorNode: OperatorNode
			{
				public UnaryOperatorNode(string @operator, Node target) : base(@operator)
				{
					this.Target = target;
				}
				public Node Target { get; set; }
				public override string ToString() => $"{this.Operator}({this.Target})";
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
			public Node[] Parse(in Token[] tokens)
			{
				List<Node> trees = new();

				Node Parse0(in Token[] tokens, ref int begin, int end)
				{
					Node left = Parse1(tokens, ref begin, end);
					while (begin < Min(tokens.Length, end))
					{
						Token token = tokens[begin];
						if (token.Type == Token.Types.Operator && token.Value == ":")
						{
							begin++;
							Node right = Parse1(tokens, ref begin, end);
							left = new BinaryOperatorNode(token.Value, left, right);
						}
						else break;
					}
					return left;
				}
				Node Parse1(in Token[] tokens, ref int begin, int end)
				{
					Node left = Parse2(tokens, ref begin, end);
					while (begin < Min(tokens.Length, end))
					{
						Token token = tokens[begin];
						if (token.Type == Token.Types.Operator && (token.Value == "+" || token.Value == "-"))
						{
							begin++;
							Node right = Parse2(tokens, ref begin, end);
							left = new BinaryOperatorNode(token.Value, left, right);
						}
						else break;
					}
					return left;
				}
				Node Parse2(in Token[] tokens, ref int begin, int end)
				{
					Node left = Parse3(tokens, ref begin, end);
					while (begin < Min(tokens.Length, end))
					{
						Token token = tokens[begin];
						if (token.Type == Token.Types.Operator && (token.Value == "*" || token.Value == "/"))
						{
							begin++;
							Node right = Parse3(tokens, ref begin, end);
							left = new BinaryOperatorNode(token.Value, left, right);
						}
						else break;
					}
					return left;
				}
				Node Parse3(in Token[] tokens, ref int begin, int end)
				{
					if (begin < Min(tokens.Length, end))
					{
						Token token = tokens[begin];
						if (token.Type == Token.Types.Number)
						{
							double value = Convert.ToDouble(token.Value, CultureInfo.GetCultureInfo("en-US"));
							Node node = new DataNode(value);
							begin++;
							return node;
						}
						else if (token.Type == Token.Types.Identifier)
						{
							string value = token.Value;
							Node node = new ReferenceNode(value);
							begin++;
							return node;
						}
						else if (token.Type == Token.Types.Keyword)
						{
							switch (token.Value)
							{
								case "data":
								{
									begin++;
									Node target = Parse3(tokens, ref begin, end);
									return new UnaryOperatorNode(token.Value, target);
								}
								case "print":
								{
									begin++;
									Node target = Parse3(tokens, ref begin, end);
									return new UnaryOperatorNode(token.Value, target);
								}
								default: throw new ArgumentException($"Invalid keyword: {token}");
							}
						}
						else if (token.Type == Token.Types.Operator)
						{
							if (token.Value == "+" || token.Value == "-")
							{
								begin++;
								Node target = Parse3(tokens, ref begin, end);
								return new UnaryOperatorNode(token.Value, target);
							}
							else throw new ArgumentException($"Invalid operator: {token}");
						}
						else if (token.Type == Token.Types.Brackets)
						{
							if (brackets.TryGetValue(token.Value, out string? pair))
							{
								for (int index2 = begin + 1; index2 < tokens.Length; index2++)
								{
									if (tokens[index2].Value == pair)
									{
										begin++;
										Node node = Parse0(tokens, ref begin, index2);
										begin++;
										return node;
									}
								}
								throw new ArgumentException($"Expected '{pair}'");
							}
							else throw new ArgumentException($"Invalid bracket: {token}");
						}
						else if (token.Type == Token.Types.Semicolon)
						{
							if (token.Value == ";")
							{
								Node node = DataNode.Null;
								begin++;
								trees.Add(Parse0(tokens, ref begin, end));
								return node;
							}
							else throw new ArgumentException($"Invalid semicolon: {token}");
						}
						else throw new ArgumentException($"Invalid token: {token}");
					}
					else return DataNode.Null;
				}

				for (int index = 0; true;)
				{
					if (index < tokens.Length)
					{
						Token token = tokens[index];
						if (token.Type == Token.Types.Semicolon && token.Value == ";") break;
						Node tree = Parse0(tokens, ref index, tokens.Length);
						trees.Add(tree);
					}
					else throw new ArgumentException($"Expected ';'");
				}
				return trees.ToArray();
			}
			#endregion
			#region Evalutor
			private readonly Dictionary<string, double?> memory = new()
			{
				{ "Pi", PI },
				{ "E", E },
			};
			public double?[] Evaluate(in Node[] trees)
			{
				Node Assemble0(in Node node)
				{
					if (node is DataNode nodeData)
					{
						return nodeData;
					}
					else if (node is ReferenceNode nodeReference)
					{
						return nodeReference;
					}
					else if (node is UnaryOperatorNode nodeUnaryOperator)
					{
						switch (nodeUnaryOperator.Operator)
						{
							case "data":
							{
								if (nodeUnaryOperator.Target is ReferenceNode nodeTargetReference)
								{
									this.memory[nodeTargetReference.Path] = null;
								}
								else throw new ArgumentException($"Identifier expected");
								return nodeTargetReference;
							}
							case "+":
							case "-":
							case "print":
							{
								return nodeUnaryOperator;
							}
							default: throw new ArgumentException($"Invalid {nodeUnaryOperator.Operator} keyword");
						}
					}
					else if (node is BinaryOperatorNode nodeBinaryOperator)
					{
						switch (nodeBinaryOperator.Operator)
						{
							case "+":
							case "-":
							case "*":
							case "/":
							{
								return new BinaryOperatorNode
								(
									nodeBinaryOperator.Operator,
									Assemble0(nodeBinaryOperator.Left),
									Assemble0(nodeBinaryOperator.Right)
								);
							}
							case ":":
							{
								return new BinaryOperatorNode
								(
									nodeBinaryOperator.Operator,
									Assemble0(nodeBinaryOperator.Left),
									Assemble0(nodeBinaryOperator.Right)
								);
							}
							default: throw new ArgumentException($"Invalid {nodeBinaryOperator.Operator} operator");
						}
					}
					else throw new ArgumentException($"Invalid node {node.GetType()} type");
				}
				double? Evaluate1(in Node node)
				{
					if (node is DataNode nodeData)
					{
						return nodeData.Value;
					}
					else if (node is ReferenceNode nodeReference)
					{
						return this.memory.TryGetValue(nodeReference.Path, out double? value)
							? value
							: throw new Exception($"Identifier '{nodeReference.Path}' does not exist");
					}
					else if (node is UnaryOperatorNode nodeUnaryOperator)
					{
						switch (nodeUnaryOperator.Operator)
						{
							case "+":
							{
								return Evaluate1(nodeUnaryOperator.Target);
							}
							case "-":
							{
								return -Evaluate1(nodeUnaryOperator.Target);
							}
							case "print":
							{
								Console.WriteLine(Evaluate1(nodeUnaryOperator.Target));
								return null;
							}
							default: throw new ArgumentException($"Invalid {nodeUnaryOperator.Operator} keyword");
						}
					}
					else if (node is BinaryOperatorNode nodeBinaryOperator)
					{
						switch (nodeBinaryOperator.Operator)
						{
							case "+":
							{
								double left = Evaluate1(nodeBinaryOperator.Left) ?? throw new NullReferenceException($"Operator '{nodeBinaryOperator.Operator}' cannot be applied to null operand");
								double right = Evaluate1(nodeBinaryOperator.Right) ?? throw new NullReferenceException($"Operator '{nodeBinaryOperator.Operator}' cannot be applied to null operand");
								return left + right;
							}
							case "-":
							{
								double left = Evaluate1(nodeBinaryOperator.Left) ?? throw new NullReferenceException($"Operator '{nodeBinaryOperator.Operator}' cannot be applied to null operand");
								double right = Evaluate1(nodeBinaryOperator.Right) ?? throw new NullReferenceException($"Operator '{nodeBinaryOperator.Operator}' cannot be applied to null operand");
								return left - right;
							}
							case "*":
							{
								double left = Evaluate1(nodeBinaryOperator.Left) ?? throw new NullReferenceException($"Operator '{nodeBinaryOperator.Operator}' cannot be applied to null operand");
								double right = Evaluate1(nodeBinaryOperator.Right) ?? throw new NullReferenceException($"Operator '{nodeBinaryOperator.Operator}' cannot be applied to null operand");
								return left * right;
							}
							case "/":
							{
								double left = Evaluate1(nodeBinaryOperator.Left) ?? throw new NullReferenceException($"Operator '{nodeBinaryOperator.Operator}' cannot be applied to null operand");
								double right = Evaluate1(nodeBinaryOperator.Right) ?? throw new NullReferenceException($"Operator '{nodeBinaryOperator.Operator}' cannot be applied to null operand");
								return left / right;
							}
							case ":":
							{
								double? right = Evaluate1(nodeBinaryOperator.Right); // ?? throw new NullReferenceException($"Operator '{nodeBinaryOperator.Operator}' cannot be applied to null operand");
								if (!(nodeBinaryOperator.Left is ReferenceNode)) throw new ArgumentException($"Left side of assignment must be identifier");
								ReferenceNode nodeLeftReference = (ReferenceNode) nodeBinaryOperator.Left;
								if (!memory.ContainsKey(nodeLeftReference.Path)) throw new Exception($"Identifier '{nodeLeftReference.Path}' does not exist");
								this.memory[nodeLeftReference.Path] = right;
								return right;
							}
							default: throw new ArgumentException($"Invalid {nodeBinaryOperator.Operator} operator");
						}
					}
					else throw new ArgumentException($"Invalid node {node.GetType()} type");
				}

				return trees.Select(tree => Evaluate1(Assemble0(tree))).ToArray();
			}
			public double?[] Evaluate(in string code) => this.Evaluate(this.Parse(this.Tokenize(code)));
			#endregion
		}
	}
}