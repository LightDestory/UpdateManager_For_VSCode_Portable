using System;
using System.ComponentModel;
using System.Net;

namespace UpdateManager_For_VSCode_Portable
{
    class Downloader
    {
        public WebClient client = new WebClient();
        private volatile bool _completed;

        public void DownloadFile(string address, string location)
        {
            
            Uri Uri = new Uri(address);
            _completed = false;

            client.DownloadFileCompleted += new AsyncCompletedEventHandler(Completed);

            client.DownloadProgressChanged += new DownloadProgressChangedEventHandler(DownloadProgress);
            client.DownloadFileAsync(Uri, location);

        }

        public bool DownloadCompleted { get { return _completed; } }

        private void DownloadProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            Console.Write("\rDownloaded {0} of {1} KBs. {2} % complete...",
                e.BytesReceived/1024,
                e.TotalBytesToReceive/1024,
                e.ProgressPercentage);
        }

        private void Completed(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Cancelled == true)
            {
                Console.WriteLine("\nDownload has been canceled.\n");
            }
            else
            {
                Console.WriteLine("\nDownload completed!\n");
            }

            _completed = true;
        }
    }
}
