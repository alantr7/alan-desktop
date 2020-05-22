using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;

namespace Alan {

    class Controller {

        //public static string URL = "http://localhost/alan/";
        public static string URL = "http://alantr7.uwebweb.com/alan/";

        public static string DEVICE_ID = GetMACAddress();

        public static void Main(string[] args) {

            Thread.Sleep(1000);

            if (Process.GetProcessesByName("Alan").Length > 1) return;

            CreateDirectories();
            CheckForUpdate();

            string DirectoryName = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (!DirectoryName.ToLower().Contains("appdata")) {
                Process.Start(new ProcessStartInfo() {
                    FileName = Environment.GetEnvironmentVariable("APPDATA") + "\\Alan\\alan.exe"
                });
                Environment.Exit(0);
            }

            Console.WriteLine("DEVICE ID: " + DEVICE_ID);

            ProcessTracker.Start();
            ScreenShare.Start();

            Server.CreateServer();

        }

        public static void CreateDirectories() {
            Directory.CreateDirectory(Environment.GetEnvironmentVariable("APPDATA") + "\\Alan");
            Directory.CreateDirectory(Environment.GetEnvironmentVariable("APPDATA") + "\\Alan\\logs");
        }

        public static void CheckForUpdate() {
            Updater Updater = new Updater("https://raw.githubusercontent.com/alantr7/alan-desktop/master/updater", Environment.GetEnvironmentVariable("APPDATA") + "\\Alan", 4);

            Updater.SetRequiredFiles(new string[] {
                "ngrok.exe", "update.bat"
            });

            if (Updater.UpdateExists()) {
                Console.WriteLine($"Postoji nova verzija : {Updater.GetLatestVersion()}");

                Updater.InstallUpdate();
            }
            else Console.WriteLine($"Zadnja verzija instalirana : {Updater.GetLatestVersion()}");
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

            Server.Broadcast("{\"action\":\"log\",\"time\":\"\",\"log\":\"" + line.Replace("\"", "\\\"") + "\"}");

        }

    }
}