using AIMaidSan.Properties;
using System;
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
        VoiceVox voicevox;
        WindowInfoControl windowInfoControl;
        AudioControl audioControl;

        AICharacter charactorSettings;

        public MainWindow()
        {
            InitializeComponent();

            voicevox = new VoiceVox();
            voicevox.voiceVoxReady += Voicevox_voiceVoxReady;
            var _ = voicevox.StartVoiceVox();

            windowInfoControl = new WindowInfoControl(Dispatcher);
            audioControl = new AudioControl(voicevox);

            charactorSettings = new AICharacter(Settings.Default.Name, GetAPIKey());
            main_image.Source = new BitmapImage(new Uri(charactorSettings.BaseImage, UriKind.Relative));

            this.MouseLeftButtonDown += (sender, e) => { this.DragMove(); };
            Console.CancelKeyPress += Console_CancelKeyPress;
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

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Window_Loaded");

            windowInfoControl.ChangeWindowEvent += WindowInfoControl_ChangeWindowEvent;
            audioControl.StartAudioEvent += AudioControl_startAudio;
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

        private void WindowInfoControl_ChangeWindowEvent(string windowTitle, string processName, string? productName, string? fileName, int lookTimeMin)
        {
            Console.WriteLine($"{processName} ({lookTimeMin} min)");

            Console.WriteLine($"windowTitle = {windowTitle}");
            Console.WriteLine($"processName = {processName}");
            Console.WriteLine($"productName = {productName}");
            Console.WriteLine($"fileName = {fileName}");
            Console.WriteLine($"lookTimeMin = {lookTimeMin}");

            if (windowTitle == "AIMaidSan MainWindow" && lookTimeMin == 0)
            {
                var _ = audioControl.Speak("どうかしましたか？");
            }
        }

        private void Voicevox_voiceVoxReady()
        {
            var _ = audioControl.SpeakMulti(charactorSettings.Taking().Result);
        }


        private void main_image_Loaded(object sender, RoutedEventArgs e)
        {
            MaidWindow.Height = SystemParameters.WorkArea.Height / 2.5;
            MaidWindow.Width = MaidWindow.Height * (main_image.Source.Width / main_image.Source.Height);

            MaidWindow.Top = SystemParameters.WorkArea.Height - MaidWindow.Height;
            MaidWindow.Left = SystemParameters.WorkArea.Width - MaidWindow.Width - (SystemParameters.WorkArea.Width / 20);

            Task _;
            _ = FadeInWindow();
            _ = windowInfoControl.GetActiveWindowTitle();
        }

        private async Task FadeInWindow()
        {
            for (int i = 0; i < 100; i += 15)
            {
                Dispatcher.Invoke(() =>
                {
                    main_image.Opacity = i / 100.0;
                });
                await Task.Delay(120);
            }
            Dispatcher.Invoke(() =>
            {
                main_image.Opacity = 1.0;
            });
        }

        private void MaidWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            voicevox.ExitVoiceVox();
        }

        private void MaidWindow_Closed(object sender, EventArgs e)
        {
            voicevox.ExitVoiceVox();
        }

        private void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            voicevox.ExitVoiceVox();
        }
    }

}