using SuperWebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Alan {
    class ScreenShare {

        public static List<WebSocketSession> s = new List<WebSocketSession>();

        public static void Start() {
            new Thread(() => {
                while (true) {
                    if (s.Count > 0) Send();
                    Thread.Sleep(1000);
                }
            }).Start();
        }

        private static void Send() {

            string json = Robot.ScreenshotJson();

            for (int i = 0; i < s.Count; i++) {
                s[i].Send(json);
            }

        }

    }
}
