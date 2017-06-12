using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
//using Oraycn.MCapture;
//using ESBasic;
//using NAudio.Wave;

namespace InGameDanmaku
{
    public class InGameDanmaku : BilibiliDM_PluginFramework.DMPlugin
    {
        public static string DefaultTitle = "default title";
        private static readonly string UserDocumentsPath = @Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        private static readonly string Classpath = UserDocumentsPath + @"\InGameDanmaku";
        private static readonly string PluginPath = UserDocumentsPath + @"\弹幕姬\Plugins";
        public static string TitleFilepath = Classpath + @"\in_game_danmaku_title_name.txt";
        public static string DanmakuFilepath = Classpath + @"\in_game_danmakus.txt";

        private static readonly Translate Translator = new Translate();
        private static Manage _manageForm;
        private readonly ProcessStartInfo _rtssProcInfo;
        //        private readonly ProcessStartInfo _sttInfo;
        private Process _rtssProc;
        //        private Process _sttProc;

        //        private IMicrophoneCapturer _microphoneCapturer;
        //        private static readonly WaveFormat DefaultWavFormat = new WaveFormat(16000, 16, 1);
        //private static WaveFileWriter _wavWriter;
        //        private static Timer _wavTimer;
        //        private static int _wavCounter = 0;
        private static Timer _danmakuTimer;
        private static readonly int DanmakuExpireSeconds = 20;
        private static readonly SortedDictionary<string, DateTime> DanmakuMap = new SortedDictionary<string, DateTime>();

        public InGameDanmaku()
        {
            Connected += InGameDanmaku_Connected;
            Disconnected += InGameDanmaku_Disconnected;
            ReceivedDanmaku += InGameDanmaku_ReceivedDanmaku;
            ReceivedRoomCount += InGameDanmaku_ReceivedRoomCount;
            PluginAuth = "Shuieryin";
            PluginName = "In game danmaku";
            PluginCont = "shuieryin@gmail.com";
            PluginVer = "0.0.1";
            PluginDesc = "2333333";

            if (!Directory.Exists(Classpath))
            {
                Directory.CreateDirectory(Classpath);
            }

            string rtssPath = PluginPath + @"\RTSSSharedMemorySample.exe";
            _rtssProcInfo = new ProcessStartInfo();
            _rtssProcInfo.FileName = rtssPath;
            _rtssProcInfo.Arguments = "";

            ProcessStartInfo killExistingRtssProc = new ProcessStartInfo();
            killExistingRtssProc.FileName = "Taskkill.exe";
            killExistingRtssProc.Arguments = "/F /IM RTSSSharedMemorySample.exe";
            Process.Start(killExistingRtssProc);

            //            ProcessStartInfo killExistingIatProc = new ProcessStartInfo();
            //            killExistingIatProc.FileName = "Taskkill.exe";
            //            killExistingIatProc.Arguments = "/F /IM iat_sample.exe";
            //            Process.Start(killExistingIatProc);

            _manageForm = new Manage();
            UpdateTextBox();
            // ClearSpeechWavFiles();

            //            _sttInfo = new ProcessStartInfo();
            //            _sttInfo.FileName = PluginPath + @"\iat_sample.exe";
            //            _sttInfo.Arguments = Classpath;
        }


        private void InGameDanmaku_ReceivedRoomCount(object sender, BilibiliDM_PluginFramework.ReceivedRoomCountArgs e)
        {
            // throw new NotImplementedException();
        }

        private void InGameDanmaku_ReceivedDanmaku(object sender, BilibiliDM_PluginFramework.ReceivedDanmakuArgs e)
        {
            try
            {
                string curDanmaku = e.Danmaku.CommentText.Trim();

                if (String.IsNullOrEmpty(curDanmaku))
                {
                    return;
                }
                UpdateDanmaku(Translate(e.Danmaku.CommentUser) + " : " + Translate(curDanmaku));
            }
            catch (Exception)
            {
            }

            // Process.Start(@startupPath);
            // MessageBox.Show(@startupPath);
            // MessageBox.Show(e.Danmaku.CommentText);
            // throw new NotImplementedException();
        }

        private void InGameDanmaku_Disconnected(object sender, BilibiliDM_PluginFramework.DisconnectEvtArgs e)
        {
            // throw new NotImplementedException();
        }

        private void InGameDanmaku_Connected(object sender, BilibiliDM_PluginFramework.ConnectedEvtArgs e)
        {
            // throw new NotImplementedException();
        }

        public override void Admin()
        {
            base.Admin();

            _manageForm.ShowDialog();

            // Console.WriteLine("Hello World");
            // this.Log("Hello World");
            // this.AddDM("Hello World",true);
        }

        public override void Stop()
        {
            if (null != _rtssProc)
            {
                _rtssProc.Kill();
                _rtssProc.Close();
                _rtssProc = null;
            }

            //            _sttProc.Kill();
            //            _sttProc.Close();
            //            _sttProc = null;

            base.Stop();
            //請勿使用任何阻塞方法
            Console.WriteLine("Plugin Stoped!");
            Log("Plugin Stoped!");
            AddDM("Plugin Stoped!", true);

            //            _microphoneCapturer.Stop();

            //            if (null != _wavWriter)
            //            {
            //                _wavWriter.Close();
            //            }

            // ClearSpeechWavFiles();
            _danmakuTimer.Stop();
        }

