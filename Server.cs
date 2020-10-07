using Alan.command;
using Alan.program;
using alan_ngrok;
using SuperWebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Threading;

namespace Alan {
    class Server {

        private static WebSocketServer ws;
        private static Dictionary<WebSocketSession, string> clients = new Dictionary<WebSocketSession, string>();

        private static Ngrok ngrok;

        // Creates local server and opens it to web
        public static void CreateServer() {

            ws = new WebSocketServer();
            try {
                ws.Setup(25000);

                ws.NewSessionConnected += Ws_NewSessionConnected;
                ws.NewMessageReceived += Ws_NewMessageReceived;
                ws.SessionClosed += Ws_SessionClosed;

                ws.Start();
                Console.WriteLine("Local server running on port 25000");

                ngrok = new Ngrok(ProgramData.Directory + "bin\\ngrok.exe", ProgramData.NgrokToken);
                ngrok.Options = new NgrokOptions() {
                    CloseIfRunning = true,
                    Region = "eu",
                    Type = "tcp"
                };
                ngrok.Expose(25000);

                // GET IP OF THE NGROK SERVER
                string ip;
                while ((ip = ngrok.GetIP()).Length < 8) {
                    Thread.Sleep(5000);
                }
                try {
                    Console.WriteLine("Uploading data...");
                    Controller.APIWrapper.getUser().CreateDevice(Controller.DEVICE_ID, GetIPv4(), ip, "");
                }
                catch (Exception e) {
                    Console.WriteLine("Error whilst updating ngrok: " + e.Message);
                }
            } catch (Exception e) {
                Console.WriteLine("Server could not be created.");
                Console.WriteLine(e.Message);
            } finally {
                Console.WriteLine("Server started.");
            }

            
        }

        public static string GetIPv4() {
            foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList) {
                if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork) {
                    return ip.ToString();
                }
            }
            return "";
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
            ScreenShare.s.Remove(session);
            clients.Remove(session);
        }

        // Command is received from client. Response will be sent after command is executed.
        private static void Ws_NewMessageReceived(WebSocketSession session, string value) {
            try {                
                Command.ExecuteCommand(session, value);
            }
            catch (Exception e) {
                Console.WriteLine(e.Message);
            }
        }

        private static void Ws_NewSessionConnected(WebSocketSession session) {
            Console.WriteLine("Somebody connected");

            // Send commands, and auto-complete to client
            //Command.SendAutocomplete(session);

            clients.Add(session, "");
        }
    }



}
