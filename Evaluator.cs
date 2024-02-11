using static System.Math;

namespace ALM
{
	internal partial class Interpreter
	{
		private class DataInitializer
		{
			public String Type { get; set; } = "Any";
			public Boolean Writeable { get; set; } = true;
		}
		private class Data
		{
			public Data(Double? value, DataInitializer? initializer = null)
			{
				DataInitializer mainInitializer = initializer ?? new();
				this.Type = mainInitializer.Type;
				this.Writeable = mainInitializer.Writeable;
				this.value = value;
			}
			public readonly String Type;
			public readonly Boolean Writeable;
			private Double? value;
			public Double? Value
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
		private readonly Dictionary<String, Data> memory = new()
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
				return this.memory.TryGetValue(nodeIdentifier.Address, out Data? data)
					? new ValueNode(data.Value)
					: throw new Exception($"Identifier '{nodeIdentifier.Address}' does not exist");
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
				case "print":
					{
						ValueNode nodeValue2 = this.ToValueNode(nodeUnaryOperator.Target);
						Console.WriteLine(nodeValue2.Value);
						return ValueNode.Null;
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
						Double dataLeft = this.ToValueNode(nodeBinaryOperator.Left).Value ?? throw new NullReferenceException($"Operator '{nodeBinaryOperator.Operator}' cannot be applied to null operand"); ;
						Double dataRight = this.ToValueNode(nodeBinaryOperator.Right).Value ?? throw new NullReferenceException($"Operator '{nodeBinaryOperator.Operator}' cannot be applied to null operand"); ;
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
							? this.memory.TryAdd(nodeIdentifierTarget.Address, new Data(null))
								? nodeIdentifierTarget
								: throw new ArgumentException($"Identifier '{nodeIdentifierTarget.Address}' already exists")
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
						if (!this.memory.TryGetValue(nodeIdentifierLeft.Address, out Data? data)) throw new Exception($"Identifier '{nodeIdentifierLeft.Address}' does not exist");
						data.Value = data.Writeable
							? nodeValueRight.Value
							: throw new InvalidOperationException($"Identifier '{nodeIdentifierLeft.Address}' is non-writeable");
						return nodeIdentifierLeft;
					}
				default: throw new ArgumentException($"Unidentified '{nodeBinaryOperator.Operator}' operator");
				}
			}
			else throw new ArgumentException($"Unable to evaluate identifier from {node}");
		}
		private void Evaluate(in Node node)
		{
			this.ToValueNode(node);
		}
		public void Evaluate(in Node[] trees)
		{
			foreach (Node tree in trees)
			{
				this.Evaluate(tree);
			}
		}
		public void Evaluate(in String code)
		{
			this.Evaluate(this.Parse(this.Tokenize(code)));
		}
	}
}
