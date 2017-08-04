using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Windows.Forms;
using Ionic.Zip;
using System.Collections.Generic;

namespace UpdateManager_For_VSCode_Portable
{
    class Manager
    {
        private readonly string LauncherINI = @"AppInfo\Launcher\VSCodePortable.ini";
        private readonly string AppInfoINI = @"AppInfo\AppInfo.ini";
        private readonly string Exe = @"VSCode\code.exe";
        private string UpdateFile = "https://code.visualstudio.com/docs?dv=winzip";
        public string OnlineVersion = "https://code.visualstudio.com/updates/";
        public string LocalVersion;
        private string[] prefix =  { "href=\"/updates/", "(version ", "<strong>Update " };
        private string[] suffix = { "\" />", ")</h1>" };
        private string dir;
        private WebClient Net;
        private OpenFileDialog Select;
        private List<String> TempList;

        public Manager(string dir)
        {
            this.dir = dir;
            Net = new WebClient();
            Select = new OpenFileDialog();
            TempList = new List<string>();
            Select.Filter = "ZIP File(.zip) | *.zip";
            Init();
        }

        private void Init()
        {
            RetrieveVersionOnline();
            LocalVersion = RetrieveDataFromFile(AppInfoINI, "LocalVersion");
        }

        public bool CheckForUpdate()
        {
            return !LocalVersion.Equals(OnlineVersion);
        }

        private string RetrieveDataFromFile(string FilePath, string type)
        {
            string data = "";
            try
            {
                string[] lines = File.ReadAllLines(dir+FilePath);
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
                for (int i = 0; i < 2; i++)
                {
                    string temp = Net.DownloadString(OnlineVersion);
                    int start, end, max = 0;
                    if (i == 0)
                    {
                        start = temp.IndexOf(prefix[0]) + prefix[0].Length;
                        end = temp.IndexOf(suffix[0]) - start;
                        OnlineVersion = OnlineVersion + temp.Substring(start, end);
                    }
                    else
                    {
                        if (temp.LastIndexOf(prefix[2]) == -1)
                        {
                            start = temp.IndexOf(prefix[1]) + prefix[1].Length;
                            end = temp.IndexOf(suffix[1]) - start;
                            OnlineVersion = temp.Substring(start, end);
                        }
                        else
                        {
                            foreach (Match find in Regex.Matches(temp, @"<p><strong>Update \d+.\d+.\d+</strong>"))
                            {
                                string RegExResult = find.Value.Replace("<p><strong>Update ", "").Replace("</strong>", "");
                                int NumVersion = Int32.Parse(RegExResult.Replace(".", ""));
                                if (NumVersion > max)
                                {
                                    max = NumVersion;
                                    OnlineVersion = RegExResult;
                                }
                            }
                        }
                    }
                }
            } catch(Exception ex)
            {
                ErrorCall("An Error occured while retriving VSCode's latest version from website!\nPlease Contact the developer and sent this a screenshot of me!\n\n " + ex.ToString());
            }
        }

        private void ErrorCall(string message)
        {
            Console.Error.WriteLine(message);
            Console.Error.WriteLine("\n\nPress any key to close...");
            Console.ReadKey();
            Environment.Exit(0);
        }

        public void StartProgram()
        {
            try
            {
                Console.WriteLine("starting Visual Studio Code...");
                Process.Start(dir + Exe, RetrieveDataFromFile(LauncherINI, "Args"));
            } catch (Exception ex)
            {
                ErrorCall("An Error occured while starting Visual Studio Code!\nPlease Contact the developer and sent this a screenshot of me!\n\n " + ex.ToString());
            }
        }

        public void Update()
        {
            ZipFile Zipper = new ZipFile(GetDownloadedFile());
            foreach(ZipEntry file in Zipper)
            {
                Console.WriteLine("Extracting... " + file.FileName);
                file.Extract(dir + @"VSCode\", ExtractExistingFileAction.OverwriteSilently);
            }
            Console.Clear();
            Console.WriteLine("Extraction completed!\nUpdating Verison file...");
            RefreshLocalVersion();
            Console.WriteLine("Done!");
        }

        private string GetDownloadedFile()
        {
            try
            {
                Console.WriteLine("You need to download the file using your own browser...\nOpening Direct download link, you just need to save the file!");
                Process.Start(UpdateFile);
                if (Select.ShowDialog() != DialogResult.OK)
                {
                    Console.WriteLine("Aborted");
                    Console.ReadKey();
                    Environment.Exit(0);
                }
            } catch(Exception ex)
            {
                ErrorCall("An Error occured while download Visual Studio Code!\nPlease Contact the developer and sent this a screenshot of me!\n\n " + ex.ToString());
            }
            return Select.FileName;
        }
        
        private void RefreshLocalVersion()
        {
            string[] temp = File.ReadAllLines(dir + AppInfoINI);
            foreach(String s in temp)
            {
                if (s.Contains("PackageVersion"))
                {
                    TempList.Add("PackageVersion=" + OnlineVersion + ".0");
                }
                else if (s.Contains("DisplayVersion"))
                {
                    TempList.Add("DisplayVersion=" + OnlineVersion);
                }
                else
                {
                    TempList.Add(s);
                }
            }
            File.WriteAllLines(dir + AppInfoINI, TempList.ToArray());
        }

        public String GetLocalVersion => LocalVersion;

        public String GetOnlineVersion => OnlineVersion;
    }
}
