using SuperWebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alan.command {
    class cmdEnv {

        public static string Answer(WebSocketSession client, string line) {
            string response = "";

            string subc = Command.AnySubcommand(line, "get", "list");
            switch (subc) {
                case "get":

                    Command.RequireParameters(line, "name");
                    string name = Command.GetString(line, "name").Replace("%", "");

                    return Environment.GetEnvironmentVariable(name);
                case "list":
                    IDictionary<string, string> envs = (IDictionary<string, string>)Environment.GetEnvironmentVariables();
                    foreach (string k in envs.Keys) {
                        response += k + "\t\t" + envs[k];
                    }
                    return response;
                case "set":

                    break;
            }

            return response;
        }

    }
}
