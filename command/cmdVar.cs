using Alan.program;
using SuperWebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Alan.command.Command;

namespace Alan.command {
    class cmdVar {

        public static Dictionary<string, string> variables = new Dictionary<string, string>();

        public static string Answer(WebSocketSession client, string line) {
            string response = "";

            string mainAction = AnySubcommand(line, "define", "list");
            switch (mainAction) {
                case "list":
                    foreach (string v in variables.Keys) {
                        client.Send(IndentAfter(v, 3) + variables[v].ToString());
                    }
                    return response;
                case "define":
                    variables.Add(GetString(line, "name"), GetString(line, "value"));
                    SaveVariables();
                    return $"Variable ${GetString(line, "name")} created and saved";
            }

            return response;
        }

        private static void SaveVariables() {
            string text = "";
            foreach (string k in variables.Keys) {
                text += $"{k}={variables[k]}\n";
            }
            File.WriteAllText(ProgramData.Directory + "bin\\variables.txt", text);
        }

        public static void LoadVariables() {
            string[] text = File.ReadAllLines(ProgramData.Directory + "bin\\variables.txt");
            foreach (string t in text) {
                if (!t.Contains('='))
                    continue;

                int index = t.IndexOf('=');
                string k = t.Substring(0, index), v = t.Substring(index + 1);
                variables.Add(k, v);
            }
        }

    }
}
