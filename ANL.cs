using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Globalization;

namespace AdaptiveCore
{
	namespace ANL
	{
		public class Interpreter
		{
			#region Tokenizer
			public class Token
			{
				public enum Types { Number, Identifier, Operator, Parenthesis, }
				public Token(Types type, string value)
				{
					this.Type = type;
					this.Value = value;
				}
				public Types Type { get; }
				public string Value { get; }
				public override string ToString() => $"{this.Type}: {this.Value}";
			}
			private Match MatchDigit(in char symbol) => new Regex(@"\d").Match(symbol.ToString());
			private Match MatchNumber(in string text) => new Regex(@"^\d+(\.\d+)?").Match(text);
			private Match MatchLetter(in char symbol) => new Regex(@"[A-Za-z_]").Match(symbol.ToString());
			private Match MatchIdentifier(in string text) => new Regex(@"\w+").Match(text);
			private Match MatchOperator(in char text) => new Regex(@"\+|-|\*|\/|\^").Match(text.ToString());
			private Match MatchParenthesis(in char text) => new Regex(@"\(|\)").Match(text.ToString());
			private Match MatchWhiteSpace(in char symbol) => new Regex(@"\s").Match(symbol.ToString());
			public Token[] Tokenize(in string input)
			{
				int index = 0;
				List<Token> tokens = new();
				while (index < input.Length)
				{
					char current = input[index];
					if (MatchDigit(current).Success)
					{
						Match match = MatchNumber(input.Substring(index));
						string value = match.Value;
						tokens.Add(new Token(Token.Types.Number, value));
						index += value.Length;
					}
					else if (MatchLetter(current).Success)
					{
						Match match = MatchIdentifier(input.Substring(index));
						string value = match.Value;
						tokens.Add(new Token(Token.Types.Identifier, value));
						index += value.Length;
					}
					else if (MatchOperator(current).Success)
					{
						tokens.Add(new Token(Token.Types.Operator, current.ToString()));
						index++;
					}
					else if (MatchParenthesis(current).Success)
					{
						tokens.Add(new Token(Token.Types.Parenthesis, current.ToString()));
						index++;
					}
					else if (MatchWhiteSpace(current).Success)
					{
						index++;
					}
					else
						throw new ArgumentException($"Unexpected character '{current}' at position {index}");
				}
				return tokens.ToArray();
			}
			#endregion
			#region Parser
			public abstract class Node { }
			public class DataNode: Node
			{
				public DataNode(double value) : base()
				{
					this.Value = value;
				}
				public double Value { get; }
				public override string ToString() => $"{this.Value}";
			}
			public class PointerNode: Node
			{
				public PointerNode(string path) : base()
				{
					this.Path = path;
				}
				public string Path { get; }
				public override string ToString() => $"{this.Path}";
			}
			public class OperatorNode: Node
			{
				public OperatorNode(string @operator, Node left, Node right) : base()
				{
					this.Operator = @operator;
					this.Left = left;
					this.Right = right;
				}
				public string Operator { get; }
				public Node Left { get; }
				public Node Right { get; }
				public override string ToString() => $"({this.Left} {this.Operator} {this.Right})";
			}
			/* private readonly Dictionary<string, string> parentheses = new()
			{
				{ @"(", @")" },
			}; */
			public Node Parse(in Token[] tokens)
			{
				int index = 0;
				Node node = this.ParseSecondary(tokens, ref index);
				if (index < tokens.Length)
					throw new ArgumentException($"Expected end of expression at position {index}");
				return node;
			}
			private Node ParseSecondary(in Token[] tokens, ref int index)
			{
				Node left = this.ParsePrimary(tokens, ref index);
				while (index < tokens.Length)
				{
					Token token = tokens[index];
					if (token.Type == Token.Types.Operator && (token.Value == "+" || token.Value == "-"))
					{
						index++;
						Node right = this.ParsePrimary(tokens, ref index);
						left = new OperatorNode(token.Value, left, right);
					}
					else
						break;
				}
				return left;
			}
			private Node ParsePrimary(in Token[] tokens, ref int index)
			{
				Node left = this.ParseTerm(tokens, ref index);
				while (index < tokens.Length)
				{
					Token token = tokens[index];
					if (token.Type == Token.Types.Operator && (token.Value == "*" || token.Value == "/" || token.Value == "^"))
					{
						index++;
						Node right = this.ParseTerm(tokens, ref index);
						left = new OperatorNode(token.Value, left, right);
					}
					else
						break;
				}
				return left;
			}
			private Node ParseTerm(in Token[] tokens, ref int index)
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
						Node node = new PointerNode(value);
						index++;
						return node;
					}
					else if (token.Type == Token.Types.Operator)
					{
						if (token.Value == "+")
						{
							index++;
							Node right = this.ParseSecondary(tokens, ref index);
							return new OperatorNode(token.Value, new DataNode(0), right);
						}
						else if (token.Value == "-")
						{
							index++;
							Node right = this.ParseSecondary(tokens, ref index);
							return new OperatorNode(token.Value, new DataNode(0), right);
						}
						else
							throw new ArgumentException($"Invalid operator at position {index}");
					}
					else if (token.Type == Token.Types.Parenthesis)
					{
						if (token.Value == "(")
						{
							index++;
							if (index < tokens.Length && tokens[index].Type == Token.Types.Parenthesis && tokens[index].Value == ")")
							{
								index++;
								return new DataNode(double.NaN);
							}
							Node expression = this.ParseSecondary(tokens, ref index);
							if (index >= tokens.Length || tokens[index].Type != Token.Types.Parenthesis || tokens[index].Value != ")")
								throw new ArgumentException($"Expected ')' at position {index}");
							index++;
							return expression;
						}
						else
							throw new ArgumentException($"Invalid parentheses '{token.Value}' at position {index}");
					}
					else
						throw new ArgumentException($"Invalid term '{token.Value}' at position {index}");
				}
				else
					return new DataNode(double.NaN);
			}
			#endregion
			#region Evalutor
			private readonly Dictionary<string, double> memory = new()
			{
				{ "PI", Math.PI },
				{ "E", Math.E },
				{ "Tau", Math.Tau },
			};
			public double Evaluate(in Node node)
			{
				if (node is DataNode dataNode)
				{
					return dataNode.Value;
				}
				else if (node is PointerNode pointerNode)
				{
					double value;
					if (this.memory.TryGetValue(pointerNode.Path, out value))
					{
						return value;
					}
					else
						throw new NullReferenceException($"Identifier with name '{pointerNode.Path}' could not be found");
				}
				else if (node is OperatorNode operatorNode)
				{
					double left = Evaluate(operatorNode.Left);// ?? throw new NullReferenceException("Left value is not defined.");
					double right = Evaluate(operatorNode.Right);// ?? throw new NullReferenceException("Left value is not defined.");
					return operatorNode.Operator switch
					{
						"+" => left + right,
						"-" => left - right,
						"*" => left * right,
						"/" => left / right,
						"^" => Math.Pow(left, right),
						_ => throw new ArgumentException($"Invalid operator: {operatorNode.Operator}"),
					};
				}
				else
					throw new ArgumentException($"Invalid node type: {node.GetType()}");
			}
			#endregion
		}
	}
}
