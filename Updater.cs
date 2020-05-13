using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Alan {
    class Updater {

        private int CurrentVersion, LatestVersion;
        private string GithubApplicationInfoUrl, InstallDirectory;

        private Dictionary<string, string> RequiredFiles = new Dictionary<string, string>();
        private bool FilesMissing = false;

        public Updater(string GithubApplicationInfoUrl, string InstallDirectory, int CurrentVersion) {

            if (!GithubApplicationInfoUrl.EndsWith("/")) GithubApplicationInfoUrl += "/";
            if (!InstallDirectory.EndsWith("\\")) InstallDirectory += "\\";

            this.GithubApplicationInfoUrl = GithubApplicationInfoUrl;
            this.InstallDirectory = InstallDirectory;
            this.CurrentVersion = CurrentVersion;

            if (!Directory.Exists(InstallDirectory)) {
                Directory.CreateDirectory(InstallDirectory);
            }

            PerformCheck();
        }

        public void SetRequiredFiles(string[] Files) {
            foreach (string F in Files) {
                if (!File.Exists(InstallDirectory + F)) {
                    FilesMissing = true;
                    return;
                }
            }
        }

        private void PerformCheck() {

            using (WebClient wc = new WebClient()) {

                string Response = wc.DownloadString(GithubApplicationInfoUrl + "application.json?time=" + DateTimeOffset.Now.ToUnixTimeMilliseconds());

                string[] Lines = Response.Split('\n');

                foreach (string Line in Lines) {
                    if (Line.Length < 1) continue;

                    JSONElement Json = JSON.Parse(Line);
                    string Type = Json.c["type"].ToString();

                    switch (Type) {
                        case "application.info":
                            LatestVersion = Json.c["latest-version"].ToInt();
                            break;
                        case "application.required.file":
                            RequiredFiles.Add(Json.c["remote-name"].ToString(), Json.c["name"].ToString());
                            break;
                    }

                    Console.WriteLine($"Type: {Json.c["type"].ToString()}");
                }



            }

        }

        public bool UpdateExists() => CurrentVersion < LatestVersion || FilesMissing;

        public void InstallUpdate() {

            // DOWNLOAD ALL REQUIRED FILES.
            try {
                using (WebClient wc = new WebClient()) {
                    foreach (string RemoteFileName in RequiredFiles.Keys) {
                        string FileName = RequiredFiles[RemoteFileName];

                        try {
                            if (!File.Exists(InstallDirectory + FileName) || FilesMissing) {
                                Console.WriteLine($"Downloading {FileName} from {GithubApplicationInfoUrl + RemoteFileName}");
                                wc.DownloadFile(GithubApplicationInfoUrl + RemoteFileName + "?t=" + DateTimeOffset.Now.ToUnixTimeMilliseconds(), InstallDirectory + RemoteFileName);

                                Console.WriteLine($"{FileName} Downloaded.");

                                // EXTRACT IT IF IT'S ZIPPED
                                if (FileName.EndsWith(".zip")) {
                                    Console.WriteLine($"Extracting {FileName}...");
                                    ZipFile.ExtractToDirectory(InstallDirectory + FileName, InstallDirectory);
                                }
                                Console.WriteLine($"Completed {FileName}.");
                            }
                        }
                        catch {
                            Console.WriteLine($"Remote file {RemoteFileName} not found. It will be skipped.");
                        }
                    }

                }
            }
            catch { }

            // START AN UPDATE COMMAND BATCH
            if (File.Exists(InstallDirectory + "update.bat")) {
                Process.Start(new ProcessStartInfo() {
                    FileName = InstallDirectory + "update.bat",
                    WindowStyle = ProcessWindowStyle.Hidden
                });
                Environment.Exit(0);
            }

        }

        public int GetLatestVersion() {
            return LatestVersion;
        }

    }
}
