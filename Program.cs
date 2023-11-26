using AdaptiveCore.ANL;

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
				double result = interpreter.Evaluate(input);
				Console.WriteLine(result);
			}
			catch (Exception exception)
			{
				Console.WriteLine($"{exception.Message}");
				continue;
			}
		}
	}
}