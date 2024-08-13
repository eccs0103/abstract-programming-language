using APL;

using static APL.Interpreter;

internal partial class Program
{
	private static readonly Interpreter Interpreter = new();
	private static void Debug(in string input)
	{
		ConsoleColor foreground = Console.ForegroundColor;
		try
		{
			Console.WriteLine();
			Console.ForegroundColor = ConsoleColor.Yellow;
			Token[] tokens = Tokenize(input);
			if (tokens.Length > 0)
			{
				Console.WriteLine($"{string.Join<Token>('\n', tokens)}\n");
			}
			IEnumerable<Node> trees = Interpreter.Parse(tokens);
			if (trees.Any())
			{
				Console.WriteLine($"{string.Join('\n', trees)}\n");
			}
			Interpreter.Evaluate(trees);
		}
		catch (Exception exception)
		{
			Console.WriteLine($"{exception.Message}\n");
		}
		finally
		{
			Console.ForegroundColor = foreground;
		}
	}
	private static void Run(in string input)
	{
		ConsoleColor foreground = Console.ForegroundColor;
		try
		{
			Console.WriteLine();
			Console.ForegroundColor = ConsoleColor.Yellow;
			Interpreter.Evaluate(input);
		}
		catch (Exception exception)
		{
			Console.WriteLine($"{exception.Message}\n");
		}
		finally
		{
			Console.ForegroundColor = foreground;
		}
	}
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
	private enum RunModes: byte
	{
		Debug,
		Run,
	}
	static void Main(string[] args)
	{
		RunModes mode = RunModes.Run;
		foreach (string instruction in GenerateInstructions(args))
		{
			switch (mode)
			{
			case RunModes.Debug: Debug(instruction); break;
			case RunModes.Run: Run(instruction); break;
			default: throw new ArgumentException($"Unidentified run mode '{nameof(mode)}'");
			}
		}
	}
}