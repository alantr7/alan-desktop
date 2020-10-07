using Alan.program;
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

        // Process tracker thread
        private static Thread t;

        // Number of times that process list has been updated (every 5 seconds)
        private static int Count = 0;

        // Start process tracker thread
        public static void Start() {
            t = new Thread(Work);
            t.Start();
        }

        // List of all running processes
        private static List<string> Processes = new List<string>();

        // This will be running on process tracker thread
        private static void Work() {

            if (!File.Exists(ProgramData.Directory + "process-tracker\\total.json"))
                File.WriteAllText(ProgramData.Directory + "process-tracker\\total.json", "{}");

            // Update total times file and delete older sessions
            UpdateTotalTimes();

            // Create new directory for current session
            Directory.CreateDirectory($"{ProgramData.Directory}\\process-tracker\\{ProgramData.SystemBoot}");

            while (true) {

                try {
                    Process[] procs = Process.GetProcesses();

                    foreach (Process proc in procs) {
                        if (!Processes.Contains(proc.ProcessName)) {

                            // New process has just started
                            Processes.Add(proc.ProcessName);

                            // Save process start time to file
                            long StartTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                            File.AppendAllText($"{ProgramData.Directory}process-tracker\\{ProgramData.SystemBoot}\\{proc.ProcessName}.txt", $"s{StartTime}\n");

                        }
                    }

                    // Check if any process has closed
                    List<string> procnames = procs.Select(p => p.ProcessName).ToList();
                    for (int i = Processes.Count - 1; i >= 0; i--) {
                        string proc = Processes[i];
                        if (!procnames.Contains(proc)) {

                            // Process has just closed
                            Processes.Remove(proc);

                            // Save process exit time to file
                            File.AppendAllText($"{ProgramData.Directory}process-tracker\\{ProgramData.SystemBoot}\\{proc}.txt", $"e{DateTimeOffset.Now.ToUnixTimeMilliseconds()}\n");

                        }
                    }

                    // Run every 10 seconds
                    if (Count++ % 2 == 0) {

                        // Save latest update to file
                        File.WriteAllText($"{ProgramData.Directory}\\process-tracker\\{ProgramData.SystemBoot}\\last-update.txt", "");

                    }
                }
                catch { }

                Thread.Sleep(5000);
            }
        }

        private static void UpdateTotalTimes() {

            JSONElement root = JSON.Parse(File.ReadAllText(ProgramData.Directory + "process-tracker\\total.json"));

            string[] ss = Directory.GetDirectories(ProgramData.Directory + "process-tracker");
            foreach (string s in ss) {

                // Get session id from directory path
                string session = s.Substring(s.LastIndexOf('\\') + 1, s.Length - s.LastIndexOf('\\') - 1);

                long shutdown = DateTimeOffset.Now.ToUnixTimeMilliseconds(); // stores time when computer shut down, if it's still on, it will be current time
                bool CurrentSession = session.Equals(ProgramData.SystemBoot + ""); // checks if checked session is equal to current one

                if (!CurrentSession) {
                    shutdown = (long)(new FileInfo(s + "\\last-update.txt").LastWriteTime - new DateTime(1970, 1, 1, 0, 0, 0)).TotalMilliseconds;
                }

                // Read all data
                string[] fs = Directory.GetFiles(s);
                foreach (string f in fs) {

                    // Get process name from file path
                    string process = f.Substring(f.LastIndexOf('\\') + 1, f.Length - f.LastIndexOf('\\') - 5);

                    // Read process start and exit times, and calculate elapsed times
                    string[] Lines = File.ReadAllLines(f);

                    long startAt = 0; // stores last known start time for the process
                    long total = 0; // stores total time that process has been up for that session

                    // If 'total.json' file already contains time for this process, then it will be used as start value
                    if (root.c.ContainsKey(process))
                        total = root.c[process].ToLong();

                    // This will be true if last line of the file starts with 'e'
                    bool Ended = false;

                    foreach (string l in Lines) {
                        if (l.Length < 10) continue;
                        try {
                            long time = Int64.Parse(l.Substring(1));
                            if (l.StartsWith("s")) {
                                if (startAt != 0) total += time - startAt;
                                startAt = time;
                                Ended = false;
                            }
                            else {
                                total += time - startAt;
                                Ended = true;
                            }
                        }
                        catch { }
                    }

                    // This will be 'false' if computer shut down or 'Alan' process was closed
                    if (!Ended) {
                        // If 'false' then app is closed because system turned off
                        if (!CurrentSession) {
                            total += shutdown - startAt;
                        } else {
                            if (Process.GetProcessesByName(process).Length > 0)
                                total += DateTimeOffset.Now.ToUnixTimeMilliseconds() - startAt;
                        }
                    }

                    root.c[process] = new JSONElement(total);
                    Console.WriteLine($"New total for {process} : {total} for session {session}");
                }

                // Delete session directory since all values are already saved
                foreach (string f in fs) {
                    File.Delete(f);
;               }
                Directory.Delete(s);
                Console.WriteLine($"Files and directory deleted @ {s}");

            }

            File.WriteAllText(ProgramData.Directory + "process-tracker\\total.json", JSON.Stringify(root));

        }

        // Get total times in milliseconds that process has been running
        public static long GetProcessTotalUptime(string name) {
            long uptime = 0;

            JSONElement root = JSON.Parse(File.ReadAllText(ProgramData.Directory + "process-tracker\\total.json"));
            if (root.c.ContainsKey(name)) {
                uptime = root.c[name].ToLong();
            }

            return uptime;
        }

        public static string FormatProcessTime(long millis) {
            int s = (int)(millis / 1000); // 3727114 / 1000 = 3727; 
            int m = s / 60;
            s -= m * 60;

            int h = m / 60;
            m -= h * 60;

            return $"{command.Command.FormatNumber(h, 2)}h {command.Command.FormatNumber(m, 2)}m {command.Command.FormatNumber(s, 2)}s";
        }

        // Get time that process has been running this session
        public static long GetProcessUptime(string name) {
            Process[] p = Process.GetProcessesByName(name);
            if (p.Length == 0) return 0;

            try {
                return (long)(p[0].StartTime - new DateTime(1970, 1, 1, 0, 0, 0)).TotalMilliseconds;
            } catch {
                return 0;
            }
        }

        // Get last time PC was shut down in milliseconds
        public static long PCLastShutdown() {
            try {
                Microsoft.Win32.RegistryKey k = Microsoft.Win32.Registry.LocalMachine.OpenSubKey("System\\CurrentControlSet\\Control\\Windows");
                return ((DateTimeOffset)(DateTime.FromFileTime(BitConverter.ToInt64((byte[])k.GetValue("ShutdownTime"), 0)))).ToUnixTimeMilliseconds();
            } catch {}
            return 0;
        }

        public static List<string> GetProcesses() {
            return Processes;
        }

    }
}
