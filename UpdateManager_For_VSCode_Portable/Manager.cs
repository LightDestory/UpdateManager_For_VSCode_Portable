using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Ionic.Zip;
using System.Threading;
using System.Collections.Generic;
using System.Net.NetworkInformation;

namespace UpdateManager_For_VSCode_Portable
{
    class Manager
    {
        private Dictionary<string, string> files = new Dictionary<string, string>()
        {
            {"LauncherINI", @"AppInfo\Launcher\VSCodePortable.ini" },
            {"AppInfoINI", @"AppInfo\AppInfo.ini" },
            {"App32", @"VSCode\code.exe" },
            {"App64", @"VSCode64\code.exe" }
        };
        private string[] ExtractPath = { @"VSCode\", @"VSCode64\" };
        private string[] UpdateFile =
        {
            "https://az764295.vo.msecnd.net/stable/%COMMIT-CODE%/VSCode-win32-ia32-%VERSION%.zip",
            "https://az764295.vo.msecnd.net/stable/%COMMIT-CODE%/VSCode-win32-x64-%VERSION%.zip"
        };
        private string CommitUrl = "https://github.com/Microsoft/vscode/tags";
        private string LocalVersion, OnlineVersion, dir, temp, commit = "";
        private Downloader Network;
        private int i;

        public Manager()
        {
            dir = AppDomain.CurrentDomain.BaseDirectory;
            Network = new Downloader();
        }

        public void Init()
        {
            RetrieveVersionOnline();
            LocalVersion = RetrieveDataFromFile(files["AppInfoINI"], "LocalVersion");
        }

        public bool CheckForUpdate()
        {
            return !LocalVersion.Equals(OnlineVersion);
        }

        public void Update()
        {
            for(i = 0; i<2; i++)
            {
                DownloadUpdate(i);
                ApplyUpdate(i);
            }
            WriteLineColored(ConsoleColor.White, ConsoleColor.Blue, "Updating Verison file...");
            RefreshLocalVersion();
            WriteLineColored(ConsoleColor.Yellow, ConsoleColor.Green, "Done!");
        }

        private void DownloadUpdate(int id)
        {
            try
            {
                if (id == 0)
                {
                    WriteLineColored(ConsoleColor.White, ConsoleColor.Blue, "Downloading 32bit zip...");
                }
                else
                {
                    WriteLineColored(ConsoleColor.White, ConsoleColor.Blue, "Downloading 64bit zip...");
                }
                Network.DownloadFile(GetDownloadUrl(id), dir + id + ".zip");
                while (!Network.DownloadCompleted)
                    Thread.Sleep(1000);
            }
            catch (Exception ex)
            {
                ErrorCall("An Error occured while downloading Visual Studio Code!\nPlease Contact the developer and sent this a screenshot of me!\n\n " + ex.ToString());
            }
        }

        private void ApplyUpdate(int id)
        {
            try
            {
                Console.WriteLine("");
                ZipFile Zipper = new ZipFile(dir + id + ".zip");
                int i = 1;
                foreach (ZipEntry file in Zipper)
                {
                    Console.WriteLine("Extracting " + i + "/" + Zipper.Count + ": " + file.FileName);
                    file.Extract(dir + ExtractPath[id], ExtractExistingFileAction.OverwriteSilently);
                    i++;
                }
                Zipper.Dispose();
                WriteLineColored(ConsoleColor.White, ConsoleColor.Blue, "Extraction completed!\nDeleting downloaded zip...");
                File.Delete(dir + id + ".zip");
            }
            catch(Exception ex)
            {
                ErrorCall("An Error occured while applying Update!\nPlease Contact the developer and sent this a screenshot of me!\n\n " + ex.ToString());
            }
        }

        public void StartProgram()
        {
            try
            {
                WriteLineColored(ConsoleColor.White, ConsoleColor.Blue, "No Update found, You are using the latest Visual Studio Code!\nStarting Visual Studio Code...");
                if (Environment.Is64BitOperatingSystem)
                {
                    Process.Start(dir + files["App64"], RetrieveDataFromFile(files["LauncherINI"], "Args"));
                }
                else
                {
                    Process.Start(dir + files["App32"], RetrieveDataFromFile(files["LauncherINI"], "Args"));
                }
            }
            catch (Exception ex)
            {
                ErrorCall("An Error occured while starting Visual Studio Code!\nPlease Contact the developer and sent this a screenshot of me!\n\n " + ex.ToString());
            }
        }

        private void RefreshLocalVersion()
        {
            try
            {
                string[] temp = File.ReadAllLines(dir + files["AppInfoINI"]);
                for (i = 0; i < temp.Length; i++)
                {
                    if (temp[i].Contains("PackageVersion"))
                    {
                        temp[i] = "PackageVersion=" + OnlineVersion + ".0";
                    }
                    else if (temp[i].Contains("DisplayVersion"))
                    {
                        temp[i] = "DisplayVersion=" + OnlineVersion;
                    }
                }
                File.WriteAllLines(dir + files["AppInfoINI"], temp);
            }
            catch (Exception ex)
            {
                ErrorCall("An Error occured while updating AppInfo file!\nPlease Contact the developer and sent this a screenshot of me!\n\n " + ex.ToString());
            }
        }

        private string RetrieveDataFromFile(string FilePath, string type)
        {
            string data = "";
            try
            {
                string[] lines = File.ReadAllLines(dir + FilePath);
                switch (type)
                {
                    case "Args":
                        foreach (String line in lines)
                        {
                            if (line.Contains("CommandLineArguments"))
                            {
                                data = line.Replace("CommandLineArguments='", "").Replace("%PAL:DataDir%", dir.Replace(@"App\", "Data")).Replace("'", "");
                                break;
                            }
                        }
                        break;
                    case "LocalVersion":
                        foreach (String line in lines)
                        {
                            if (line.Contains("DisplayVersion"))
                            {
                                data = line.Replace("DisplayVersion=", "");
                                break;
                            }
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                ErrorCall("An Error occured while retriving VSCode's " + type + "!\nPlease Contact the developer and sent this a screenshot of me!\n\n " + ex.ToString());
            }
            return data;
        }

        private void RetrieveVersionOnline()
        {
            try
            {
                string temp = Network.client.DownloadString(CommitUrl);
                OnlineVersion = Regex.Match(temp, "<span class=\"tag-name\">.{6}</span>").Value.Replace("<span class=\"tag-name\">", "").Replace("</span>", "");

            }
            catch (Exception ex)
            {
                ErrorCall("An Error occured while retriving VSCode's latest version from website!\nPlease Contact the developer and sent this a screenshot of me!\n\n " + ex.ToString());
            }
        }

        public bool GetConnection()
        {
            try
            {
                Ping myPing = new Ping();
                PingReply reply = myPing.Send("23.221.227.186", 1000, new byte[32], new PingOptions());
                // 23.221.227.186 = www.visualstudio.com
                return true;
            }
            catch
            {
                return false;
            }
        }

        private string GetDownloadUrl(int id)
        {
            if (commit.Length == 0)
            {
                try
                {
                    string materials = Network.client.DownloadString(CommitUrl);
                    commit = Regex.Match(materials, "<a href=\"/Microsoft/vscode/commit/.{40}\">").Value.Replace("<a href=\"/Microsoft/vscode/commit/", "").Replace("\">", "");
                }
                catch (Exception ex)
                {
                    ErrorCall("An Error occured while retriving VSCode's update file url!\nPlease Contact the developer and sent this a screenshot of me!\n\n " + ex.ToString());
                }
            }
            return UpdateFile[id].Replace("%COMMIT-CODE%", commit).Replace("%VERSION%", OnlineVersion);
        }

        public string GetLocalVersion => LocalVersion;

        public string GetOnlineVersion => OnlineVersion;

        private void ErrorCall(string message)
        {
            Console.Error.WriteLine(message + "\n");
            WriteLineColored(ConsoleColor.Red, ConsoleColor.Black, "Press any key to close...");
            Console.ReadKey();
            Environment.Exit(0);
        }

        public void WriteLineColored(ConsoleColor Background, ConsoleColor FontColor, String Text)
        {
            Console.BackgroundColor = Background;
            Console.ForegroundColor = FontColor;
            Console.Out.WriteLine(" " + Text + " ");
            Console.ResetColor();
        }
    }
}
