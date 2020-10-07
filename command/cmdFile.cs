using SuperWebSocket;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Alan.command.Command;

namespace Alan.command
{
    class cmdFile {

        public static string Answer(WebSocketSession client, string line) {

            string response = "";
            line = line
                .Replace("%appdata%", Environment.GetEnvironmentVariable("APPDATA"))
                .Replace("%userprofile%", Environment.GetEnvironmentVariable("USERPROFILE"));

            string mainAction = Command.AnySubcommand(line,
                "create",
                "append",
                "get",
                "write",
                "delete",
                "rename",
                "move",
                "list",
                "copy"
            );

            switch (mainAction) {
                case "create":
                    RequireParameters(line, "l", "name");
                    response = Create(GetString(line, "l"), GetString(line, "name"));
                    break;
                case "append":
                    RequireParameters(line, "");
                    break;
                case "get":
                    RequireParameters(line, "l", "buffer");
                    Console.WriteLine("Lokacija: " + GetString(line, "l"));
                    return Get(client, GetString(line, "l"), GetInt(line, "buffsize"));
                case "delete":
                    RequireParameters(line, "l");
                    response = Delete(GetString(line, "l"));
                    break;
                case "list":
                    RequireParameters(line, "dir");
                    response = cmdFile.List(GetString(line, "dir"));
                    break;
                case "rename":
                    RequireParameters(line, "l", "name");
                    break;
                case "move":
                    RequireParameters(line, "from", "to");
                    break;
            }

            return response;

        }

        public static string Create(string location, string name) {
            return "File successfuly created";
        }

        // TODO: WebSocketClient has Send(b, offset, length); I should use that function for shorter code
        public static string Get(WebSocketSession client, string location, int buffer = 4096) {
            FileInfo fi = new FileInfo(location);
            if (fi.Exists) {
                client.Send($"10|{fi.Name}|{fi.Length}");
                byte[] bytes = File.ReadAllBytes(location);
                int currByte = 0;

                while (currByte < bytes.Length) {

                    int currBuffSize = buffer;
                    if (currByte + currBuffSize + 1 > bytes.Length) {
                        currBuffSize = bytes.Length - currByte;
                    }

                    byte[] buffBytes = new byte[currBuffSize];
                    for (int i = 0; i < currBuffSize; i++) {
                        buffBytes[i] = bytes[currByte + i];
                    }

                    client.Send($"12|{bytes.Length - currByte}");
                    client.Send(buffBytes, 0, buffBytes.Length);
                    currByte += currBuffSize;

                }
                return $"11|FILE_TRANSFER_STOP";
            }
            return $"File na lokaciji §c{location} §7ne postoji";
        }

        public static string Delete(string location) {
            return "File successfuly deleted";
        }

        public static string List(string dir) {

            string response = "";

            List<string> dirs = new List<string>();
            List<string> files = new List<string>();
            if (dir == "/") {
                foreach (DriveInfo drive in DriveInfo.GetDrives()) {
                    files.Add(drive.Name);
                }
            } else {
                if (Directory.Exists(dir)) {
                    foreach (string d in Directory.GetDirectories(dir)) {
                        string name = d.Replace('\\', '/');
                        name = name.Substring(name.LastIndexOf('/') + 1);
                        dirs.Add(name);
                    }
                    foreach (string f in Directory.GetFiles(dir)) {
                        string name = f.Replace('\\', '/');
                        name = name.Substring(name.LastIndexOf('/') + 1);
                        files.Add(name);
                    }
                }
            }

            foreach (string d in dirs) {
                response += $"§aFOLDER\t§7{d}\n";
            }
            foreach (string f in files) {
                response += $"§2FILE\t§7{f}\n";
            }

            return response;

        }

        public static void Rename() {
            
        }

        public static void Move(string from, string to) {

        }

    }
}
