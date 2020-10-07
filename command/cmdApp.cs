using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Alan.program;
using SuperWebSocket;

namespace Alan.command {
    class cmdApp {

        public static string Answer(WebSocketSession client, string line) {
            string response = "";

            string subc = Command.AnySubcommand(line,
                "list-update",
                "list",
                "install",
                "scan",
                "start",
                "delete"
            );

            switch (subc) {
                case "list-update":
                    return UpdateList(client);
                case "list":
                    return GetAppList(client);
                case "install":
                    return InstallApp(client, line);
                case "scan":
                    return ScanApps(client, line);
                case "start":
                    return StartApp(client, line);
            }

            return response;
        }

        public static string InstallApp(WebSocketSession client, string line) {
            using (WebClient wc = new WebClient()) {
                string[] lines = File.ReadAllLines(ProgramData.Directory + "bin\\app-list.txt");
                foreach (string l in lines) {
                    JSONElement e = JSON.Parse(line);
                    if (e.c["keyword"].ToString().Equals(Command.GetString(line, "name").ToLower())) {

                        Stream stream = wc.OpenRead(e.c["url"].ToString());
                        int filesize = Convert.ToInt32(wc.ResponseHeaders["Content-Length"]);
                        stream.Dispose();

                        wc.DownloadFileAsync(new Uri(e.c["url"].ToString()), ProgramData.Directory + "bin\\downloads\\" + e.c["bin"].ToString());
                        bool done = false;

                        client.Send($"Ukupna velicina: {filesize / (1024 * 1024)}.{("" + filesize % (1024 * 1024)).Substring(0, 2)}MB");

                        wc.DownloadFileCompleted += (o, s) => done = true;

                        while (!done) {
                            client.Send($"Preuzeto: {(int)(new FileInfo(ProgramData.Directory + "bin\\downloads\\" + e.c["bin"].ToString()).Length / (double)filesize * 100)}%");
                            Thread.Sleep(500);
                        };

                        Console.WriteLine();
                        client.Send("Cekam da se instalacija zavrsi...");

                        Process.Start(new ProcessStartInfo() {
                            FileName = ProgramData.Directory + "bin\\downloads\\" + e.c["bin"].ToString()
                        }).WaitForExit();

                        client.Send("Brisem setup");
                        Thread.Sleep(2000);
                        File.Delete(ProgramData.Directory + "downloads\\" + e.c["bin"].ToString());

                        client.Send("Instalacija zavrsena");

                        break;
                    }
                }
            }
            return "";
        }

        public static string UpdateList(WebSocketSession client) {

            client.Send("Preuzimam linkove...");
            using (WebClient wc = new WebClient()) {
                wc.DownloadFile("http://alantr7.uwebweb.com/alan/terminal/app-list.txt?_=" + DateTimeOffset.Now.ToUnixTimeMilliseconds(), ProgramData.Directory + "bin\\app-list.txt");
            }

            return "Linkovi uspjesno preuzeti";

        }

        public static string GetAppList(WebSocketSession client) {

            string response = "";

            string[] lines = File.ReadAllLines(ProgramData.Directory + "bin\\app-list.txt");
            List<string> installed = new List<string>();

            foreach (string l in File.ReadAllLines(ProgramData.Directory + "bin\\app-installed.txt")) {
                JSONElement json = JSON.Parse(l);
                installed.Add(json.c["keyword"].ToString());
            }

            response += "\t§8NAZIV\t\t\tNAZIV PROGRAMA\t\tBIN\n";
            foreach (string line in lines) {
                JSONElement e = JSON.Parse(line);
                string output = $"\t{e.c["keyword"].ToString()}";

                string tabs;
                if (e.c["keyword"].ToString().Length < 8)
                    tabs = "\t\t\t";
                else tabs = "\t\t";

                output += tabs + e.c["name"].ToString();

                if (e.c["name"].ToString().Length < 8)
                    tabs = "\t\t\t";
                else tabs = "\t\t";

                output += tabs + e.c["bin"];

                if (e.c["bin"].ToString().Length < 8)
                    tabs = "\t\t\t";
                else if (e.c["bin"].ToString().Length < 16)
                    tabs = "\t\t";
                else tabs = "\t";

                output += tabs;

                if (installed.Contains(e.c["keyword"].ToString()))
                    output += "§aInstalirano§f";

                client.Send(output);

            }

            return "";
        }
    
