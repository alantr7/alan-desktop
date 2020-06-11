using SuperWebSocket;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.IO;
using System;
using ScreenRecorderLib;
using System.Threading.Tasks;

namespace Alan {
    class ScreenShare {

        public static List<WebSocketSession> s = new List<WebSocketSession>();
        public static int[] STREAM_SIZE = new int[] { 800, 450 };
        public static int STREAM_FRAMES = 10, STREAM_SECONDS = 10, STREAM_BITRATE = 300000;

        private static Recorder recorder, recorder2;
        private static bool RecordersReset = true;

        public static async void Start() {

            Directory.CreateDirectory(Environment.GetEnvironmentVariable("APPDATA") + "\\Alan\\screenshare");

            RecorderOptions options = new RecorderOptions {
                VideoOptions = new VideoOptions()
            };
            options.VideoOptions.BitrateMode = BitrateControlMode.Quality;
            
            recorder = Recorder.CreateRecorder(options);
            recorder2 = Recorder.CreateRecorder(options);

            recorder.OnRecordingComplete += Recorder_OnRecordingComplete;
            recorder2.OnRecordingComplete += Recorder_OnRecordingComplete;

            while (true) {
                try {

                    options.VideoOptions.Bitrate = STREAM_BITRATE;
                    options.VideoOptions.Framerate = STREAM_FRAMES;
                    recorder.SetOptions(options);
                    recorder2.SetOptions(options);

                    if (RecordersReset) {
                        if (s.Count > 0) recorder2.Record(Path.Combine(Environment.GetEnvironmentVariable("APPDATA") + "\\Alan\\screenshare\\" + DateTimeOffset.Now.ToUnixTimeMilliseconds() + ".mp4"));
                            recorder.Stop();
                    } else {
                        recorder2.Stop();
                        if (s.Count > 0) recorder.Record(Path.Combine(Environment.GetEnvironmentVariable("APPDATA") + "\\Alan\\screenshare\\" + DateTimeOffset.Now.ToUnixTimeMilliseconds() + ".mp4"));
                    }

                    RecordersReset = !RecordersReset;
                        
                }
                catch { }   
                await Task.Delay(STREAM_SECONDS * 1000);
            }

        }

        private static void Recorder_OnRecordingComplete(object sender, RecordingCompleteEventArgs e) {
            
            string[] files = Directory.GetFiles(Environment.GetEnvironmentVariable("APPDATA") + "\\Alan\\screenshare");
            if (files.Length > 3)
                for (int i = 0; i < files.Length - 2; i++) {
                    Console.WriteLine("Deleting " + files[i] + "...");
                    File.Delete(files[i]);
                }

            byte[] b = File.ReadAllBytes(e.FilePath);

            for (int i = 0; i < s.Count; i++)
                s[i].Send(b, 0, b.Length);

        }
        public static string PropertiesJson() {
            return $"{{\"action\":\"pc.screenshare.properties\",\"w\":{ScreenShare.STREAM_SIZE[0]},\"h\":{ScreenShare.STREAM_SIZE[1]},\"fps\":{ScreenShare.STREAM_FRAMES},\"seconds\":{STREAM_SECONDS},\"bitrate\":{STREAM_BITRATE}}}";
        }

    }
}
