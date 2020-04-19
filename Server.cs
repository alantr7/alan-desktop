using SuperWebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;

namespace Alan {
    class Server {

        private static WebSocketServer ws;
        private static Dictionary<WebSocketSession, string> clients = new Dictionary<WebSocketSession, string>();

        private static string NGROK_IP = "";

        // CREATES LOCAL SERVER AND EXPOSE IT USING NGROK
        public static void CreateServer() {

            ws = new WebSocketServer();
            ws.Setup(27000);

            ws.NewSessionConnected += Ws_NewSessionConnected;
            ws.NewMessageReceived += Ws_NewMessageReceived;
            ws.SessionClosed += Ws_SessionClosed;

            ws.Start();

            // EXPOSE IT USING NGROK
            // CHECK IF NGROK IS ALREADY RUNNING
            if (Process.GetProcessesByName("ngrok").Length == 0) {
                // CREATE NEW NGROK PROCESS
                Process p = new Process();
                p.StartInfo.FileName = Environment.GetEnvironmentVariable("APPDATA") + "/Alan/ngrok.exe";
                p.StartInfo.Arguments = "tcp 27000 --region eu";
                p.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                p.Start();
            }

            // GET IP OF THE NGROK SERVER
            while (NGROK_IP.Length < 8) {
                try {
                    using (WebClient wc = new WebClient()) {
                        string s = wc.DownloadString("http://127.0.0.1:4040/api/tunnels/command_line");
                        string ip = s
                            .Split(new string[] { "public_url\":\"" }, StringSplitOptions.None)[1]
                            .Split('"')[0]
                            .Split('/')[2];

                        Console.WriteLine("IP: " + ip);
                        NGROK_IP = ip;

                        Console.WriteLine(wc.DownloadString($"{Controller.URL}request/host.php?device={Controller.DEVICE_ID}&ip={ip}&password=test"));
                    }
                }
                catch { }
                Thread.Sleep(5000);
            }
        }

        public static void Respond(WebSocketSession s, JSONElement json) {
            switch (json.c["action"].v) {

                case "device.info":
                    clients[s] = ((string)json.c["info"].v).Replace("\\", "");
                    s.Send($"{{\"action\":\"viewer.add\", \"info\":{clients[s]}}}");
                    Broadcast($"{{\"action\":\"viewer.add\", \"info\":{clients[s]}}}");
                    break;

                case "process.start":
                    string p = (string)json.c["path"].v;
                    if (File.Exists(p)) {
                        Process process = new Process();
                        process.StartInfo.FileName = p;
                        process.Start();
                    } else {
                        Process.Start(new ProcessStartInfo() {
                            FileName = p
                        });
                    }
                    break;
                case "process.kill":
                    ProcessTracker.KillProcess((string)json.c["process"].v);
                    break;
                case "pc.shutdown":
                    Process.Start(new ProcessStartInfo("shutdown", "/s /t 0") {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });
                    break;
                case "pc.input.mouse":
                    if ((string)json.c["button"].v == "left") Robot.MouseLeftClick();
                    else Robot.MouseRightClick();
                    break;
                case "pc.input.keyboard":
                    Robot.KeyboardWrite((string)json.c["keys"].v);
                    break;
                case "pc.file.list":
                    p = (string)json.c["path"].v;
                    string caller = (string)json.c["caller"].v;
                    string r = "{\"action\":\"pc.file.list\",\"caller\":\"" + caller + "\",\"list\":[";                    
                    if (p == "/") {
                        foreach (DriveInfo di in DriveInfo.GetDrives())
                            r += $"{{\"name\":\"{di.Name.Replace("\\", "")}\",\"directory\":\"True\"}},";

                        if (r.EndsWith(",")) r = r.Substring(0, r.Length - 1);
                        r += "]}";

                        s.Send(r);
                        break;
                    }
                    if (Directory.Exists(p)) {
                        List<string> vs = new List<string>(Directory.GetFiles(p));
                        foreach (string f in Directory.GetDirectories(p))
                            vs.Add(f);

                        foreach (string f in vs) {
                            string fn = f.Replace("\\", "/");
                            fn = fn.Split('/')[fn.Split('/').Length - 1];
                            r += $"{{\"name\":\"{fn}\",\"directory\":\"{Directory.Exists(f)}\"}},";
                        }
                        if (r.EndsWith(",")) r = r.Substring(0, r.Length - 1);
                        r += "]}";
                        s.Send(r);
                    }
                    break;
                case "pc.file.delete":

                    break;
                case "pc.command":
                    Process.Start(new ProcessStartInfo() {
                        FileName = "cmd",
                        Arguments = "/C " + json.c["command"].v,
                        WindowStyle = ProcessWindowStyle.Hidden
                    });
                    break;
                case "pc.screenshare.start":
                    if (!ScreenShare.s.Contains(s))
                        ScreenShare.s.Add(s);
                    break;
                case "pc.screenshare.stop":
                    ScreenShare.s.Remove(s);
                    break;
                case "pc.torrent.games.find":
                    string Games = TorrentFinder.FindGamesJson((string)json.c["query"].v);
                    s.Send($"{{\"action\":\"pc.torrent.games.find\",\"caller\":\"{json.c["caller"].v}\",\"list\":\"{Games}\"}}");
                    break;
                case "pc.torrent.games.scan":

                    break;
                case "open.link":

                    break;


                case "watch.play":
                    int season = 0, episode = 0;
                    if (json.c["type"].ToString() == "serie") {
                        season = (int)json.c["season"].v;
                        episode = (int)json.c["episode"].v;
                    }
                    Watch.Play(json.c["imdbid"].ToString(), json.c["type"].ToString(), season, episode);
                    break;
                case "watch.resume":
                    break;
                case "watch.pause":
                    break;

            }
        }

        public static void Broadcast(string Message) {
            try {
                foreach (WebSocketSession c in ws.GetAllSessions()) c.Send(Message);
            }
            catch { }
        }

        private static void Ws_SessionClosed(WebSocketSession session, SuperSocket.SocketBase.CloseReason value) {
            try {
                Broadcast($"{{\"action\":\"viewer.remove\", \"info\":{clients[session]}}}");
            }
            catch { }

            ScreenShare.s.Remove(session);
            clients.Remove(session);
            Console.WriteLine("Somebody disconnected");
        }

        private static void Ws_NewMessageReceived(WebSocketSession session, string value) {
            Console.WriteLine($"Received message: {value}");
            try {                
                Respond(session, JSON.Parse(value));
                Controller.Log("server", clients[session] + ": " + value);
            }
            catch { }
        }

        private static void Ws_NewSessionConnected(WebSocketSession session) {
            Console.WriteLine("Somebody connected");
            session.Send(ProcessTracker.GetJson());
        }
    }



}
