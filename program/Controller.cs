using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Threading;
using Alan.command;
using Alan.program;
using Alan_API_Wrapper;
using ScreenRecorderLib;

namespace Alan {

    class Controller {

        //public static string URL = "http://localhost/";
        public static string URL = "http://alantr7.uwebweb.com/";

        public static string DEVICE_ID = GetMACAddress();

        public static AlanAPIWrapper APIWrapper;

        public static void Main(string[] args) {

            bool NoProcessTracker = false, DuplicateBinary = false, AnyDirectory = false;
            foreach (string c in args) {
                switch (c) {
                    case "-noprocesstracker":
                        NoProcessTracker = true;
                        break;
                    case "-duplicatebinary":
                        DuplicateBinary = true;
                        break;
                    case "-anydirectory":
                        AnyDirectory = true;
                        break;
                }
                Console.WriteLine($"Parameter found : {c}");
            }

            APIWrapper = new AlanAPIWrapper(Controller.URL, "Nt209SSknJyTsF4OM91r");

            ProgramData.Load();
            Console.WriteLine("PC Booted at: " + ProgramData.SystemBoot);

            Thread.Sleep(1000);

            if (Process.GetProcessesByName("Alan").Length > 1 && !DuplicateBinary) return;

            CreateDirectories();
            CheckForUpdate();

            string DirectoryName = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
            if (!DirectoryName.ToLower().Contains("appdata") && !AnyDirectory) {
                Process.Start(new ProcessStartInfo() {
                    FileName = Environment.GetEnvironmentVariable("APPDATA") + "\\Alan\\alan.exe"
                });
                Environment.Exit(0);
            }

            Console.WriteLine("DEVICE ID: " + DEVICE_ID);

            if (NoProcessTracker) {
                Console.WriteLine("-noprocesstracker parameter found.");
            } else ProcessTracker.Start();

            try {
                string r = cmdHttp.RequestWithCookies($"{Controller.URL}user/notif/send", "post", ProgramData.LoginCookie,
                       $"{{\"cause\":\"pc_on\",\"device_id\":\"{Controller.DEVICE_ID}\"}}");
            }
            catch { }

            ScreenShare.Start();
            Server.CreateServer();

        }

        public static void CreateDirectories() {
            Directory.CreateDirectory(Environment.GetEnvironmentVariable("APPDATA") + "\\Alan");
            Directory.CreateDirectory(Environment.GetEnvironmentVariable("APPDATA") + "\\Alan\\logs");
        }

        public static void CheckForUpdate() {
            try {
                Updater Updater = new Updater("https://raw.githubusercontent.com/alantr7/alan-desktop/master/updater", Environment.GetEnvironmentVariable("APPDATA") + "\\Alan", 7);

                Updater.SetRequiredFiles(new string[] {
                "bin\\ngrok.exe", "bin\\update.bat"
            });

                if (Updater.UpdateExists()) {
                    Console.WriteLine($"Postoji nova verzija : {Updater.GetLatestVersion()}");

                    Updater.InstallUpdate();
                }
                else Console.WriteLine($"Zadnja verzija instalirana : {Updater.GetLatestVersion()}");
            } catch {
                Console.WriteLine("Doslo je do greske prilikom provjere update-a");
            }
        }

        public static string GetMACAddress() {
            NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces();
            string sMacAddress = string.Empty;
            foreach (NetworkInterface adapter in nics) {
                if (sMacAddress == string.Empty)// only return MAC Address from first card  
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

            //Server.Broadcast("{\"action\":\"log\",\"time\":\"\",\"log\":\"" + line.Replace("\"", "\\\"") + "\"}");
        }

    }
}