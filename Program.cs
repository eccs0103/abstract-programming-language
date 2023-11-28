using System.IO;
using System.Text.RegularExpressions;

using AdaptiveCore.ANL;

using static AdaptiveCore.ANL.Interpreter;

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
				//string input = Console.ReadLine() ?? throw new NullReferenceException($"Input cant be null");
				//Token[] tokens = interpreter.Tokenize(input);
				//Console.WriteLine(string.Join<Token>("\n", tokens));
				//Node[] trees = interpreter.Parse(tokens);
				//Console.WriteLine(string.Join<Node>("\n", trees));
				//_ = interpreter.Evaluate(trees);
				#endregion
				#region External
				//string input = Console.ReadLine() ?? throw new NullReferenceException($"Input cant be null");
				//Match match = instructions.Match(input);
				//if (!match.Success) throw new FormatException($"Invalid instruction");
				//Group function = match.Groups[1];
				//Group parameter1 = match.Groups[2];
				//switch (function.Value)
				//{
				//	case "run":
				//	{
				//		FileInfo file = new(parameter1.Value);
				//		if (file.Exists)
				//		{
				//			Console.WriteLine($"Running {file.Name}");
				//			string content = File.ReadAllText(file.FullName);
				//			_ = interpreter.Evaluate(input);
				//		}
				//		else throw new NullReferenceException($"File {file.Name} in {file.Directory?.FullName} doesn't exist");
				//	}
				//	break;
				//	default: continue;
				//}
				#endregion
				#region Internal
				string input = Console.ReadLine() ?? throw new NullReferenceException($"Input cant be null");
				_ = interpreter.Evaluate(input);
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