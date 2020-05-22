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

        private static Thread DownloadThread;

        private static bool DownloadCanceled = false;

        // CREATES LOCAL SERVER AND EXPOSE IT USING NGROK
        public static void CreateServer() {

            ws = new WebSocketServer();
            try {
                ws.Setup(27000);

                ws.NewSessionConnected += Ws_NewSessionConnected;
                ws.NewMessageReceived += Ws_NewMessageReceived;
                ws.SessionClosed += Ws_SessionClosed;

                ws.Start();
            } catch (Exception e) {
                Console.WriteLine("Server could not be created.");
                Console.WriteLine(e.Message);
            } finally {
                Console.WriteLine("Server started.");
            }

            // EXPOSE IT USING NGROK
            // CHECK IF NGROK IS ALREADY RUNNING
            if (Process.GetProcessesByName("ngrok").Length == 0) {
                // CREATE NEW NGROK PROCESS

                Console.WriteLine("NGROK NOT RUNNING: " + Environment.GetEnvironmentVariable("APPDATA"));

                Process.Start(new ProcessStartInfo() {
                    FileName = Environment.GetEnvironmentVariable("APPDATA") + "/Alan/ngrok.exe",
                    Arguments = "tcp 27000 --region eu --authtoken 1bpPKGSlSrnm4XhZ4XtWkkPJHrW_5ZqcrYdzk3rtwNKKiqLWq",
                    WindowStyle = ProcessWindowStyle.Hidden
                });

            } else Console.WriteLine("NGROK ALREADY RUNNING");

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

                        Console.WriteLine(wc.DownloadString($"{Controller.URL}request/host.php?device={Controller.DEVICE_ID}&ip={ip}&password=061439126"));
                    }
                }
                catch (Exception e) {
                    Console.WriteLine("Error whilst updating ngrok: " + e.Message);
                }
                Thread.Sleep(5000);
            }
        }

        public static void Respond(WebSocketSession s, JSONElement json) {

            Console.WriteLine("Responding to " + json.c["action"].v);

            string s1, s2, s3, response = "";

            switch (json.c["action"].v) {

                case "device.info":
                    clients[s] = ((string)json.c["info"].v).Replace("\\", "");
                    //s.Send($"{{\"action\":\"viewer.add\", \"info\":{clients[s]}}}");

                    foreach (string line in File.ReadAllLines(Environment.GetEnvironmentVariable("APPDATA") + "\\Alan\\logs\\" + DateTime.Now.ToString("ddMMyyyy") + "\\server.txt")) {
                        s.Send($"{{\"action\":\"log\",\"log\":\"{line.Replace("\\", "\\\\").Replace("\"", "\\\"")}\"}}");
                    }

                    //Broadcast($"{{\"action\":\"viewer.add\", \"info\":{clients[s]}}}");
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
                case "process.log":
                    s1 = (string)json.c["process"].v;
                    s2 = (string)json.c["date"].v;

                    s3 = $"{Environment.GetEnvironmentVariable("APPDATA")}\\Alan\\logs\\{s2}\\processtracker.txt";

                    response = $"{{\"action\":\"process.log\",\"process\":\"{s1}\",\"date\":\"{s2}\",\"log\":[";

                    if (File.Exists(s3)) {

                        response = $"{{\"action\":\"process.log\",\"process\":\"{s1}\",\"date\":\"{s2}\",";
                        response += $"\"lastUpdate\":{((DateTimeOffset)File.GetLastWriteTime(Environment.GetEnvironmentVariable("APPDATA") + "\\Alan\\process-times.json")).ToUnixTimeMilliseconds()},";
                        response += $"\"log\":[";
                        foreach (string line in File.ReadLines(s3))
                            if (line.Contains($"\"process\":\"{s1}\""))
                                response += $"{line},";

                        if (response.EndsWith(",")) response = response.Substring(0, response.Length - 1);

                    }


                    response += "]}";

                    s.Send(response);

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
                        r += $"],\"decodedpath\":\"{p.Replace("\\", "/")}\"}}";

                        s.Send(r);
                        break;
                    }
                    string decodedpath = "";
                    int EnvVarStarted = -1;
                    for (int i = 0; i < p.Length; i++) {
                        if (p[i] == '%') {
                            if (EnvVarStarted != -1) {
                                decodedpath += Environment.GetEnvironmentVariable(p.Substring(EnvVarStarted, i - EnvVarStarted).Replace("%", ""));
                                Console.WriteLine("FOUND ENV VARIABLE: " + p.Substring(EnvVarStarted, i - EnvVarStarted).Replace("%", ""));
                                EnvVarStarted = -1;
                            }
                            else EnvVarStarted = i;
                        } else if (EnvVarStarted == -1) decodedpath += p[i];
                    }
                    p = decodedpath;

                    Console.WriteLine("DECODED PATH: " + decodedpath);

                    if (Directory.Exists(p)) {
                        List<string> vs = new List<string>(Directory.GetDirectories(p));
                        foreach (string f in Directory.GetFiles(p))
                            vs.Add(f);

                        foreach (string f in vs) {
                            string fn = f.Replace("\\", "/");
                            fn = fn.Split('/')[fn.Split('/').Length - 1];
                            r += $"{{\"name\":\"{fn}\",\"directory\":\"{Directory.Exists(f)}\"}},";
                        }
                        if (r.EndsWith(",")) r = r.Substring(0, r.Length - 1);
                        r += $"],\"decodedpath\":\"{decodedpath.Replace("\\", "/")}\"}}";
                        s.Send(r);
                    }
                    break;
                case "pc.file.download":
                    p = (string)json.c["path"].v;

                    if (File.Exists(p)) {

                        DownloadThread = new Thread(() => {
                            try {

                                DownloadCanceled = false;

                                byte[] FileBytes = File.ReadAllBytes(p);
                                int CurrentBytePointer = 0;

                                int BUFFER_SIZE = 10240;

                                int i = 0;

                                while (i < FileBytes.Length / BUFFER_SIZE && !DownloadCanceled) {

                                    r = $"{{\"action\":\"pc.file.download\",\"id\":\"{json.c["id"]}\",\"totalBytes\":{FileBytes.Length},\"bytes\":[";
                                    for (int j = 0; j < BUFFER_SIZE; j++) {
                                        r += FileBytes[i * BUFFER_SIZE + j] + ", ";
                                    }

                                    if (r.EndsWith(", ")) r = r.Substring(0, r.Length - 2);

                                    r += "], \"status\":\"wait\"}";
                                    s.Send(r);

                                    CurrentBytePointer += BUFFER_SIZE;
                                    i++;

                                    Console.WriteLine($"I: {i} / {FileBytes.Length / BUFFER_SIZE}");

                                }

                                if (!DownloadCanceled) {

                                    r = $"{{\"action\":\"pc.file.download\",\"id\":\"{json.c["id"]}\",\"totalBytes\":{FileBytes.Length},\"bytes\":[";

                                    for (i = CurrentBytePointer; i < FileBytes.Length; i++) r += FileBytes[i] + ", ";
                                    if (r.EndsWith(", ")) r = r.Substring(0, r.Length - 2);

                                    r += "], \"status\":\"completed\"}";
                                    s.Send(r);

                                }
                                else Console.WriteLine("Downloading is cancelled.");

                            }
                            catch { }

                        });
                        DownloadThread.Start();

                    }
                    break;
                case "pc.file.download.cancel":

                    Console.WriteLine("Cancelling thread...");

                    DownloadCanceled = true;
                    DownloadThread = null;

                    r = $"{{\"action\":\"pc.file.download\",\"id\":\"{json.c["id"]}\",\"status\":\"canceled\"}}";
                    s.Send(r);

                    break;
                case "pc.file.delete":

                    break;
                case "console.input":
                    Console.WriteLine("Executing command " + json.c["command"].v);
                    Process CommandProcess = new Process();

                    CommandProcess.StartInfo.FileName = "cmd";
                    CommandProcess.StartInfo.Arguments = "/C " + json.c["command"].v;
                    CommandProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                    CommandProcess.StartInfo.UseShellExecute = false;
                    CommandProcess.StartInfo.RedirectStandardOutput = true;

                    CommandProcess.Start();

                    string CommandOutput = "", Line = "";

                    StreamReader streamReader = CommandProcess.StandardOutput;
                    while (!CommandProcess.StandardOutput.EndOfStream)
                        if ((Line = CommandProcess.StandardOutput.ReadLine()) != null) CommandOutput += Line + "<br>";

                    streamReader.Dispose();

                    CommandOutput = CommandOutput.Replace("\"", "\\\"");

                    Broadcast($"{{\"action\":\"console.output\",\"itemid\":\"{json.c["itemid"].v}\",\"output\":\"{CommandOutput}\"}}");
                    break;
                case "pc.screenshare.start":
                    if (!ScreenShare.s.Contains(s))
                        ScreenShare.s.Add(s);
                    break;
                case "pc.screenshare.stop":
                    ScreenShare.s.Remove(s);
                    break;
                case "pc.screenshare.properties":
                    ScreenShare.STREAM_FRAMES = json.c["frames"].ToInt();
                    ScreenShare.STREAM_SIZE[0] = json.c["w"].ToInt();
                    ScreenShare.STREAM_SIZE[1] = json.c["h"].ToInt();
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
                    //Watch.Play(json.c["imdbid"].ToString(), json.c["type"].ToString(), season, episode);
                    break;
                case "watch.resume":
                    break;
                case "watch.pause":
                    break;

                case "phone.info":
                    MyPhone.LastUpdate = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    MyPhone.Battery = json.c["battery"].ToString();

                    Broadcast($"{{\"action\":\"phone.info\",\"battery\":{MyPhone.Battery}}}");
                    break;

            }
        }

        public static void Broadcast(string Message) {
            try {
                foreach (WebSocketSession c in ws.GetAllSessions()) c.Send(Message);
            }
            catch (Exception e) {
                Console.WriteLine("Server is not online yet.");
                Console.WriteLine(e.Message);
            }
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
                Controller.Log("server", DateTimeOffset.Now.ToUnixTimeMilliseconds() + ": " + clients[session] + ": " + value);

                Console.WriteLine("Response sent.");
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
            }
        }

        private static void Ws_NewSessionConnected(WebSocketSession session) {
            Console.WriteLine("Somebody connected");
            session.Send(ProcessTracker.GetJson());
            session.Send($"{{\"action\":\"phone.info\",\"battery\":{MyPhone.Battery}}}");
        }
    }



}
