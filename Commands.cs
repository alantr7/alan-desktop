using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Alan {
    class Commands {

        public static void Answer(string[] args) {

            string Command = args[0].ToLower();

            Console.WriteLine("Arguments: " + args.Length);

            switch (Command) {
                case "http":
                    using (WebClient wc = new WebClient()) {
                        string RequestType  = GetArgument(args, "type", "get");
                        string Url = GetArgument(args, "url");
                        
                        if (RequestType == "get") {
                            Console.WriteLine(wc.DownloadString(Url));
                            return;
                        }

                        Console.WriteLine($"Request Type: {RequestType}");
                    }
                    break;
            }

        }

        public static string GetArgument(string[] args, string Name, string Default = "") {
            
            foreach (string a in args) {

                string ArgumentName = a.Split(':')[0];
                if (ArgumentName.ToLower().Equals(Name.ToLower())) {

                    string Value = a.Substring(ArgumentName.Length + 1);
                    return Value;

                }

            }

            return Default;
        }

    }
}
