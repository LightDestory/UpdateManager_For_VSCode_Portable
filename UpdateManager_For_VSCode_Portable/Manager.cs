using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Ionic.Zip;
using System.Threading;

namespace UpdateManager_For_VSCode_Portable
{
    class Manager
    {
        private readonly string LauncherINI = @"AppInfo\Launcher\VSCodePortable.ini";
        private readonly string AppInfoINI = @"AppInfo\AppInfo.ini";
        private readonly string Exe = @"VSCode\code.exe";
        private string UpdateFile = "https://az764295.vo.msecnd.net/stable/%COMMIT-CODE%/VSCode-win32-ia32-%VERSION%.zip";
        private string CommitUrl = "https://github.com/Microsoft/vscode/tags";
        public string OnlineVersion = "https://code.visualstudio.com/updates/";
        public string LocalVersion;
        private string[] prefix =  { "href=\"/updates/", "(version ", "<strong>Update " };
        private string[] suffix = { "\" />", ")</h1>" };
        private string dir, temp, downloadPath;
        private Downloader Network;
        private int start, end, max = 0, i;

        public Manager(string dir)
        {
            this.dir = dir;
            downloadPath = dir + "Update.zip";
            Network = new Downloader();
            Init();
        }

        private void Init()
        {
            RetrieveFullUpdateLink();
            RetrieveVersionOnline();
            LocalVersion = RetrieveDataFromFile(AppInfoINI, "LocalVersion");
        }

        public bool CheckForUpdate()
        {
            return !LocalVersion.Equals(OnlineVersion);
        }

        public void Update()
        {
            DownloadUpdate();
            ApplyUpdate();
            Console.WriteLine("Done!");
        }

        private void DownloadUpdate()
        {
            try
            {
                Network.DownloadFile(GetDownloadUrl(), downloadPath);
                while (!Network.DownloadCompleted)
                    Thread.Sleep(1000);
            }
            catch (Exception ex)
            {
                ErrorCall("An Error occured while downloading Visual Studio Code!\nPlease Contact the developer and sent this a screenshot of me!\n\n " + ex.ToString());
            }
        }

        private void ApplyUpdate()
        {
            try
            {
                Console.WriteLine("");
                ZipFile Zipper = new ZipFile(downloadPath);
                int i = 1;
                foreach (ZipEntry file in Zipper)
                {
                    Console.WriteLine("Extracting " + i + "/" + Zipper.Count + ": " + file.FileName);
                    file.Extract(dir + @"VSCode\", ExtractExistingFileAction.OverwriteSilently);
                    i++;
                }
                Zipper.Dispose();
                Console.WriteLine("Extraction completed!\nUpdating Verison file...");
                RefreshLocalVersion();
                Console.WriteLine("Deleting downloaded zip...");
                File.Delete(downloadPath);
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
                Console.WriteLine("No Update found, You are using the latest Visual Studio Code!\nStarting Visual Studio Code...");
                Process.Start(dir + Exe, RetrieveDataFromFile(LauncherINI, "Args"));
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
                string[] temp = File.ReadAllLines(dir + AppInfoINI);
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
                File.WriteAllLines(dir + AppInfoINI, temp);
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

        private void RetrieveFullUpdateLink()
        {
            try
            {
                temp = Network.client.DownloadString(OnlineVersion);
                start = temp.IndexOf(prefix[0]) + prefix[0].Length;
                end = temp.IndexOf(suffix[0]) - start;
                OnlineVersion = OnlineVersion + temp.Substring(start, end);
            }
            catch (Exception ex)
            {
                ErrorCall("An Error occured while retriving VSCode's updates list!\nPlease Contact the developer and sent this a screenshot of me!\n\n " + ex.ToString());
            }
        }

        private void RetrieveVersionOnline()
        {
            try
            {
                string temp = Network.client.DownloadString(OnlineVersion);
                if (temp.LastIndexOf(prefix[2]) == -1)
                {
                    start = temp.IndexOf(prefix[1]) + prefix[1].Length;
                    end = temp.IndexOf(suffix[1]) - start;
                    OnlineVersion = temp.Substring(start, end) + ".0";
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
            catch (Exception ex)
            {
                ErrorCall("An Error occured while retriving VSCode's latest version from website!\nPlease Contact the developer and sent this a screenshot of me!\n\n " + ex.ToString());
            }
        }

        private string GetDownloadUrl()
        {
            string commit = "";
            try
            {
                string materials = Network.client.DownloadString(CommitUrl);
                commit = Regex.Match(materials, "<a href=\"/Microsoft/vscode/commit/.{40}\">").Value.Replace("<a href=\"/Microsoft/vscode/commit/", "").Replace("\">", "");
            }
            catch (Exception ex)
            {
                ErrorCall("An Error occured while retriving VSCode's update file url!\nPlease Contact the developer and sent this a screenshot of me!\n\n " + ex.ToString());
            }
            return UpdateFile.Replace("%COMMIT-CODE%", commit).Replace("%VERSION%", OnlineVersion);
        }

        public string GetLocalVersion => LocalVersion;

        public string GetOnlineVersion => OnlineVersion;

        private void ErrorCall(string message)
        {
            Console.Error.WriteLine(message);
            Console.Error.WriteLine("\n\nPress any key to close...");
            Console.ReadKey();
            Environment.Exit(0);
        }
    }
}
