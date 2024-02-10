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
				get
				{
					return this.value;
				}
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
						return this.ToValueNode(this.ToRegisterNode(nodeBinaryOperator));
					}
				default: throw new ArgumentException($"Unidentified '{nodeBinaryOperator.Operator}' operator");
				}
			}
			else throw new ArgumentException($"Unable to evaluate value from {node}");
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
							return this.memory.TryAdd(nodeRegisterTarget.Address, new Data(null)) ?
								nodeRegisterTarget :
								throw new ArgumentException($"Identifier '{nodeRegisterTarget.Address}' already exists");
						}
						else throw new ArgumentException($"Identifier expected");
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
						RegisterNode nodeRegisterLeft = this.ToRegisterNode(nodeBinaryOperator.Left);
						if (!this.memory.TryGetValue(nodeRegisterLeft.Address, out Data? data)) throw new Exception($"Identifier '{nodeRegisterLeft.Address}' does not exist");
						data.Value = data.Writeable
							? nodeValueRight.Value
							: throw new InvalidOperationException($"Identifier '{nodeRegisterLeft.Address}' is non-writeable");
						return nodeRegisterLeft;
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
