using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Alan {
    class Robot {

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);
        //Mouse actions
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;

        public static void MouseRightClick() {
            uint X = (uint)Cursor.Position.X;
            uint Y = (uint)Cursor.Position.Y;
            mouse_event(MOUSEEVENTF_RIGHTDOWN | MOUSEEVENTF_RIGHTUP, X, Y, 0, 0);
        }

        public static void MouseLeftClick() {
            uint X = (uint)Cursor.Position.X;
            uint Y = (uint)Cursor.Position.Y;
            mouse_event(MOUSEEVENTF_LEFTDOWN | MOUSEEVENTF_LEFTUP, X, Y, 0, 0);
        }

        public static void KeyboardWrite(string ks) {

            ks = ks.ToUpper();

            for (int i = 0; i < ks.Length; i++) {
                List<byte> Keys = new List<byte>();
                if (ks[i] == '{') {
                    int iof;
                    if ((iof = ks.Substring(i).IndexOf('}')) != -1) {
                        Console.WriteLine("i : " + i + ", iof : " + iof);

                        string content = ks.Substring(i + 1, iof - 1);
                        string[] SpecialKeys = content.Contains("+") ? content.Split('+') : new string[] { content };

                        foreach (string SpecialKey in SpecialKeys) {
                            byte? CurrentKey = null;
                            switch (SpecialKey) {
                                case "ENTER":
                                    CurrentKey = 0x0D;
                                    break;
                                case "ALT":
                                    CurrentKey = 0x12;
                                    break;
                                case "CTRL":
                                    CurrentKey = 0x11;
                                    break;
                                case "TAB":
                                    CurrentKey = 0x09;
                                    break;
                                case "START":
                                    CurrentKey = 0x5B;
                                    break;
                                case "ESC":
                                    CurrentKey = 0x1B;
                                    break;
                            }
                            if (SpecialKey.StartsWith("F")) {
                                string fnum = SpecialKey.Substring(1);
                                try {
                                    CurrentKey = (byte)(0x70 + (byte)Int32.Parse(fnum) - 1);
                                }
                                catch { }
                                Console.WriteLine("FNUM : " + fnum);
                            }

                            if (!CurrentKey.HasValue) {
                                if (content.EndsWith("MS") && !content.Contains("+")) {

                                    int ms = Int32.Parse(SpecialKey.Substring(0, SpecialKey.Length - 2));
                                    Console.WriteLine("Waiting " + ms);

                                    Thread.Sleep(ms);

                                    continue;
                                } else
                                CurrentKey = (byte)SpecialKey[0];
                            }

                            Keys.Add(CurrentKey.Value);

                            Console.WriteLine("Special Key: " + SpecialKey);
                        }

                        i += iof;
                    }
                } else {
                    Keys.Add((byte)ks[i]);
                }

                foreach (byte b in Keys) {
                    Console.WriteLine("Pressed: " + b);
                    KeyDown(b);
                }
                foreach (byte b in Keys) {
                    Console.WriteLine("Released: " + b);
                    KeyUp(b);
                }

            }
        }

        [DllImport("user32.dll")]
        public static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, int dwExtraInfo);

        public static void KeyDown(byte b) {
            keybd_event(b, 0, 0x00, 0);
        }

        public static void KeyUp(byte b) {
            keybd_event(b, 0, 0x02, 0);
        }

        public static async Task KeyPress(byte b) {
            KeyDown(b);
            await Task.Delay(1000);
            KeyUp(b);
        }
        
        public static string ScreenshotJson() {

            string data = "";
            
            int SW = Screen.PrimaryScreen.Bounds.Width, SH = Screen.PrimaryScreen.Bounds.Height;

            Bitmap bmp = new Bitmap(SW, SH);

            using (Graphics g = Graphics.FromImage(bmp)) {
                g.CopyFromScreen(0, 0, 0, 0, new Size(SW, SH));

                Bitmap bmp2 = new Bitmap(ScreenShare.STREAM_SIZE[0], ScreenShare.STREAM_SIZE[1]);
                Graphics g2 = Graphics.FromImage(bmp2);
                g2.DrawImage(bmp, 0, 0, ScreenShare.STREAM_SIZE[0], ScreenShare.STREAM_SIZE[1]);

                System.IO.MemoryStream stream = new System.IO.MemoryStream();
                bmp2.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);
                byte[] imageBytes = stream.ToArray();

                data = Convert.ToBase64String(imageBytes);

                Console.WriteLine("Bytes - Base64: " + data.Length);
                Console.WriteLine("Bytes: " + imageBytes.Length);
                data = UTF8Encoding.UTF8.GetString(imageBytes);

                bmp.Dispose();
                g2.Dispose();
                bmp2.Dispose();
                stream.Dispose();
            }

            return $"{{\"action\":\"pc.screenshare.data\", \"screen\":{{\"w\":{ScreenShare.STREAM_SIZE[0]},\"h\":{ScreenShare.STREAM_SIZE[1]},\"fps\":{ScreenShare.STREAM_FRAMES}}}, \"data\":\"{data}\"}}";

        }

        public static Bitmap Screenshot() {
            int SW = Screen.PrimaryScreen.Bounds.Width, SH = Screen.PrimaryScreen.Bounds.Height;

            Bitmap bmp = new Bitmap(SW, SH);

            using (Graphics g = Graphics.FromImage(bmp)) {
                g.CopyFromScreen(0, 0, 0, 0, new Size(SW, SH));

                Bitmap bmp2 = new Bitmap(ScreenShare.STREAM_SIZE[0], ScreenShare.STREAM_SIZE[1]);
                Graphics g2 = Graphics.FromImage(bmp2);
                g2.DrawImage(bmp, 0, 0, ScreenShare.STREAM_SIZE[0], ScreenShare.STREAM_SIZE[1]);

                bmp.Dispose();
                g2.Dispose();

                return bmp2;
            }
        }

    }
}
