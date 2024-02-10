using System.Text.RegularExpressions;

using ALM;

using static ALM.Interpreter;

internal class Program
{
	static void Main()
	{
		Interpreter interpreter = new();
		Regex instructions = new(@"(\w+) (\S+)", RegexOptions.Compiled);
		while (true)
		{
			try
			{
				#region Debug
				//String input = Console.ReadLine() ?? throw new NullReferenceException($"Input cant be null");
				//Token[] tokens = interpreter.Tokenize(input);
				//Console.WriteLine(String.Join<Token>("\n", tokens));
				//Node[] trees = interpreter.Parse(tokens);
				//Console.WriteLine(String.Join<Node>("\n", trees));
				//interpreter.Evaluate(trees);
				#endregion
				#region External
				//String input = Console.ReadLine() ?? throw new NullReferenceException($"Input cant be null");
				//Match match = instructions.Match(input);
				//if (!match.Success) throw new FormatException($"Invalid instruction");
				//Group function = match.Groups[1];
				//Group parameter1 = match.Groups[2];
				//switch (function.Value)
				//{
				//case "run":
				//	{
				//		FileInfo file = new(parameter1.Value);
				//		if (file.Exists)
				//		{
				//			Console.WriteLine($"Running {file.Name}");
				//			String content = File.ReadAllText(file.FullName);
				//			interpreter.Evaluate(input);
				//		}
				//		else throw new NullReferenceException($"File {file.Name} in {file.Directory?.FullName} doesn't exist");
				//	}
				//	break;
				//default: continue;
				//}
				#endregion
				#region Internal
				interpreter.Evaluate(Console.ReadLine() ?? throw new NullReferenceException($"Input cant be null"));
				#endregion
			}
			catch (Exception exception)
			{
				Console.WriteLine($"{exception.Message}");
				continue;
			}
		}
	}
}