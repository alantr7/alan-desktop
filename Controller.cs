using System;
using System.IO;
using System.Net.NetworkInformation;

namespace Alan {

    class Controller {

        public static string URL = "http://localhost/alan/";
        //public static string URL = "http://alantr7.uwebweb.com/alan/";

        public static string DEVICE_ID = GetMACAddress();

        public static void Main() {

            Console.WriteLine("DEVICE ID: " + DEVICE_ID);

            ProcessTracker.Start();
            ScreenShare.Start();

            Server.CreateServer();

            //Watch.Play("tt0095016", "movie");

        }

        public static string GetMACAddress() {
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            String sMacAddress = string.Empty;
            foreach (NetworkInterface adapter in nics) {
                if (sMacAddress == String.Empty)// only return MAC Address from first card  
                {
                    //IPInterfaceProperties properties = adapter.GetIPProperties(); Line is not required
                    sMacAddress = adapter.GetPhysicalAddress().ToString();
                }
            }
            return sMacAddress;

        }
        
        public static void Log(string name, string line) {
            string Path = Environment.GetEnvironmentVariable("APPDATA") + "\\Alan\\logs\\" + DateTime.Now.ToString("ddMMyyyy");
            Directory.CreateDirectory(Path);

            File.AppendAllText(Path + "\\" + name + ".txt", line + "\n");
        }

    }
}