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
        private static Dictionary<string, long> ProcessList = new Dictionary<string, long>();

        private static int Count = 0;

        public static void Start() {
            t = new Thread(a2);
            t.Start();
        }

        private static void a() {

            if (!File.Exists(Environment.GetEnvironmentVariable("APPDATA") + "\\Alan\\process-times.json"))
                File.WriteAllText(Environment.GetEnvironmentVariable("APPDATA") + "\\Alan\\process-times.json", "{}");

            string[] logdates = Directory.GetDirectories(Controller.DIRECTORY + "logs");
            long[] lastmod = new long[] { 0, 0 };

            for (int i = 1; i < logdates.Length; i++) {
                long millis;
                if ((millis = ((DateTimeOffset)new FileInfo(logdates[i]).LastWriteTime).ToUnixTimeMilliseconds()) > lastmod[1]) {
                    lastmod[0] = i;
                    lastmod[1] = millis;
                }
            }

            if (logdates.Length > 0)
                File.AppendAllText(logdates[lastmod[0]] + "\\processtracker.txt", $"{{\"action\":\"pc.shutdown\",\"time\":\"{PCLastShutdown()}\"}}\n");

            while (true) {

                Count++;

                // GET LIST OF ACTIVE PROCESSES
                Process[] plist = Process.GetProcesses();
                
                // CHECK IF PROCESS HAS ALREADY BEEN STARTED BEFORE. IF NOT
                // THEN ADD IT TO LIST
                foreach (Process p in plist) {
                    string pname = p.ProcessName.ToLower();
                    if (!ProcessList.ContainsKey(pname)) {

                        // PROCESS HAS JUST STARTED
                        Console.WriteLine($"{pname} has started.");
                        ProcessList.Add(pname, DateTimeOffset.Now.ToUnixTimeMilliseconds());

                        string Json = $"{{\"action\":\"process.start\", \"process\":\"{pname}\", \"appname\":\"{p.MainWindowTitle.Replace("\\", "\\\\")}\", \"time\":\"{DateTimeOffset.Now.ToUnixTimeMilliseconds()}\"}}";

                        Server.Broadcast(Json);
                        Controller.Log("processtracker", Json);
                    }
                }

                if (Count % 2 == 0) {
                    JSONElement Root = JSON.Parse(File.ReadAllText(Environment.GetEnvironmentVariable("APPDATA") + "\\Alan\\process-times.json").ToLower());
                    foreach (string p in ProcessList.Keys) {
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
                    string s = ProcessList.ElementAt(i).Key;
                    bool f = false;
                    foreach (Process p in plist) {
                        if (p.ProcessName.ToLower().Equals(s)) {
                            f = true;
                            break;
                        }
                    }
                    if (!f) {
                        // PROCESS IS CLOSED
                        Console.WriteLine($"{s} has closed.");
                        ProcessList.Remove(s);

                        string Json = $"{{\"action\":\"process.close\", \"process\":\"{s}\", \"time\":\"{DateTimeOffset.Now.ToUnixTimeMilliseconds()}\"}}";

                        Server.Broadcast(Json);
                        Controller.Log("processtracker", Json);
                    }
                }
                Thread.Sleep(5000);
            }
        }

        private static Dictionary<string, JSONElement> ProcessesJSON = new Dictionary<string, JSONElement>();
        private static List<string> Processes = new List<string>();
        private static void a2() {

            if (!File.Exists(Environment.GetEnvironmentVariable("APPDATA") + "\\Alan\\process-times.json"))
                File.WriteAllText(Environment.GetEnvironmentVariable("APPDATA") + "\\Alan\\process-times.json", "{}");

            string[] logdates = Directory.GetDirectories(Controller.DIRECTORY + "logs");
            long[] lastmod = new long[] { 0, 0 };

            for (int i = 1; i < logdates.Length; i++) {
                long millis;
                if ((millis = ((DateTimeOffset)new FileInfo(logdates[i]).LastWriteTime).ToUnixTimeMilliseconds()) > lastmod[1]) {
                    lastmod[0] = i;
                    lastmod[1] = millis;
                }
            }

            while (true) {

                Count++;
                Process[] procs = Process.GetProcesses();

                foreach (Process proc in procs) {
                    if (!Processes.Contains(proc.ProcessName.ToLower())) {
                        // PROCESS HAS JUST STARTED.
                        Processes.Add(proc.ProcessName.ToLower());

                        if (!ProcessesJSON.ContainsKey(proc.ProcessName.ToLower())) {
                            JSONElement e = new JSONElement();
                            e.c.Add("name", new JSONElement(proc.ProcessName.ToLower()));
                            e.c.Add($"{DateTimeOffset.Now.ToUnixTimeMilliseconds()}", new JSONElement("start"));

                            ProcessesJSON.Add(proc.ProcessName.ToLower(), e);

                            continue;
                        }

                        ProcessesJSON[proc.ProcessName.ToLower()].c.Add($"{DateTimeOffset.Now.ToUnixTimeMilliseconds()}", new JSONElement("start"));

                    }
                }

                // CHECK IF ANY PROCESS HAS CLOSED
                List<string> procnames = procs.Select(p => p.ProcessName.ToLower()).ToList();
                for (int i = Processes.Count - 1; i >= 0; i--) {
                    string proc = Processes[i];
                    if (!procnames.Contains(proc)) {
                        // PROCESS HAS JUST CLOSED
                        Processes.Remove(proc);

                        ProcessesJSON[proc].c.Add($"{DateTimeOffset.Now.ToUnixTimeMilliseconds()}", new JSONElement("close"));
                    }
                }

                if (Count % 2 == 0) {

                    List<string> lines = new List<string>();

                    // SAVE JSON TO FILE
                    foreach (JSONElement e in ProcessesJSON.Values)
                        lines.Add(JSON.Stringify(e));

                    File.WriteAllLines(Controller.DIRECTORY + "process-tracker\\" + Controller.SESSION_ID + ".txt", lines);
                }

                Thread.Sleep(5000);
            }
        }
        
        public static string GetJson() {
            string m = "{\"action\":\"process.list\", \"list\":[";

            try {/*
                for (int i = Processes.Count - 1; i >= 0; i--) {
                    string p = Processes[i];
                    try {
                        m += $"{{\"name\":\"{p}\",\"appname\":\"{Process.GetProcessesByName(p)[0].MainWindowTitle.Replace("\\", "\\\\")}\",\"start\":{0}}}, ";
                    }
                    catch { }
                }
                */

                for (int i = ProcessesJSON.Count - 1; i >= 0; i--) {
                    JSONElement e = ProcessesJSON.ElementAt(i).Value;
                    m += $"\"{e.c["name"].ToString()}\",";
                }

                if (m.EndsWith(",")) m = m.Substring(0, m.Length - 1);
            } catch { }

            m += "]}";
            return m;
        }

        public static string GetProcessDetails(string name) {

            string r = $"{{\"action\":\"process.details\",\"process\":\"{name}\",";

            try {
                long total = 0;

                DateTime dt = DateTime.Today;
                List<long> times = GetAppsUptime(new int[] {
                dt.Day,
                dt.Month,
                dt.Year
            }, new int[] {
                dt.Day,
                dt.Month,
                dt.Year
            }, name).Values.ToList();
                if (times.Count > 0) {
                    total += times[0];
                    r += $"\"today\":{times[0]},";
                }
                else r += $"\"today\":0,";

                try {
                    r += $"\"appname\":\"{Process.GetProcessesByName(name)[0].MainWindowTitle.Replace("\\", "\\\\")}\",";
                }
                catch {
                    r += "\"appname\":\"\",";
                }

                if (ProcessesJSON.ContainsKey(name)) {
                    JSONElement e = ProcessesJSON[name];
                    long start = 0;
                    for (int j = e.c.Count - 1; j >= 0; j--) {
                        if (e.c.ElementAt(j).Value.ToString() == "start") {
                            start = long.Parse(e.c.ElementAt(j).Key);
                            break;
                        }
                    }
                    r += $"\"start\":{start},";
                }
                else r += $"\"start\":0,";

                r += "\"chart\":[";

                for (int i = 0; i < 7; i++) {
                    dt = dt.AddDays(-1);

                    int[] date = new int[] { dt.Day, dt.Month, dt.Year };
                    long[] arr = GetAppsUptime(date, date, name).Values.ToArray();

                    if (arr.Length > 0) {
                        total += arr[0];

                        r += $"{{\"index\":{i},\"val\":{arr[0]}}},";
                        Console.WriteLine($"{arr[0]} on {date[0]}, {date[1]}, {date[2]}");
                    }
                }

                if (r.EndsWith(",")) r = r.Substring(0, r.Length - 1);
                r += $"],\"total\":{total}}}";

                Console.WriteLine(r);
            }
            catch {
                return "";
            }

            return r;

        }

        public static void KillProcess(string n) {
            Process[] ps = Process.GetProcessesByName(n);
            if (ps.Length == 0) return;

            Process p = ps[0];
            p.Kill();
        }

        public static long PCLastShutdown() {

            long time = 0;

            try {
                Microsoft.Win32.RegistryKey k = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\Control\\Windows");
                time = ((DateTimeOffset)(DateTime.FromFileTime(BitConverter.ToInt64((byte[])k.GetValue("ShutdownTime"), 0)))).ToUnixTimeMilliseconds();
            }
            catch { }

            return time;

        }

        public static Dictionary<string, long> GetAppsUptime(int[] start, int[] end, string process = null) {

            Dictionary<string, long> uptimes = new Dictionary<string, long>();

            string[] fs = Directory.GetFiles(Controller.DIRECTORY + "process-tracker");
            foreach (string f in fs) {
                FileInfo fi = new FileInfo(f);
                DateTime dt = fi.CreationTime;
                DateTime dt2 = fi.LastWriteTime;

                // SESSION IS IN RANGE [start - end]
                if (dt.Day >= start[0] && dt.Day <= end[0] && dt.Month >= start[1] && dt.Month <= end[1] && dt.Year >= start[2] && dt.Year <= start[2]) {
                    string[] Lines = File.ReadAllLines(f);
                    foreach (string Line in Lines) {
                        JSONElement json = JSON.Parse(Line);
                        if (process != null && !json.c["name"].ToString().ToLower().Equals(process.ToLower())) continue;

                        if (!uptimes.ContainsKey(json.c["name"].ToString().ToLower())) uptimes.Add(json.c["name"].ToString().ToLower(), 0);

                        long startedAt = 0;
                        bool closed = false;
                        for (int i = 1; i < json.c.Keys.Count; i++) {
                            if (json.c.Values.ElementAt(i).ToString() == "start") {
                                startedAt = long.Parse(json.c.Keys.ElementAt(i));
                                closed = false;
                                continue;
                            }

                            long closedAt = long.Parse(json.c.Keys.ElementAt(i));
                            long elapsed = closedAt - startedAt;

                            uptimes[json.c["name"].ToString()] += elapsed;
                            closed = true;

                        }

                        if (!closed) {
                            // APP HAS STOPPED
                            uptimes[json.c["name"].ToString()] += ((DateTimeOffset)dt2).ToUnixTimeMilliseconds() - startedAt;
                        }

                        if (process != null) break;

                    }
                }
            }

            return uptimes;

        }

    }
}
