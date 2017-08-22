using System;

namespace UpdateManager_For_VSCode_Portable
{
    class Program
    {
        static void Main(string[] args)
        {
            Manager App = new Manager();
            if (App.GetConnection())
            {
                App.Init();
                if (App.CheckForUpdate())
                {
                    App.WriteLineColored(ConsoleColor.White, ConsoleColor.Black, "\tA new update is available!\t");
                    App.WriteLineColored(ConsoleColor.Green, ConsoleColor.Blue, "\t---------------------------\t");
                    App.WriteLineColored(ConsoleColor.White, ConsoleColor.Black, "Local Version:  " + App.GetLocalVersion);
                    App.WriteLineColored(ConsoleColor.Yellow, ConsoleColor.Blue, "Latest Version: " + App.GetOnlineVersion);
                    App.WriteLineColored(ConsoleColor.Green, ConsoleColor.Blue, "\t---------------------------\t");
                    Console.WriteLine("Do you want to updare now? (y/n)");
                    if (RequestInput().Equals("y"))
                    {
                        App.Update();
                    }
                }
            }
            App.StartProgram();
        }

        public static String RequestInput()
        {
            string answer;
            do
            {
                answer = Console.ReadLine();
                answer = answer.ToLower();
            } while (!(answer.Equals("y") || answer.Equals("n")));
            return answer;
        }
    }
}
