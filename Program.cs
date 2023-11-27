using AdaptiveCore.ANL;

using static AdaptiveCore.ANL.Interpreter;

internal class Program
{
	static void Main()
	{
		Interpreter interpreter = new();
		while (true)
		{
			try
			{
				string input = Console.ReadLine() ?? throw new NullReferenceException($"Input cant be null");
				//Token[] tokens = interpreter.Tokenize(input);
				//Console.WriteLine(string.Join<Token>("\n", tokens));
				//Node tree = interpreter.Parse(tokens);
				//Console.WriteLine(tree);
				//double? result = interpreter.Evaluate(tree);
				double? result = interpreter.Evaluate(input);
				//Console.WriteLine($"{result}");
			}
			catch (Exception exception)
			{
				Console.WriteLine($"{exception.Message}");
				continue;
			}
		}
	}
}