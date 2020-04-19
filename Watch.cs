using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Alan {
    class Watch {

        private static IWebDriver wd;
        private static string imdbid = "", title = "";

        public static void ChromeDriver() {

            wd = new ChromeDriver(Environment.GetEnvironmentVariable("APPDATA") + "\\Alan\\chromedriver-80");
            wd.Url = "http://v2.vidsrc.me";
            wd.Manage().Cookies.AddCookie(new Cookie("ops_login", "%7B%22user%22%3A%22alant7_%22%2C%22pass%22%3A%228699ebf48a4fd99c31d5ef88c0c27f40%22%7D"));
            wd.Manage().Cookies.AddCookie(new Cookie("ops_langs", "%5Bhrv%5D"));

        }

        public static string GetCurrentMedia() {
            return $"{{\"imdbid\":\"{imdbid}\",\"title\":\"{title}\"}}";
        }

        public static void Play(string imdbid, string type = "movie", int season = 0, int episode = 0) {
            Watch.imdbid = imdbid;

            if (wd == null) {
                ChromeDriver();
                wd.Url = $"http://vidsrc.me/embed/{imdbid}/" + (type == "serie" ? (season + "-" + episode) : "");

                IJavaScriptExecutor js = (IJavaScriptExecutor)wd;
                js.ExecuteScript("ops = new opensubtitles({user: \"alant7_\", pass: md5(\"nBWXJDj4jCMU.tx\")});ops.login();");

                Thread.Sleep(2000);

                js.ExecuteScript("ops.getSubs(" + imdbid.Substring(2) + "), ['Croatian']");

                Thread.Sleep(2000);

                js.ExecuteScript($"$.cookie(\"current_sub\", JSON.stringify({{imdb:\"{imdbid.Substring(2)}\",url:ops.search_data[0].SubtitlesLink}}, {{path:'/'}}))");
                wd.Navigate().Refresh();

                Thread.Sleep(2000);
                wd.Navigate().Refresh();
            }
        }

        public static void Stop() {
            wd.Dispose();
            wd = null;
        }

    }
}
