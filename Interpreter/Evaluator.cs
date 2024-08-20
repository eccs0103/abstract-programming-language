using System.Text;

using static System.Math;

namespace APL;

internal partial class Interpreter
{
	private class Datul(in object? value, in Datul.Initializer initializer)
	{
		public readonly struct Initializer(in bool mutable = true)
		{
			public readonly bool Mutable = mutable;
		}
		public readonly bool Mutable = initializer.Mutable;
		private object? _Value = value;
		public object? Value
		{
			get => this._Value;
			set
			{
				if (!this.Mutable) return;
				this._Value = value;
			}
		}
	}

	//private class MemberInformation(in string type, in object? value, in MemberInformation.Initializer initializer)
	//{
	//	public class Initializer(in bool mutable = true)
	//	{
	//		public readonly bool Mutable = mutable;
	//	}
	//	public readonly string Type = type;
	//	private object? _Value = value;
	//	public readonly bool Mutable = initializer.Mutable;
	//	public object? Value
	//	{
	//		get => this._Value;
	//		set
	//		{
	//			if (!this.Mutable) return;
	//			this._Value = value;
	//		}
	//	}
	//}
	//private class TypeInformation(in Dictionary<string, MemberInformation> value, in TypeInformation.Initializer initializer): MemberInformation("Type", value, initializer)
	//{
	//	public new class Initializer(): MemberInformation.Initializer(false)
	//	{

	//	}
	//	public new Dictionary<string, MemberInformation> Value
	//	{
	//		get => base.Value;
	//		set => base.Value = value;
	//	}
	//}
	//private readonly Dictionary<string, MemberInformation> Memory2 = new()
	//{
	//	{ "Type", new TypeInformation(null, new()) }
	//};

	private static string? GlobalFetch(in string address)
	{
		try
		{
			using HttpClient client = new();
			HttpResponseMessage response = client.GetAsync(address).Result;
			response.EnsureSuccessStatusCode();
			return response.Content.ReadAsStringAsync().Result;
		}
		catch (Exception)
		{
			return null;
		}
	}
	private static string? LocalFetch(in string address)
	{
		try
		{
			FileInfo file = new(address);
			if (!file.Extension.Equals(".APL", StringComparison.OrdinalIgnoreCase)) throw new FileNotFoundException($"Only files with the .APL extension can be imported");
			using StreamReader reader = file.OpenText();
			return reader.ReadToEnd();
		}
		catch (Exception)
		{
			return null;
		}
	}
	private static string? Fetch(in string address)
	{
		return LocalFetch(address) ?? GlobalFetch(address);
	}

	private readonly Dictionary<string, Datul> Memory = new()
	{
		{ "Pi", new Datul(PI, new(false)) },
		{ "E", new Datul(E, new(false)) },
	};
	private abstract partial class Node
	{
		protected static T PreventEvaluation<T>(in Node node) where T : Node
		{
			throw new Error($"Unable to evaluate {typeof(T).Name} from {node.GetType().Name}", node.RangePosition.Begin);
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
	private partial class IdentifierNode: Node
	{
		public override T Evaluate<T>(in Interpreter interpreter)
		{
			if (IsCompatible<T, ValueNode>())
			{
				if (!interpreter.Memory.TryGetValue(this.Name, out Datul? datul)) throw new Error($"Identifier '{this.Name}' does not exist", this.RangePosition.Begin);
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
						if (builder.Length > 0) builder.Append('\n');
						builder.Append(argument.Evaluate<ValueNode>(interpreter).GetValue<double>());
					}
					Console.WriteLine(builder.ToString());
					return Cast<T>(new ValueNode(null, this.RangePosition));
				}
				default: throw new Error($"Function '{this.Target.Name}' does not exist", this.RangePosition.Begin);
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
					string address = this.Target.Evaluate<ValueNode>(interpreter).GetValue<string>();
					string input = Fetch(address) ?? throw new Error($"Executable APL file in '{address}' doesn't exist", this.RangePosition.Begin);
					interpreter.Run(input);
					return Cast<T>(new ValueNode(null, this.RangePosition));
				}
				default: throw new Error($"Unidentified '{this.Operator}' operator", this.RangePosition.Begin);
				}
			}
			if (IsCompatible<T, IdentifierNode>())
			{
				switch (this.Operator)
				{
				case "data":
				{
					IdentifierNode identifier = this.Target.Evaluate<IdentifierNode>(interpreter);
					if (!interpreter.Memory.TryAdd(identifier.Name, new Datul(null, new(true)))) throw new Error($"Identifier '{identifier.Name}' already exists", this.RangePosition.Begin);
					return Cast<T>(identifier);
				}
				default: throw new Error($"Unidentified '{this.Operator}' operator", this.RangePosition.Begin);
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
					double left = this.Left.Evaluate<ValueNode>(interpreter).GetValue<double>();
					double right = this.Right.Evaluate<ValueNode>(interpreter).GetValue<double>();
					return Cast<T>(new ValueNode(left + right, this.RangePosition));
				}
				case "-":
				{
					double left = this.Left.Evaluate<ValueNode>(interpreter).GetValue<double>();
					double right = this.Right.Evaluate<ValueNode>(interpreter).GetValue<double>();
					return Cast<T>(new ValueNode(left - right, this.RangePosition));
				}
				case "*":
				{
					double left = this.Left.Evaluate<ValueNode>(interpreter).GetValue<double>();
					double right = this.Right.Evaluate<ValueNode>(interpreter).GetValue<double>();
					return Cast<T>(new ValueNode(left * right, this.RangePosition));
				}
				case "/":
				{
					double left = this.Left.Evaluate<ValueNode>(interpreter).GetValue<double>();
					double right = this.Right.Evaluate<ValueNode>(interpreter).GetValue<double>();
					return Cast<T>(new ValueNode(left / right, this.RangePosition));
				}
				case ":":
				{
					return Cast<T>(this.Evaluate<IdentifierNode>(interpreter).Evaluate<ValueNode>(interpreter));
				}
				default: throw new Error($"Unidentified '{this.Operator}' operator", this.RangePosition.Begin);
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
					if (!interpreter.Memory.TryGetValue(left.Name, out Datul? datul)) throw new Error($"Identifier '{left.Name}' does not exist", this.RangePosition.Begin);
					if (!datul.Mutable) throw new Error($"Identifier '{left.Name}' is non-mutable", this.RangePosition.Begin);
					datul.Value = right.GetValue<object>();
					return Cast<T>(left);
				}
				default: throw new Error($"Unidentified '{this.Operator}' operator", this.RangePosition.Begin);
				}
			}
			return PreventEvaluation<T>(this);
		}
	}
	private void Evaluate(in IEnumerable<Node> trees)
	{
		foreach (Node tree in trees)
		{
			tree.Evaluate<ValueNode>(this);
		}
	}
}
