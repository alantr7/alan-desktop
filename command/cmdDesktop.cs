using Alan.program;
using SuperWebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using SuperSocket.Common;
using WebSocket4Net;

namespace Alan.command {
    class cmdDesktop {

        public static WebSocketSession client;
        public static WebSocket ws;

        public static string Answer(WebSocketSession client, string line) {

            string subc = Command.AnySubcommand(line, "connect");

            switch (subc) {
                case "connect":
                    cmdDesktop.client = client;

                    Command.RequireParameters(line, "id");
                    Connect(Command.GetString(line, "id"));
                    break;
            }

            return "§7Nije pronadjena podkomanda";

        }

        public static void Connect(string id) {

            string r = cmdHttp.RequestWithCookies(Controller.URL + "remote/list", "get", ProgramData.LoginCookie);

            int Start, End;
            while ((Start = r.IndexOf('{')) > 0 && (End = r.IndexOf('}')) > 0) {

                string device = r.Substring(Start, End - Start + 1);

                Console.WriteLine("Parsing " + device);
                JSONElement json = JSON.Parse(device);
                Console.WriteLine(JSON.Stringify(json));

                string device_id = json.c["device_id"].ToString();

                if (device_id.Equals(id)) {

                    Console.WriteLine("Found device.");

                    string ipv4 = json.c["ipv4"].ToString();
                    string public_ip = json.c["public_ip"].ToString();

                    // User is allowed to device

                    Console.WriteLine("Trying ip: " + ipv4 + ", then: " + public_ip);

                    ws = new WebSocket($"ws://{ipv4}:25000");
                    ws.Open();

                    ws.Error += Ws_Error1;
                    ws.Opened += Ws_Opened;
                    ws.MessageReceived += Ws_MessageReceived;

                    break;

                }
                r = r.Substring(End + 1);

            }
        }

        private static void Ws_MessageReceived(object sender, MessageReceivedEventArgs e) {
            client.Send(e.Message);
        }

        private static void Ws_Opened(object sender, EventArgs e) {
            Console.WriteLine("Successfuly connected to ws");
        }

        private static void Ws_Error1(object sender, SuperSocket.ClientEngine.ErrorEventArgs e) {
            Console.WriteLine("There was an error whilst connecting to ws: " + e.Exception.Message);
        }
    }
}
