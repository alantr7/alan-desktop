using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Alan {
    class TorrentFinder {

        public static void ScanPcGamesTorrents() {
            string link = "https://pcgamestorrents.com/games-list.html";
            using (WebClient wc = new WebClient()) {
                string content = wc.DownloadString(link);

                string[] lis = content.Split(new string[] {
                    "<li>"
                }, StringSplitOptions.None);

                foreach (string li in lis) {
                    string href = li.Split('"')[1];
                    if (!href.EndsWith(".html")) continue;

                    string name = li.Split('>')[1].Split('<')[0];

                    Console.WriteLine(name + ": " + href);
                }
            }
        }

        public static string FindMagnet(string url) {
            using (WebClient wc = new WebClient()) {
                string content = wc.DownloadString(url);
            }
            return "";
        }

        public static string FindGamesJson(string q) {

            string json = "[";

            string[] Games = File.ReadAllLines(Environment.GetEnvironmentVariable("APPDATA") + "\\Alan\\games.txt");
            for (int i = 0; i < Games.Length / 2; i++) {
                if (Games[i].ToLower().Contains(q.ToLower())) {
                    json += $"{{\"name\":\"{Games[2 * i]}\",\"magnetcontainer\":\"{Games[2 * i + 1]}\"}}";
                }
            }

            json += "]";
            return json;
        }

    }
}
