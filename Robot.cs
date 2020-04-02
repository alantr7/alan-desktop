using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using WindowsInput;

namespace Alan {
    class Robot {

        [DllImport("user32.dll", CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        public static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint cButtons, uint dwExtraInfo);
        //Mouse actions
        private const int MOUSEEVENTF_LEFTDOWN = 0x02;
        private const int MOUSEEVENTF_LEFTUP = 0x04;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x08;
        private const int MOUSEEVENTF_RIGHTUP = 0x10;

        private static InputSimulator s = new InputSimulator();

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
            s.Keyboard.TextEntry(ks);
        }

        public static string ScreenshotJson() {

            string data = "";
            
            int SW = Screen.PrimaryScreen.Bounds.Width, SH = Screen.PrimaryScreen.Bounds.Height;

            Bitmap bmp = new Bitmap(SW, SH);

            using (Graphics g = Graphics.FromImage(bmp)) {
                g.CopyFromScreen(0, 0, 0, 0, new Size(SW, SH));

                Bitmap bmp2 = new Bitmap(1024, 600);
                Graphics g2 = Graphics.FromImage(bmp2);
                g2.DrawImage(bmp, 0, 0, 1024, 600);

                Console.WriteLine("Petlja");

                System.IO.MemoryStream stream = new System.IO.MemoryStream();
                bmp2.Save(stream, System.Drawing.Imaging.ImageFormat.Jpeg);
                byte[] imageBytes = stream.ToArray();

                data = Convert.ToBase64String(imageBytes);

                Console.WriteLine("Converting to string");
                Console.WriteLine("Data: " + data);
                Console.WriteLine("Done writing data");

                bmp.Dispose();
                g2.Dispose();
                bmp2.Dispose();
                stream.Dispose();
            }

            return $"{{\"action\":\"pc.screenshare.data\", \"screen\":{{\"w\":{SW},\"h\":{SH}}}, \"data\":\"{data}\"}}";

        }

    }
}
