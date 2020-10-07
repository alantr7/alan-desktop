using SuperWebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alan.command {
    class Command {

        public static void ExecuteCommand(WebSocketSession client, string command) {

            string response = "<no response>"; // Command will return this if response does not exist

            // If computer is connected to another computer, it will redirect the command
            if (cmdDesktop.ws != null) {
                cmdDesktop.ws.Send(command);
                return;
            }

            string commandLine; // Actual command without command ID
            string commandID = ""; // Used when responding to client

            bool HasID = true;

            // Check if command contains numeric ID
            if (command.Contains("|")) {
                commandID = command.Substring(0, command.IndexOf('|'));
                HasID = int.TryParse(commandID, out _);
            }
            else HasID = false;

            Console.WriteLine("Command has ID: " + HasID);

            // Remove ID from command
            if (HasID) {
                commandLine = command.Substring(command.IndexOf('|') + 1);
            }
            else commandLine = command;

            switch (GetCommand(commandLine)) {
                case "help":
                    response = cmdHelp.Answer(client, commandLine);
                    break;
                case "ip":
                    response = cmdIp.Answer(client, commandLine);
                    break;
                case "http":
                    response = cmdHttp.Answer(client, commandLine);
                    break;
                case "app":
                    response = cmdApp.Answer(client, commandLine);
                    break;
                case "file":
                    response = cmdFile.Answer(client, commandLine);
                    break;
                case "desktop":
                    response = cmdDesktop.Answer(client, commandLine);
                    break;
                case "process":
                    response = cmdProcess.Answer(client, commandLine);
                    break;
                case "env":
                    response = cmdEnv.Answer(client, commandLine);
                    break;
                case "var":
                    response = cmdVar.Answer(client, commandLine);
                    break;
            }

            // Add ID to response
            if (HasID) {
                response = commandID + "|" + response;
            }

            if (response.Length > 0)
                client.Send(response);
        }

        public static int GetInt(string line, string name) {
            return int.Parse(GetString(line, name));
        }

        public static string GetString(string line, string name) {
            name = "-" + name;
            try {
                string val = line.Substring(line.IndexOf(name) + name.Length).Substring(1);
                foreach (string var in cmdVar.variables.Keys) {
                    val = val.Replace($"${{{var}}}", cmdVar.variables[var]);
                }
                if (val.StartsWith("\"")) {
                    return val.Substring(1, val.Substring(1).IndexOf('"'));
                }
                else if (val.Contains(' ')) return val.Split(' ')[0];
                else return val;
            } catch {
                return null;
            }
        }

        public static string GetCommand(string line) => line.Contains(" ") ? line.Split(' ')[0].Trim() : line.Trim();

        public static string GetSubcommands(string line) {
            return "";
        }

        public static bool HasSubcommand(string line, string cmd) {
            return line.Contains(cmd);
        }

        public static string IndentAfter(string line, int tabs) {

            int CurrentTab = (int)Math.Round((decimal)line.Length / 8);
            for (int i = CurrentTab; i < tabs + 1; i++) {
                line += '\t';
            }

            return line;

        }

        public static string AnySubcommand(string line, params string[] possibleSubs) {
            foreach (string s in possibleSubs)
                if (line.Contains(s)) return s;
            return "";
        }

        public static void RequireParameters(string line, params string[] param) {
            foreach (string p in param) {
                if (GetString(line, p) == null)
                    throw new Exception($"Missing parameter : {p}");
            }
        }

        public static string FormatTime(string format, long millis) {
            DateTimeOffset dt = DateTimeOffset.FromUnixTimeMilliseconds(millis);
            return format
                .Replace("{D}", dt.Day + "")
                .Replace("{H}", FormatNumber(dt.Hour, 2))
                .Replace("{M}", FormatNumber(dt.Minute, 2))
                .Replace("{S}", FormatNumber(dt.Second, 2));
        }

        public static string FormatNumber(int number, int digits) {
            string s = number + "";
            while (s.Length < digits) s = "0" + s;

            return s;
        }

    }
}
