using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Alan {
    class ProcessTracker {

        private static Thread t;
        private static List<string> ProcessList = new List<string>();

        private static int Count = 0;

        public static void Start() {
            t = new Thread(a);
            t.Start();
        }

        private static void a() {

            while (true) {

                Count++;

                // GET LIST OF ACTIVE PROCESSES
                Process[] plist = Process.GetProcesses();
                
                // CHECK IF PROCESS HAS ALREADY BEEN STARTED BEFORE. IF NOT
                // THEN ADD IT TO LIST
                foreach (Process p in plist) {
                    if (!ProcessList.Contains(p.ProcessName)) {

                        // PROCESS HAS JUST STARTED
                        Console.WriteLine($"{p.ProcessName} has started.");
                        ProcessList.Add(p.ProcessName);

                        string Json = $"{{\"action\":\"process.start\", \"process\":\"{p.ProcessName}\", \"time\":\"{DateTime.Now.Ticks}\"}}";

                        Server.Broadcast(Json);
                        Controller.Log("processtracker", Json);
                    }
                }

                if (Count % 2 == 0) {
                    JSONElement Root = JSON.Parse(File.ReadAllText(Environment.GetEnvironmentVariable("APPDATA") + "\\Alan\\process-times.json"));
                    foreach (string p in ProcessList) {
                        if (Root.c.ContainsKey(p)) {
                            Root.c[p].v = Int32.Parse(Root.c[p].ToString()) + 10;
                        } else {
                            JSONElement e = new JSONElement();
                            e.v = 0;
                            Root.c.Add(p, e);
                        }
                    }

                    File.WriteAllText(Environment.GetEnvironmentVariable("APPDATA") + "\\Alan\\process-times.json", JSON.Stringify(Root));

                }

                // CHECK IF ANY OF PROCESSES HAS CLOSED
                for (int i = ProcessList.Count - 1; i >= 0; i--) {
                    string s = ProcessList[i];
                    bool f = false;
                    foreach (Process p in plist) {
                        if (p.ProcessName.Equals(s)) {
                            f = true;
                            break;
                        }
                    }
                    if (!f) {
                        // PROCESS IS CLOSED
                        Console.WriteLine($"{s} has closed.");
                        ProcessList.RemoveAt(i);

                        string Json = $"{{\"action\":\"process.close\", \"process\":\"{s}\", \"time\":\"{DateTime.Now.Ticks}\"}}";

                        Server.Broadcast(Json);
                        Controller.Log("processtracker", Json);
                    }
                }
                Thread.Sleep(5000);
            }
        }

        public static string GetJson() {
            string m = "{\"action\":\"process.list\", \"list\":[";

            foreach (string p in ProcessList) {
                m += $"{{\"name\":\"{p}\"}}, ";
            }

            if (m.EndsWith(", ")) m = m.Substring(0, m.Length - 2);

            m += "]}";
            return m;
        }

        public static void KillProcess(string n) {
            Process[] ps = Process.GetProcessesByName(n);
            if (ps.Length == 0) return;

            Process p = ps[0];
            p.Kill();
        }

    }
}
