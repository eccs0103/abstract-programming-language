using System;
using System.Globalization;
using System.Text.RegularExpressions;

using static System.Math;

namespace AdaptiveCore
{
	namespace ANL
	{
		public partial class Interpreter
		{
			#region Tokenizer
			public class Token(Token.Types type, string value, int line, int column)
			{
				public enum Types { Number, Register, Keyword, Operator, Brackets, Semicolon }
				public Types Type { get; } = type;
				public string Value { get; } = value;
				public int Line { get; } = line;
				public int Column { get; } = column;
				public override string ToString()
				{
					return $"{this.Type switch
					{
						Types.Number => "Number",
						Types.Register => "Register",
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
				{ new Regex(@"^(\+|-|\*|/|:)", RegexOptions.Compiled), Token.Types.Operator },
				{ new Regex(@"^[A-z]\w*", RegexOptions.Compiled), Token.Types.Register },
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
				List<Token> tokens = [];
				for (string text = code; text.Length > 0;)
				{
					bool hasChanges = false;
					foreach ((Regex regex, Token.Types? typified) in dictionary)
					{
						Match match = regex.Match(text);
						if (match.Success)
						{
							if (typified != null)
							{
								Token.Types type = typified.GetValueOrDefault();
								if (type == Token.Types.Register && keywords.Contains(match.Value))
								{
									type = Token.Types.Keyword;
								}
								tokens.Add(new Token(type, match.Value, line, column));
							}
							if (match.Value == "\n")
							{
								line++;
								column = 0;
							}
							else column += match.Length;
							hasChanges = true;
							text = text[(match.Index + match.Length)..];
							break;
						}
					}
					if (!hasChanges) throw new FormatException($"Invalid {text[0]} term");
				}
				return tokens.ToArray();
			}
			#endregion
			#region Parser
			public abstract class Node()
			{
			}
			private class ValueNode(double? value): Node()
			{
				public static readonly ValueNode Null = new(null);
				public double? Value { get; } = value;
				public override string ToString() => $"{this.Value}";
			}
			private class RegisterNode(string address): Node()
			{
				public string Address { get; } = address;
				public override string ToString() => $"{this.Address}";
			}
			private abstract class OperatorNode(string @operator): Node()
			{
				public string Operator { get; } = @operator;
				public override string ToString() => $"({this.Operator})";
			}
			private class UnaryOperatorNode(string @operator, Node target): OperatorNode(@operator)
			{
				public Node Target { get; set; } = target;
				public override string ToString() => $"{this.Operator}({this.Target})";
			}
			private class BinaryOperatorNode(string @operator, Node left, Node right): OperatorNode(@operator)
			{
				public Node Left { get; set; } = left;
				public Node Right { get; set; } = right;
				public override string ToString() => $"({this.Left} {this.Operator} {this.Right})";
			}
			private static readonly Dictionary<string, string> brackets = new()
			{
				{  @"(", @")" },
			};
			private static Node InitialParse(in Token[] tokens, ref int begin, int end)
			{
				return OperatorsGroup1Parse(tokens, ref begin, end);
			}
			private static Node OperatorsGroup1Parse(in Token[] tokens, ref int begin, int end)
			{
				Node left = OperatorsGroup2Parse(tokens, ref begin, end);
				while (begin < Min(tokens.Length, end))
				{
					Token token = tokens[begin];
					if (token.Type == Token.Types.Operator)
					{
						switch (token.Value)
						{
							case ":":
							{
								begin++;
								Node right = OperatorsGroup2Parse(tokens, ref begin, end);
								left = new BinaryOperatorNode(token.Value, left, right);
							}
							break;
						}
					}
					else break;
				}
				return left;
			}
			private static Node OperatorsGroup2Parse(in Token[] tokens, ref int begin, int end)
			{
				Node left = OperatorsGroup3Parse(tokens, ref begin, end);
				while (begin < Min(tokens.Length, end))
				{
					Token token = tokens[begin];
					if (token.Type == Token.Types.Operator && (token.Value == "+" || token.Value == "-"))
					{
						begin++;
						Node right = OperatorsGroup3Parse(tokens, ref begin, end);
						left = new BinaryOperatorNode(token.Value, left, right);
					}
					else break;
				}
				return left;
			}
			private static Node OperatorsGroup3Parse(in Token[] tokens, ref int begin, int end)
			{
				Node left = VerticesParse(tokens, ref begin, end);
				while (begin < Min(tokens.Length, end))
				{
					Token token = tokens[begin];
					if (token.Type == Token.Types.Operator && (token.Value == "*" || token.Value == "/"))
					{
						begin++;
						Node right = VerticesParse(tokens, ref begin, end);
						left = new BinaryOperatorNode(token.Value, left, right);
					}
					else break;
				}
				return left;
			}
			private static Node VerticesParse(in Token[] tokens, ref int begin, int end)
			{
				if (begin < Min(tokens.Length, end))
				{
					Token token = tokens[begin];
					switch (token.Type)
					{
						case Token.Types.Number:
						{
							Node node = new ValueNode(Convert.ToDouble(token.Value, CultureInfo.GetCultureInfo("en-US")));
							begin++;
							return node;
						}
						case Token.Types.Register:
						{
							Node node = new RegisterNode(token.Value);
							begin++;
							return node;
						}
						case Token.Types.Keyword:
						{
							switch (token.Value)
							{
								case "data":
								{
									begin++;
									Node target = VerticesParse(tokens, ref begin, end);
									return new UnaryOperatorNode(token.Value, target);
								}
								case "print":
								{
									begin++;
									Node target = VerticesParse(tokens, ref begin, end);
									return new UnaryOperatorNode(token.Value, target);
								}
								case "null":
								{
									begin++;
									return ValueNode.Null;
								}
								default: throw new ArgumentException($"Invalid keyword: {token}");
							}
						}
						case Token.Types.Operator:
						{
							if (token.Value == "+" || token.Value == "-")
							{
								begin++;
								Node target = VerticesParse(tokens, ref begin, end);
								return new UnaryOperatorNode(token.Value, target);
							}
							else throw new ArgumentException($"Invalid operator: {token}");
						}
						case Token.Types.Brackets:
						{
							if (brackets.TryGetValue(token.Value, out string? pair))
							{
								for (int index2 = tokens.Length - 1; index2 > begin; index2--)
								{
									if (tokens[index2].Value == pair)
									{
										begin++;
										Node node = InitialParse(tokens, ref begin, index2);
										begin++;
										return node;
									}
								}
								throw new ArgumentException($"Expected '{pair}'");
							}
							else throw new ArgumentException($"Invalid bracket: {token}");
						}
						default: throw new ArgumentException($"Invalid token: {token}");
					}
				}
				else throw new NullReferenceException($"Expected expression");
			}
			public Node[] Parse(in Token[] tokens)
			{
				List<Node> trees = [];
				for (int index = 0; index < tokens.Length;)
				{
					Node tree = InitialParse(tokens, ref index, tokens.Length);
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
			private class DataInitializer
			{
				public string Type { get; set; } = "Any";
				public bool Writeable { get; set; } = true;
			}
			private class Data
			{
				public Data(double? value, DataInitializer? initializer = null)
				{
					DataInitializer mainInitializer = initializer ?? new();
					this.Type = mainInitializer.Type;
					this.Writeable = mainInitializer.Writeable;
					this.value = value;
				}
				public readonly string Type;
				public readonly bool Writeable;
				private double? value;
				public double? Value
				{
					get => this.value;
					set
					{
						this.value = this.Writeable
							? value
							: throw new InvalidOperationException($"Data is non-writeable");
					}
				}
			}
			private readonly Dictionary<string, Data> memory = new()
			{
				{ "Pi", new Data(PI, new() { Writeable = false, }) },
				{ "E", new Data(E, new() { Writeable = false, }) },
			};
			private ValueNode ToValueNode(in Node node)
			{
				if (node is ValueNode nodeValue)
				{
					return nodeValue;
				}
				else if (node is RegisterNode nodeRegister)
				{
					return this.memory.TryGetValue(nodeRegister.Address, out Data? data)
						? new ValueNode(data.Value)
						: throw new Exception($"Identifier '{nodeRegister.Address}' does not exist");
				}
				else if (node is UnaryOperatorNode nodeUnaryOperator)
				{
					switch (nodeUnaryOperator.Operator)
					{
						case "+":
						case "-":
						{
							return this.ToValueNode(new BinaryOperatorNode(nodeUnaryOperator.Operator, new ValueNode(0), nodeUnaryOperator.Target));
						}
						case "data":
						{
							return this.ToValueNode(this.ToRegisterNode(nodeUnaryOperator));
						}
						case "print":
						{
							ValueNode nodeValue2 = this.ToValueNode(nodeUnaryOperator.Target);
							Console.WriteLine(nodeValue2.Value);
							return ValueNode.Null;
						}
						default: throw new ArgumentException($"Invalid '{nodeUnaryOperator.Operator}' operator");
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
							double dataLeft = this.ToValueNode(nodeBinaryOperator.Left).Value ?? throw new NullReferenceException($"Operator '{nodeBinaryOperator.Operator}' cannot be applied to null operand"); ;
							double dataRight = this.ToValueNode(nodeBinaryOperator.Right).Value ?? throw new NullReferenceException($"Operator '{nodeBinaryOperator.Operator}' cannot be applied to null operand"); ;
							return nodeBinaryOperator.Operator switch
							{
								"+" => new ValueNode(dataLeft + dataRight),
								"-" => new ValueNode(dataLeft - dataRight),
								"*" => new ValueNode(dataLeft * dataRight),
								"/" => new ValueNode(dataLeft / dataRight),
								_ => throw new ArgumentException($"Invalid {nodeBinaryOperator.Operator} operator"),
							};
						}
						case ":":
						{
							return this.ToValueNode(this.ToRegisterNode(nodeBinaryOperator));
						}
						default: throw new ArgumentException($"Invalid '{nodeBinaryOperator.Operator}' operator");
					}
				}
				else throw new ArgumentException($"Unable to evaluate ValueNode from {node}");
			}
			private RegisterNode ToRegisterNode(in Node node)
			{
				if (node is RegisterNode nodeRegister)
				{
					return nodeRegister;
				}
				else if (node is UnaryOperatorNode nodeUnaryOperator)
				{
					switch (nodeUnaryOperator.Operator)
					{
						case "data":
						{
							if (nodeUnaryOperator.Target is RegisterNode nodeRegisterTarget)
							{
								if (!this.memory.TryAdd(nodeRegisterTarget.Address, new Data(null)))
								{
									throw new ArgumentException($"Identifier '{nodeRegisterTarget.Address}' already exists");
								}
							}
							else throw new ArgumentException($"Identifier expected");
							return nodeRegisterTarget;
						}
						default: throw new ArgumentException($"Invalid '{nodeUnaryOperator.Operator}' operator");
					}
				}
				else if (node is BinaryOperatorNode nodeBinaryOperator)
				{
					switch (nodeBinaryOperator.Operator)
					{
						case ":":
						{
							ValueNode nodeValueRight = this.ToValueNode(nodeBinaryOperator.Right);
							RegisterNode nodeRegisterLeft = this.ToRegisterNode(nodeBinaryOperator.Left);
							if (!this.memory.TryGetValue(nodeRegisterLeft.Address, out Data? data)) throw new Exception($"Identifier '{nodeRegisterLeft.Address}' does not exist");
							data.Value = data.Writeable
								? nodeValueRight.Value
								: throw new InvalidOperationException($"Identifier '{nodeRegisterLeft.Address}' is non-writeable");
							return nodeRegisterLeft;
						}
						default: throw new ArgumentException($"Invalid '{nodeBinaryOperator.Operator}' operator");
					}
				}
				else throw new ArgumentException($"Unable to evaluate RegisterNode from {node}");
			}
			private void Evaluate(in Node node) => _ = this.ToValueNode(node);
			public void Evaluate(in Node[] trees)
			{
				foreach (Node tree in trees)
				{
					this.Evaluate(tree);
				}
			}
			public void Evaluate(in string code) => this.Evaluate(this.Parse(this.Tokenize(code)));
			#endregion
		}
	}
}