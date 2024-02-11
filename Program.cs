using System.Text.RegularExpressions;

using ALM;

using static ALM.Interpreter;

internal partial class Program
{
	private enum RunModes
	{
		Debug,
		Internal,
		External,
	}
	static void Main()
	{
		RunModes mode = RunModes.Internal;
		Interpreter interpreter = new();
		Regex instructions = ExternalInstructionPattern();
		while (true)
		{
			try
			{
				String input = Console.ReadLine() ?? throw new NullReferenceException($"Input cant be null");
				switch (mode)
				{
				case RunModes.Debug:
					{
						Token[] tokens = interpreter.Tokenize(input);
						Console.WriteLine($"{String.Join<Token>("\n", tokens)}\n");
						Node[] trees = interpreter.Parse(tokens);
						Console.WriteLine($"{String.Join<Node>("\n", trees)}\n");
						interpreter.Evaluate(trees);
					}
					break;
				case RunModes.Internal:
					{
						Match match = instructions.Match(input);
						if (!match.Success) throw new FormatException($"Invalid instruction");
						Group function = match.Groups[1];
						Group parameter1 = match.Groups[2];
						switch (function.Value)
						{
						case "run":
							{
								FileInfo file = new(parameter1.Value);
								if (file.Exists)
								{
									Console.WriteLine($"Running {file.Name}");
									String content = File.ReadAllText(file.FullName);
									interpreter.Evaluate(input);
								}
								else throw new NullReferenceException($"File {file.Name} in {file.Directory?.FullName} doesn't exist");
							}
							break;
						default: continue;
						}
					}
					break;
				case RunModes.External:
					{
						interpreter.Evaluate(input);
					}
					break;
				default: throw new ArgumentException($"Unidentified run mode '{nameof(mode)}'");
				}
			}
			catch (Exception exception)
			{
				Console.WriteLine($"{exception.Message}");
				continue;
			}
		}
	}

	[GeneratedRegex(@"(\w+) (\S+)", RegexOptions.Compiled)]
	private static partial Regex ExternalInstructionPattern();
}