        public override void Start()
        {
            base.Start();
            //請勿使用任何阻塞方法
            Console.WriteLine("Plugin Started!");
            Log("Plugin Started!");
            AddDM("Plugin Started!", true);

            try
            {
                _rtssProc = Process.Start(_rtssProcInfo);
            }
            catch (System.ComponentModel.Win32Exception ex)
            {
                MessageBox.Show(_rtssProcInfo.FileName);
                AddDM(ex.ToString());
            }

            // CollectSpeech();

            //            _wavTimer = new Timer();
            //            _wavTimer.Tick += new EventHandler(ResetWavWriter);
            //            _wavTimer.Interval = 10000; // in miliseconds
            //            _wavTimer.Start();

            //            _sttProc = Process.Start(_sttInfo);

            _danmakuTimer = new Timer();
            _danmakuTimer.Tick += new EventHandler(DeleteDanmaku);
            _danmakuTimer.Interval = 1000;
            _danmakuTimer.Start();
        }


        private static void DeleteDanmaku(object sender, EventArgs e)
        {
            if (DanmakuMap.Count == 0)
            {
                return;
            }

            try
            {
                File.Delete(DanmakuFilepath);

                DateTime now = DateTime.Now;

                List<string> danmakusToBeRemoved = new List<string>();
                List<string> danmakuToBeDisplayed = new List<string>();
                foreach (KeyValuePair<string, DateTime> danmaku in DanmakuMap)
                {
                    if (DateTime.Compare(now, danmaku.Value) > 0)
                    {
                        danmakusToBeRemoved.Add(danmaku.Key);
                    }
                    else
                    {
                        danmakuToBeDisplayed.Insert(0, danmaku.Key);
                    }
                }

                File.WriteAllLines(DanmakuFilepath, danmakuToBeDisplayed);

                foreach (string danmakuToBeRemoved in danmakusToBeRemoved)
                {
                    DanmakuMap.Remove(danmakuToBeRemoved);
                }
            }
            catch (Exception)
            {

            }
        }

        //        private void ResetWavWriter(object sender, EventArgs e)
        //        {
        //            if (null != _wavWriter)
        //            {
        //                _wavWriter.Close();
        //
        //                _wavWriter = null;
        //            }
        //        }

        public static void UpdateTextBox()
        {
            _manageForm.textBox1.Text = GetTitle();
        }

        private static string GetTitle()
        {
            string titleContent;
            if (!File.Exists(TitleFilepath))
            {
                using (StreamWriter sw = File.CreateText(TitleFilepath))
                {
                    titleContent = DefaultTitle;
                    sw.WriteLine(titleContent);
                }
            }
            else
            {
                using (StreamReader sr = File.OpenText(TitleFilepath))
                {
                    titleContent = sr.ReadLine();
                }
            }

            return titleContent;
        }

        public static void UpdateTitle()
        {
            if (File.Exists(TitleFilepath))
            {
                File.Delete(TitleFilepath);
            }

            using (StreamWriter sw = File.CreateText(TitleFilepath))
            {
                sw.WriteLine(_manageForm.textBox1.Text);
            }
        }

        public static void UpdateDanmaku(string danmaku)
        {
            DanmakuMap.Add(danmaku, DateTime.Now.AddSeconds(DanmakuExpireSeconds));
        }

        //        private void CollectSpeech()
        //        {
        //            try
        //            {
        //                _microphoneCapturer = CapturerFactory.CreateMicrophoneCapturer(0);
        //                _microphoneCapturer.AudioCaptured += new CbGeneric<byte[]>(MicrophoneCapturerAudioCaptured);
        //                _microphoneCapturer.Start();
        //            }
        //            catch (Exception ee)
        //            {
        //                MessageBox.Show(ee.Message);
        //            }
        //        }

        //        private static void MicrophoneCapturerAudioCaptured(byte[] audioData)
        //        {
        //            if (null == _wavWriter)
        //            {
        //                _wavWriter = new WaveFileWriter(Classpath + @"\speech_" + _wavCounter + ".wav", DefaultWavFormat);
        //
        //                if (_wavCounter < 3)
        //                {
        //                    _wavCounter++;
        //                }
        //                else
        //                {
        //                    _wavCounter = 0;
        //                }
        //            }
        //
        //            _wavWriter.Write(audioData, 0, audioData.Length);
        //        }

        //        private static void ClearSpeechWavFiles()
        //        {
        //            DirectoryInfo di = new DirectoryInfo(Classpath);
        //            foreach (FileInfo file in di.GetFiles())
        //            {
        //                if (file.Name.StartsWith("speech_"))
        //                {
        //                    file.Delete();
        //                }
        //            }
        //        }

        public static string Translate(string text)
        {
            return Translator.Main(text);
        }
    }
}