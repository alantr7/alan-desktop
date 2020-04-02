using System;
using System.Net.NetworkInformation;

namespace Alan {

    class Controller {

        public static string URL = "http://localhost/alan/";
        //public static string URL = "http://alantr7.uwebweb.com/alan/";

        public static string DEVICE_ID = GetMACAddress();

        public static void Main() {

            /*Console.WriteLine("DEVICE ID: " + DEVICE_ID);

            ProcessTracker.Start();
            ScreenShare.Start();

            Server.CreateServer();*/

            JSONElement root = JSON.Parse("{\"info\":{\"device\":{\"id\":5,\"ip\":\"192.168.0.1\"},\"browser\":{\"version\":5,\"name\":\"Chr{o}me\"}}}");
            Console.WriteLine(root.c["info"].c["device"]);
            Console.WriteLine(root.c["info"].c["browser"]);

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
        
    }
}