using SuperWebSocket;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Alan.command {
    class cmdHttp {

        public static string Answer(WebSocketSession client, string line) {

            Command.RequireParameters(line, "url", "data");

            string subc = Command.AnySubcommand("get", "post");
            return Request(Command.GetString(line, "url"), subc, Command.GetString(line, "data"));

        }

        public static string Request(string url, string method, params string[] data) {
            return RequestWithCookies(url, method, "", data);
        }

        public static string RequestWithCookies(string url, string method, string cookie, params string[] data) {
            using (WebClient wc = new WebClient()) {

                if (cookie != null && cookie.Length > 0)
                    wc.Headers.Add(HttpRequestHeader.Cookie, cookie);

                if (method == "post") {
                    wc.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
                    
                    var collection = new System.Collections.Specialized.NameValueCollection();
                    foreach (string d in data) {
                        if (!d.Contains('=')) continue;
                        string k = d.Substring(0, d.IndexOf('='));
                        string v = d.Substring(d.IndexOf('=') + 1);

                        collection.Add(k, v);
                    }

                    return wc.UploadValues(url, collection).ToString();
                }
                string furl = url;
                if (data.Length > 0) {
                    furl += "?" + data[0];
                    for (int i = 1; i < data.Length; i++)
                        furl += "&" + data[i];
                }
                return wc.DownloadString(furl);
            }
        }
    }
}
