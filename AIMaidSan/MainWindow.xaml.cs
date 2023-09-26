using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using OpenAI;
using System.Diagnostics;
using System.Threading;
using System.Runtime.InteropServices;
using System.Net.Http;
using System.IO;
using System.Net;
using System.Numerics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using MeCab;
using System.Xml.Linq;
using System.Media;
using NAudio.Wave;
using static System.Net.Mime.MediaTypeNames;

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

        public MainWindow()
        {
            InitializeComponent();

            voicevox = new VoiceVox();
            windowInfoControl = new WindowInfoControl(Dispatcher);
            audioControl = new AudioControl(voicevox);

            this.MouseLeftButtonDown += (sender, e) => { this.DragMove(); };
            Console.CancelKeyPress += Console_CancelKeyPress;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Window_Loaded");

            voicevox.voiceVoxReady += Voicevox_voiceVoxReady;
            windowInfoControl.ChangeWindowEvent += WindowInfoControl_ChangeWindowEvent;
            audioControl.startAudio += AudioControl_startAudio;

            var _ = voicevox.StartVoiceVox();
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
            Console.WriteLine($"{windowTitle} ({lookTimeMin} min)");
            Dispatcher.Invoke(() =>
            {
                if (windowTitle == "AIMaidSan MainWindow" && lookTimeMin == 0)
                {
                    var _ = audioControl.Speak("どうかしましたか？");
                }
            });
        }

        private void Voicevox_voiceVoxReady()
        {
            var _ = audioControl.Speak("ご主人様。用件がございましたらお声がけください。");
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