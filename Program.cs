using APL;

using static APL.Interpreter;

internal partial class Program
{
	private static IEnumerable<string> GenerateInstructions(IEnumerable<string> initial)
	{
		foreach (string instruction in initial)
		{
			string input = $"import \"{instruction}\";";
			Console.WriteLine(input);
			yield return input;
		}
		while (true)
		{
			yield return Console.ReadLine() ?? throw new NullReferenceException($"Input cant be null");
		}
	}
	private static void Main(string[] args)
	{
		Console.ForegroundColor = ConsoleColor.White;
		Interpreter interpreter = new(new(RunModes.Run));
		foreach (string instruction in GenerateInstructions(args))
		{
			interpreter.Run(instruction);
		}
	}
}