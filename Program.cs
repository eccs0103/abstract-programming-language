using APL;

using static APL.Interpreter;

internal partial class Program
{
	private static readonly Interpreter Interpreter = new();
	private static void Debug()
	{
		ConsoleColor foreground = Console.ForegroundColor;
		try
		{
			string input = Console.ReadLine() ?? throw new NullReferenceException($"Input cant be null");
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
	private static void InternalRun()
	{
		ConsoleColor foreground = Console.ForegroundColor;
		try
		{
			string input = Console.ReadLine() ?? throw new NullReferenceException($"Input cant be null");
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
	private static void ExternalRun()
	{
		static void ExternalRunWalker(in DirectoryInfo directory)
		{

			foreach (FileInfo file in directory.GetFiles())
			{
				if (!file.Extension.Equals(".APL", StringComparison.CurrentCultureIgnoreCase)) continue;
				Interpreter.Evaluate(File.ReadAllText(file.FullName));
			}
			foreach (DirectoryInfo subdirectory in directory.GetDirectories())
			{
				ExternalRunWalker(subdirectory);
			}
		}

		try
		{
			ExternalRunWalker(new(Directory.GetCurrentDirectory()));
		}
		catch (Exception exception)
		{
			Console.WriteLine($"{exception.Message}\n");
		}
		finally
		{
			Console.ReadKey();
		}
	}
	private enum RunModes
	{
		Debug,
		Internal,
		External,
	}
	static void Main()
	{
		RunModes mode = RunModes.Internal;
		while (true)
		{
			switch (mode)
			{
			case RunModes.Debug: Debug(); break;
			case RunModes.Internal: InternalRun(); break;
			case RunModes.External: ExternalRun(); return;
			default: throw new ArgumentException($"Unidentified run mode '{nameof(mode)}'");
			}
		}
	}
}