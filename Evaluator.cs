using static System.Math;

namespace AbstractLanguageModel;

internal partial class Interpreter
{
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
	private readonly Dictionary<string, Data> Memory = new()
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
		else if (node is IdentifierNode nodeIdentifier)
		{
			return this.Memory.TryGetValue(nodeIdentifier.Name, out Data? data)
				? new ValueNode(data.Value)
				: throw new Exception($"Identifier '{nodeIdentifier.Name}' does not exist");
		}
		else if (node is InvokationNode nodeInvokation)
		{
			IdentifierNode nodeInvokationTarget = nodeInvokation.Target;
			switch (nodeInvokationTarget.Name)
			{
			case "Write":
				{
					ValueNode nodeValue2 = this.ToValueNode(nodeInvokation.Arguments[0]);
					Console.WriteLine(nodeValue2.Value);
					return ValueNode.Null;
				}
			default: throw new Exception($"Function '{nodeInvokationTarget.Name}' does not exist");
			}
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
					return this.ToValueNode(this.ToIdentifierNode(nodeUnaryOperator));
				}
			default: throw new ArgumentException($"Unidentified '{nodeUnaryOperator.Operator}' operator");
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
					double dataLeft = this.ToValueNode(nodeBinaryOperator.Left).Value ?? throw new NullReferenceException($"Operator '{nodeBinaryOperator.Operator}' cannot be applied to null operand");
					double dataRight = this.ToValueNode(nodeBinaryOperator.Right).Value ?? throw new NullReferenceException($"Operator '{nodeBinaryOperator.Operator}' cannot be applied to null operand");
					return nodeBinaryOperator.Operator switch
					{
						"+" => new ValueNode(dataLeft + dataRight),
						"-" => new ValueNode(dataLeft - dataRight),
						"*" => new ValueNode(dataLeft * dataRight),
						"/" => new ValueNode(dataLeft / dataRight),
						_ => throw new ArgumentException($"Unidentified {nodeBinaryOperator.Operator} operator"),
					};
				}
			case ":":
				{
					return this.ToValueNode(this.ToIdentifierNode(nodeBinaryOperator));
				}
			default: throw new ArgumentException($"Unidentified '{nodeBinaryOperator.Operator}' operator");
			}
		}
		else throw new ArgumentException($"Unable to evaluate value from {node}");
	}
	private IdentifierNode ToIdentifierNode(in Node node)
	{
		if (node is IdentifierNode nodeIdentifier)
		{
			return nodeIdentifier;
		}
		else if (node is UnaryOperatorNode nodeUnaryOperator)
		{
			switch (nodeUnaryOperator.Operator)
			{
			case "data":
				{
					return nodeUnaryOperator.Target is IdentifierNode nodeIdentifierTarget
						? this.Memory.TryAdd(nodeIdentifierTarget.Name, new Data(null))
							? nodeIdentifierTarget
							: throw new ArgumentException($"Identifier '{nodeIdentifierTarget.Name}' already exists")
						: throw new ArgumentException($"Identifier expected");
				}
			default: throw new ArgumentException($"Unidentified '{nodeUnaryOperator.Operator}' operator");
			}
		}
		else if (node is BinaryOperatorNode nodeBinaryOperator)
		{
			switch (nodeBinaryOperator.Operator)
			{
			case ":":
				{
					ValueNode nodeValueRight = this.ToValueNode(nodeBinaryOperator.Right);
					IdentifierNode nodeIdentifierLeft = this.ToIdentifierNode(nodeBinaryOperator.Left);
					if (!this.Memory.TryGetValue(nodeIdentifierLeft.Name, out Data? data)) throw new Exception($"Identifier '{nodeIdentifierLeft.Name}' does not exist");
					data.Value = data.Writeable
						? nodeValueRight.Value
						: throw new InvalidOperationException($"Identifier '{nodeIdentifierLeft.Name}' is non-writeable");
					return nodeIdentifierLeft;
				}
			default: throw new ArgumentException($"Unidentified '{nodeBinaryOperator.Operator}' operator");
			}
		}
		else throw new ArgumentException($"Unable to evaluate identifier from {node}");
	}
	public void Evaluate(in IEnumerable<Node> trees)
	{
		foreach (Node nodeTree in trees)
		{
			this.ToValueNode(nodeTree);
		}
	}
	public void Evaluate(in string code)
	{
		this.Evaluate(this.Parse(Tokenize(code)));
	}
}
