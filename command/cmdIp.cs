using SuperWebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Alan.command {
    class cmdIp {
        public static string Answer(WebSocketSession client, string line) {
            using (WebClient wc = new WebClient()) {
                return "IPv4\t\t" + Server.GetIPv4() + "\nPublic IP\t\t" + wc.DownloadString("http://ip-api.com/csv/?fields=query");
            }
        }
    }
}
