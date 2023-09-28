using AIMaidSan.Properties;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace AIMaidSan
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private VoiceVox voicevox;
        private WindowInfoControl windowInfoControl;
        private AudioControl audioControl;
        private AICharacter charactorSettings;

        private bool SystemReady = false;

        public MainWindow()
        {
            InitializeComponent();

            this.MouseLeftButtonDown += (sender, e) => { this.DragMove(); };
            Console.CancelKeyPress += Console_CancelKeyPress;

            voicevox = new VoiceVox();
            voicevox.VoiceVoxReadyEvent += Voicevox_voiceVoxReady;

            windowInfoControl = new WindowInfoControl(Dispatcher);
            windowInfoControl.ChangeWindowEvent += WindowInfoControl_ChangeWindowEvent;

            audioControl = new AudioControl(voicevox);
            audioControl.StartAudioEvent += AudioControl_startAudio;

            charactorSettings = new AICharacter(Settings.Default.Name, GetAPIKey());
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Window_Loaded");
            var _ = voicevox.StartVoiceVox();

            main_image.Source = new BitmapImage(new Uri(charactorSettings.BaseImage, UriKind.Relative));
        }

        public string GetAPIKey()
        {
            var apiKey = Settings.Default.API;
            if (string.IsNullOrEmpty(apiKey) && File.Exists("apikey.txt"))
            {
                apiKey = File.ReadAllText("apikey.txt").Trim();
            }
            return apiKey;
        }

        private void AudioControl_startAudio(string text)
        {
            Dispatcher.Invoke(() =>
            {
                TalkWindow talkWindow = new TalkWindow();
                talkWindow.SetText(text);
                talkWindow.Left = this.Left - (talkWindow.Width * 1.1);
                talkWindow.Top = this.Top + (talkWindow.Height + (this.Height / 2) - new Random().Next((int)this.Height));
                talkWindow.Show();
            });
        }

        class ProcessLog
        {
            public Dictionary<string, int> titles = new();
            public int totaltime = 0;

            public string product = string.Empty;
            public string exefile = string.Empty;
            public string process = string.Empty;
        }

        private int scold = 0;

        private ConcurrentDictionary<string, ProcessLog> processLog = new ConcurrentDictionary<string, ProcessLog>();

        private void WindowInfoControl_ChangeWindowEvent(string windowTitle, string processName, string? productName, string? fileName, int lookTimeMin)
        {
            Console.WriteLine($"{processName} ({lookTimeMin} min)");
            if (!SystemReady) { return; }

            if (windowTitle == "AIMaidSan MainWindow" && lookTimeMin == 0)
            {
                var _ = audioControl.SpeakMulti(charactorSettings.ShortResponce().Result);
            }
            if (windowTitle == "AIMaidSan MainWindow" || windowTitle == "AIMaidSan InputWindows" || windowTitle == "AIMaidSan ChoiceWindow" || windowTitle == "AIMaidSan TalkWindow") return;

            if (lookTimeMin >= 0)
            {
                if (!processLog.ContainsKey(processName))
                {
                    processLog[processName] = new ProcessLog();
                }
                var process = processLog[processName];
                process.totaltime++;
                process.product = productName ?? string.Empty;
                var trim_title = windowTitle.PadRight(24)[..24].Trim();
                if (!process.titles.ContainsKey(trim_title))
                {
                    process.titles[trim_title] = 0;
                }
                process.titles[trim_title]++;
                process.exefile = fileName ?? string.Empty;
                process.process = processName ?? string.Empty;

                if (process.totaltime > 60)
                {
                    Task.Run(() =>
                    {
                        StringBuilder sb = new StringBuilder();
                        var processThree = processLog.OrderByDescending(pair => pair.Value.totaltime).Take(3).ToDictionary(pair => pair.Key, pair => pair.Value).Values.ToList();
                        for (int i = 0; i < processThree.Count; i++)
                        {
                            var item = processThree[i];
                            var titleThree = item.titles.OrderByDescending(pair => pair.Value).Take(3).ToDictionary(pair => pair, pair => pair.Value).Values.ToList();
                            sb.AppendLine($"Process {i + 1}:");
                            sb.AppendLine($"Product Name = {item.product}");
                            sb.AppendLine($"Process Name = {item.process}");
                            sb.AppendLine($"File Name = {item.exefile}");
                            sb.AppendLine($"Window Titles = [{string.Join(", ", titleThree)}]");
                            sb.AppendLine($"Cumulative working time: {item.totaltime} min.");
                            sb.AppendLine();
                        }

                        var result = charactorSettings.WorkCheck(sb.ToString().Trim()).Result;
                        if (result != null)
                        {
                            if(result["working"] < result["slacking"])
                            {
                                scold++;
                                _ = audioControl.SpeakMulti(charactorSettings.FreeTalk($"It seems like your husband is playing instead of working. Please scold me. This is the {scold} time.").Result);
                            }
                            processLog.Clear();
                        }
                    });
                }
                return;
            }
        }

        private void Voicevox_voiceVoxReady(bool success)
        {
            if (success)
            {
                var _ = audioControl.SpeakMulti("音声システムの準備が出来ました。");
            }
            else
            {
                var _ = audioControl.SpeakMulti("音声システムは使用しません。");
            }

            SystemReady = true;
        }
    }

}