using Alan.command;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Alan.program {
    class ProgramData {

        // Current session ID - Time when app is started in milliseconds
        public static long SessionID = DateTimeOffset.Now.ToUnixTimeMilliseconds();

        // This variable holds ngrok token
        public static string NgrokToken = "2j18eE6RYpHCi2m4VtZ6q_3xwvi4JncLgNtri2D1g1g";

        // This variable holds path to app's directory, probably is never changed
        public static string Directory = Environment.GetEnvironmentVariable("APPDATA") + "\\Alan\\";

        // Login cookie
        public static string LoginCookie = "PHPSESSID=r8i5egc8mn6krc56hdhf86h4pg";

        // This variable holds computer's start time
        public static long SystemBoot = 0;

        // This will load and asign all variables above
        public static void Load() {

            TimeSpan t = TimeSpan.FromMilliseconds(Environment.TickCount);
            DateTime time = DateTime.Now - t;

            SystemBoot = (long)(time - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds;

            cmdVar.LoadVariables();

        }

    }
}
