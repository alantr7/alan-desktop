using SuperWebSocket;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Alan.command.Command;

namespace Alan.command {
    class cmdProcess {

        public static string Answer(WebSocketSession client, string line) {
            string response = "";

            string mainAction = AnySubcommand(line,
                "close",
                "start",
                "list",
                "details"
            );

            switch (mainAction) {
                case "close":
                    RequireParameters(line, "name");
                    return TerminateProcess(GetString(line, "name"));
                case "start":
                    RequireParameters(line, "name");
                    return StartProcess(GetString(line, "name"));
                case "list":
                    foreach (string p in ProcessTracker.GetProcesses()) {
                        response += "§7\t" + p + "\n";
                    }
                    return response;
                case "details":
                    RequireParameters(line, "name");
                    string name = GetString(line, "name");

                    client.Send(Command.IndentAfter("Naziv procesa", 3) + name);
                    client.Send(Command.IndentAfter("Ukupno vrijeme", 3) + ProcessTracker.FormatProcessTime(ProcessTracker.GetProcessTotalUptime(name)));

                    long uptime = ProcessTracker.GetProcessUptime(name);
                    if (uptime > 0) {
                        client.Send(Command.IndentAfter("Vrijeme pokretanja", 3) + Command.FormatTime("§a{H}§7:§a{M}§7:§a{S}", uptime));
                    }

                    return response;
            }

            return response;
        }

        public static string StartProcess(string name) {
            try {
                Process.Start(name);
                return $"Proces §a{name} §7je uspjesno pokrenut";
            } catch {
                return $"Proces §c{name} §7nije pronadjen";
            }
        }

        public static string TerminateProcess(string name) {
            Process[] ps = Process.GetProcessesByName(name);
            int Count = ps.Length;
            if (Count > 0) {
                foreach (Process p in ps)
                    p.Kill();
                return $"Komanda izvrsena nad §a{Count} §7proces(a/om) sa nazivom §e{name}";
            }
            return $"Proces §c{name} §7nije pronadjen";
        }

    }
}
