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
				"print",
				"null"
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
					this.Name = path;
				}
				public string Name { get; }
				public override string ToString() => $"{this.Name}";
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
								case "null":
								{
									begin++;
									return DataNode.Null;
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
						else throw new ArgumentException($"Invalid token: {token}");
					}
					else throw new NullReferenceException($"Expected expression");
				}

				for (int index = 0; index < tokens.Length;)
				{
					Node tree = Parse0(tokens, ref index, tokens.Length);
					if (index < tokens.Length && tokens[index].Type == Token.Types.Semicolon && tokens[index].Value == ";")
					{
						trees.Add(tree);
						index++;
					}
					else throw new ArgumentException($"Expected ';'");
				}
				return trees.ToArray();
			}
			#endregion
			#region Evalutor
			private class Data
			{
				public Data(double? value)
				{
					this.Value = value;
				}
				public double? Value { get; set; }
				public bool Writeable { get; set; } = true;
			}
			private readonly Dictionary<string, Data> memory = new()
			{
				{ "Pi", new Data(PI) { Writeable = false, } },
				{ "E", new Data(E) { Writeable = false, } },
			};
			public void Evaluate(in Node[] trees)
			{
				DataNode ToDataNode(in Node node)
				{
					if (node is DataNode nodeData)
					{
						return nodeData;
					}
					else if (node is ReferenceNode nodeReference)
					{
						return this.memory.TryGetValue(nodeReference.Name, out Data? data)
							? new DataNode(data.Value)
							: throw new Exception($"Identifier '{nodeReference.Name}' does not exist");
					}
					else if (node is UnaryOperatorNode nodeUnaryOperator)
					{
						switch (nodeUnaryOperator.Operator)
						{
							case "+":
							case "-":
							{
								return ToDataNode(new BinaryOperatorNode(nodeUnaryOperator.Operator, new DataNode(0), nodeUnaryOperator.Target));
							}
							case "data":
							{
								return ToDataNode(ToReferenceNode(nodeUnaryOperator));
							}
							case "print":
							{
								DataNode nodeData2 = ToDataNode(nodeUnaryOperator.Target);
								Console.WriteLine(nodeData2.Value);
								return DataNode.Null;
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
								double dataLeft = ToDataNode(nodeBinaryOperator.Left).Value ?? throw new NullReferenceException($"Operator '{nodeBinaryOperator.Operator}' cannot be applied to null operand"); ;
								double dataRight = ToDataNode(nodeBinaryOperator.Right).Value ?? throw new NullReferenceException($"Operator '{nodeBinaryOperator.Operator}' cannot be applied to null operand"); ;
								return nodeBinaryOperator.Operator switch
								{
									"+" => new DataNode(dataLeft + dataRight),
									"-" => new DataNode(dataLeft - dataRight),
									"*" => new DataNode(dataLeft * dataRight),
									"/" => new DataNode(dataLeft / dataRight),
									_ => throw new ArgumentException($"Invalid {nodeBinaryOperator.Operator} operator"),
								};
							}
							case ":":
							{
								return ToDataNode(ToReferenceNode(nodeBinaryOperator));
							}
							default: throw new ArgumentException($"Invalid {nodeBinaryOperator.Operator} operator");
						}
					}
					else throw new ArgumentException($"Invalid expression {node}");
				}
				ReferenceNode ToReferenceNode(in Node node)
				{
					if (node is ReferenceNode nodeReference)
					{
						return nodeReference;
					}
					else if (node is UnaryOperatorNode nodeUnaryOperator)
					{
						switch (nodeUnaryOperator.Operator)
						{
							case "data":
							{
								if (nodeUnaryOperator.Target is ReferenceNode nodeReferenceTarget)
								{
									if (!this.memory.TryAdd(nodeReferenceTarget.Name, new Data(null)))
									{
										throw new ArgumentException($"Identifier '{nodeReferenceTarget.Name}' already exists");
									}
								}
								else throw new ArgumentException($"Identifier expected");
								return nodeReferenceTarget;
							}
							default: throw new ArgumentException($"Invalid {nodeUnaryOperator.Operator} keyword");
						}
					}
					else if (node is BinaryOperatorNode nodeBinaryOperator)
					{
						switch (nodeBinaryOperator.Operator)
						{
							case ":":
							{
								DataNode nodeDataRight = ToDataNode(nodeBinaryOperator.Right);
								ReferenceNode nodeReferenceLeft = ToReferenceNode(nodeBinaryOperator.Left);
								if (!this.memory.ContainsKey(nodeReferenceLeft.Name)) throw new Exception($"Identifier '{nodeReferenceLeft.Name}' does not exist");
								Data data = this.memory[nodeReferenceLeft.Name];
								data.Value = data.Writeable
									? nodeDataRight.Value
									: throw new InvalidOperationException($"Identifier '{nodeReferenceLeft.Name}' is non-writeable");
								return nodeReferenceLeft;
							}
							default: throw new ArgumentException($"Invalid {nodeBinaryOperator.Operator} operator");
						}
					}
					else throw new ArgumentException($"Invalid expression {node}");
				}

				foreach (Node tree in trees)
				{
					_ = ToDataNode(tree);
				}
			}
			public void Evaluate(in string code) => this.Evaluate(this.Parse(this.Tokenize(code)));
			#endregion
		}
	}
}