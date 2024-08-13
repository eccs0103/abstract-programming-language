using System.Text;

using static System.Math;

namespace APL;

internal partial class Interpreter
{
	private class Datul(in double? value, in Datul.Initializer initializer)
	{
		public readonly struct Initializer(in bool mutable = true)
		{
			public readonly bool Mutable = mutable;
		}
		public readonly bool Mutable = initializer.Mutable;
		private double? value = value;
		public double? Value
		{
			get => this.value;
			set
			{
				if (!this.Mutable) return;
				this.value = value;
			}
		}
	}

	private readonly Dictionary<string, Datul> Memory = new()
	{
		{ "Pi", new Datul(PI, new(false)) },
		{ "E", new Datul(E, new(false)) },
	};

	public abstract partial class Node
	{
		protected static T PreventEvaluation<T>(in Node node) where T : Node
		{
			throw new Exception($"Unable to evaluate {typeof(T).Name} from {node.GetType().Name} at {node.RangePosition.Begin}");
		}
		protected static T Cast<T>(in Node node) where T : Node
		{
			return node as T ?? PreventEvaluation<T>(node);
		}
		protected static bool IsCompatible<T, N>() where T : Node where N : Node
		{
			return typeof(T) == typeof(N);
		}
		public virtual T Evaluate<T>(in Interpreter interpreter) where T : Node
		{
			return PreventEvaluation<T>(this);
		}
	}
	private partial class ValueNode: Node
	{
		public override T Evaluate<T>(in Interpreter interpreter)
		{
			if (IsCompatible<T, ValueNode>()) return Cast<T>(this);
			return PreventEvaluation<T>(this);
		}
	}
	private partial class PathNode: Node
	{
		public override T Evaluate<T>(in Interpreter interpreter)
		{
			if (IsCompatible<T, PathNode>()) return Cast<T>(this);
			return PreventEvaluation<T>(this);
		}
	}
	private partial class IdentifierNode: Node
	{
		public override T Evaluate<T>(in Interpreter interpreter)
		{
			if (IsCompatible<T, ValueNode>())
			{
				if (!interpreter.Memory.TryGetValue(this.Name, out Datul? datul)) throw new Exception($"Identifier '{this.Name}' does not exist at {this.RangePosition.Begin}");
				return Cast<T>(new ValueNode(datul.Value, this.RangePosition));
			}
			if (IsCompatible<T, IdentifierNode>()) return Cast<T>(this);
			return PreventEvaluation<T>(this);
		}
	}
	private partial class InvokationNode: Node
	{
		public override T Evaluate<T>(in Interpreter interpreter)
		{
			if (IsCompatible<T, ValueNode>())
			{
				switch (this.Target.Name)
				{
				case "Write":
				{
					StringBuilder builder = new();
					foreach (Node argument in this.Arguments)
					{
						builder.Append($"{argument.Evaluate<ValueNode>(interpreter).Value}\n");
					}
					Console.WriteLine(builder.ToString());
					return Cast<T>(new ValueNode(null, this.RangePosition));
				}
				default: throw new Exception($"Function '{this.Target.Name}' does not exist at {this.RangePosition.Begin}");
				}
			}
			return PreventEvaluation<T>(this);
		}
	}
	private partial class UnaryOperatorNode: OperatorNode
	{
		public override T Evaluate<T>(in Interpreter interpreter)
		{
			if (IsCompatible<T, ValueNode>())
			{
				switch (this.Operator)
				{
				case "+":
				case "-":
				{
					return Cast<T>(new BinaryOperatorNode(this.Operator, new ValueNode(0, this.RangePosition), this.Target, this.RangePosition).Evaluate<ValueNode>(interpreter));
				}
				case "data":
				{
					return Cast<T>(this.Evaluate<IdentifierNode>(interpreter).Evaluate<ValueNode>(interpreter));
				}
				case "import":
				{
					string path = this.Target.Evaluate<PathNode>(interpreter).Path;
					FileInfo file = new(path);
					if (!file.Exists) throw new Exception($"File {file.FullName} doesn't exist");
					if (!file.Extension.Equals(".APL", StringComparison.CurrentCultureIgnoreCase)) throw new Exception($"Only files with the .APL extension can be imported");
					interpreter.Evaluate(File.ReadAllText(file.FullName));
					return Cast<T>(new ValueNode(null, this.RangePosition));
				}
				default: throw new ArgumentException($"Unidentified '{this.Operator}' operator at {this.RangePosition.Begin}");
				}
			}
			if (IsCompatible<T, IdentifierNode>())
			{
				switch (this.Operator)
				{
				case "data":
				{
					IdentifierNode identifier = this.Target.Evaluate<IdentifierNode>(interpreter) ?? throw new ArgumentException($"Identifier expected at {this.RangePosition.Begin}");
					if (!interpreter.Memory.TryAdd(identifier.Name, new Datul(null, new(true)))) throw new ArgumentException($"Identifier '{identifier.Name}' already exists at {this.RangePosition.Begin}");
					return Cast<T>(identifier);
				}
				default: throw new ArgumentException($"Unidentified '{this.Operator}' operator at {this.RangePosition.Begin}");
				}
			}
			return PreventEvaluation<T>(this);
		}
	}
	private partial class BinaryOperatorNode: OperatorNode
	{
		public override T Evaluate<T>(in Interpreter interpreter)
		{
			if (IsCompatible<T, ValueNode>())
			{
				switch (this.Operator)
				{
				case "+":
				{
					double left = this.Left.Evaluate<ValueNode>(interpreter).Value ?? throw new NullReferenceException($"Operator '{this.Operator}' cannot be applied to null operand at {this.RangePosition.Begin}");
					double right = this.Right.Evaluate<ValueNode>(interpreter).Value ?? throw new NullReferenceException($"Operator '{this.Operator}' cannot be applied to null operand at {this.RangePosition.Begin}");
					return Cast<T>(new ValueNode(left + right, this.RangePosition));
				}
				case "-":
				{
					double left = this.Left.Evaluate<ValueNode>(interpreter).Value ?? throw new NullReferenceException($"Operator '{this.Operator}' cannot be applied to null operand at {this.RangePosition.Begin}");
					double right = this.Right.Evaluate<ValueNode>(interpreter).Value ?? throw new NullReferenceException($"Operator '{this.Operator}' cannot be applied to null operand at {this.RangePosition.Begin}");
					return Cast<T>(new ValueNode(left - right, this.RangePosition));
				}
				case "*":
				{
					double left = this.Left.Evaluate<ValueNode>(interpreter).Value ?? throw new NullReferenceException($"Operator '{this.Operator}' cannot be applied to null operand at {this.RangePosition.Begin}");
					double right = this.Right.Evaluate<ValueNode>(interpreter).Value ?? throw new NullReferenceException($"Operator '{this.Operator}' cannot be applied to null operand at {this.RangePosition.Begin}");
					return Cast<T>(new ValueNode(left * right, this.RangePosition));
				}
				case "/":
				{
					double left = this.Left.Evaluate<ValueNode>(interpreter).Value ?? throw new NullReferenceException($"Operator '{this.Operator}' cannot be applied to null operand at {this.RangePosition.Begin}");
					double right = this.Right.Evaluate<ValueNode>(interpreter).Value ?? throw new NullReferenceException($"Operator '{this.Operator}' cannot be applied to null operand at {this.RangePosition.Begin}");
					return Cast<T>(new ValueNode(left / right, this.RangePosition));
				}
				case ":":
				{
					return Cast<T>(this.Evaluate<IdentifierNode>(interpreter).Evaluate<ValueNode>(interpreter));
				}
				default: throw new ArgumentException($"Unidentified '{this.Operator}' operator at {this.RangePosition.Begin}");
				}
			}
			if (IsCompatible<T, IdentifierNode>())
			{
				switch (this.Operator)
				{
				case ":":
				{
					ValueNode right = this.Right.Evaluate<ValueNode>(interpreter);
					IdentifierNode left = this.Left.Evaluate<IdentifierNode>(interpreter);
					if (!interpreter.Memory.TryGetValue(left.Name, out Datul? datul)) throw new Exception($"Identifier '{left.Name}' does not exist at {this.RangePosition.Begin}");
					if (!datul.Mutable) throw new InvalidOperationException($"Identifier '{left.Name}' is non-mutable at {this.RangePosition.Begin}");
					datul.Value = right.Value;
					return Cast<T>(left);
				}
				default: throw new ArgumentException($"Unidentified '{this.Operator}' operator at {this.RangePosition.Begin}");
				}
			}
			return PreventEvaluation<T>(this);
		}
	}

	public void Evaluate(in IEnumerable<Node> trees)
	{
		foreach (Node tree in trees)
		{
			tree.Evaluate<ValueNode>(this);
		}
	}
	public void Evaluate(in string code)
	{
		this.Evaluate(this.Parse(Tokenize(code)));
	}
}
