using System;

using AdaptiveCore.ANL;

internal class Program
{
	static void Main(string[] args)
	{
		while (true)
		{
			try
			{
				Console.Write("> ");
				var expression = Console.ReadLine();
				if (expression == null)
					throw new ArgumentNullException("The entered code can't be null.");
				var interpreter = new Interpreter();
				var tokens = interpreter.Tokenize(expression);
				var tree = interpreter.Parse(tokens);
				Console.WriteLine(tree);
				var result = interpreter.Evaluate(tree);
				Console.WriteLine($"< {result}");
			}
			catch (Exception exception)
			{
				Console.WriteLine($"{exception.Message}");
				continue;
			}
		}
	}
}