        public static string ScanApps(WebSocketSession client, string line) {
            int searchlevel = Command.GetInt(line, "level");

            List<string> directories = new List<string>();
            foreach (DriveInfo di in DriveInfo.GetDrives()) {
                if (di.IsReady) directories.Add(di.Name);
            }
            directories.Add(Environment.GetEnvironmentVariable("APPDATA"));
            directories.Add(Environment.GetEnvironmentVariable("LOCALAPPDATA"));
            directories.Add("C:\\Program Files (x86)\\Google\\Chrome\\Application");

            Dictionary<string, string> apps = new Dictionary<string, string>();
            foreach (string l in File.ReadAllLines(ProgramData.Directory + "bin\\app-list.txt")) {
                JSONElement json = JSON.Parse(l);
                apps.Add(json.c["keyword"].ToString(), json.c["bin"].ToString().ToLower());
            }

            Console.WriteLine($"Beginning search through {directories.Count} root directories");

            List<JSONElement> Found = new List<JSONElement>();

            void SearchDirectory(string dir, int level) {
                if (level > searchlevel) return;

                if (level == 1) {
                    client.Send($"§7Provjeravam direktorij §8{dir}");
                }

                try {
                    foreach (string s in Directory.GetFiles(dir)) {

                        if (Directory.Exists(s)) {
                            SearchDirectory(s, level);
                        }

                        string[] split = s.Split('\\');
                        string name = split[split.Length - 1].ToLower();

                        foreach (string app in apps.Keys)
                            if (name.Equals(apps[app])) {
                                JSONElement appf = new JSONElement();

                                JSONElement appf_n = new JSONElement();
                                appf_n.v = app;
                                JSONElement appf_p = new JSONElement();
                                appf_p.v = s;
                                JSONElement appf_b = new JSONElement();
                                appf_b.v = apps[app];

                                appf.c.Add("keyword", appf_n);
                                appf.c.Add("path", appf_p);
                                appf.c.Add("bin", appf_b);

                                Found.Add(appf);
                                break;
                            }
                    }
                    int newlevel = level + 1;
                    foreach (string s in Directory.GetDirectories(dir)) {
                        SearchDirectory(s, newlevel);
                    }
                }
                catch { }
            }
            foreach (string dir in directories) {
                SearchDirectory(dir, 1);
            }

            client.Send($"Pronadjeno §a{Found.Count} §7instaliranih aplikacija");

            File.Delete(ProgramData.Directory + "bin\\app-installed.txt");
            string fline = "";
            foreach (JSONElement el in Found) {
                fline += $"{JSON.Stringify(el)}\n";
            }
            if (fline.EndsWith(",")) fline = fline.Substring(0, fline.Length - 1);

            File.WriteAllText(ProgramData.Directory + "bin\\app-installed.txt", fline);

            return "";
        }
    
        public static string StartApp(WebSocketSession client, string line) {
            string name = Command.GetString(line, "name");

            foreach (string l in File.ReadAllLines(ProgramData.Directory + "bin\\app-installed.txt")) {
                JSONElement json = JSON.Parse(l);
                if (json.c["keyword"].ToString().ToLower().Equals(name.ToLower())) {
                    Process.Start(new ProcessStartInfo() {
                        FileName = json.c["path"].ToString(),
                        RedirectStandardOutput = true,
                        UseShellExecute = false
                    });
                    return $"Aplikacija §a{name} §7pokrenuta";
                }
            }

            return $"Aplikacija §c{name} §7nije pronadjena";
        }
    }
}
