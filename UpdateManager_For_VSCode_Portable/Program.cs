using System;

namespace UpdateManager_For_VSCode_Portable
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Manager App = new Manager(AppDomain.CurrentDomain.BaseDirectory);
            if (App.CheckForUpdate())
            {
                Console.WriteLine("A new update is available!\nLocal Version: " + App.GetLocalVersion +"\nLatest Version: " + App.GetOnlineVersion);
                Console.WriteLine("Do you want to updare now? (y/n)");
                if (RequestInput().Equals("y"))
                {
                    App.Update();
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
