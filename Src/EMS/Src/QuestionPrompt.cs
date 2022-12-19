using System;

namespace EMS
{
    public enum CLIPrompt_Response
    {
        Yes,
        No
    }

    public static class CliPrompt
    {
        public static string UserInput(string text)
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(text);
			Console.ForegroundColor = ConsoleColor.White;

            return Console.ReadLine();

        }

        public static CLIPrompt_Response Question(string text)
        {
            text += " (Yes/No)";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(text);
			Console.ForegroundColor = ConsoleColor.White;

            while (true)
			{
				string selection = Console.ReadLine().ToLower();
                switch (selection)
                {
                    case "yes":
                    case "y":
                        return CLIPrompt_Response.Yes;
                    case "no":
                    case "n":
                        return CLIPrompt_Response.No;
                }

                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("Invalid option.");
                Console.WriteLine(text);
                Console.ForegroundColor = ConsoleColor.White;
            }
        }
    }
}