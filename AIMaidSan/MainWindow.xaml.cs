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

namespace AIMaidSan
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        VoiceVox voicevox = new VoiceVox();
        WindowInfoControl windowInfoControl;

        public MainWindow()
        {
            InitializeComponent();

            windowInfoControl = new WindowInfoControl(Dispatcher);

            this.MouseLeftButtonDown += (sender, e) => { this.DragMove(); };
            Console.CancelKeyPress += Console_CancelKeyPress;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Console.WriteLine("Window_Loaded");

            voicevox.voiceVoxReady += Voicevox_voiceVoxReady;
            var _ = voicevox.StartVoiceVox();

            windowInfoControl.ChangeWindowEvent += WindowInfoControl_ChangeWindowEvent;
        }

        private void WindowInfoControl_ChangeWindowEvent(string windowTitle, string processName, string? productName, string? fileName, int lookTimeMin)
        {
            Console.WriteLine($"{windowTitle} ({lookTimeMin} min)");
            Dispatcher.Invoke(() =>
            {
                if (windowTitle == "AIMaidSan MainWindow" && lookTimeMin == 0)
                {
                    var _ = Speak("どうかしましたか？");
                }
            });
        }

        private void Voicevox_voiceVoxReady()
        {
            var _ = Speak("ご主人様。用件がございましたらお声がけください。");
        }

        private async Task Speak(string text)
        {
            while (Playing)
            {
                await Task.Delay(1000);
            }
            Playing = true;

            var stream = await voicevox.GetAudioStream(text);

            try
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
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            if (stream != null)
            {
                await Task.Run(() =>
                {
                    PlayVoice(stream);
                });
            }
        }

        delegate void PlaybackStopped(StoppedEventArgs e);

        class VoiceInfo : IDisposable
        {
            public event PlaybackStopped? OnPlaybackStopped;
            public VoiceInfo(Stream? voice, bool onetime = true)
            {
                VoiceStream = voice;
                outputDevice = new WaveOutEvent();
                outputDevice.PlaybackStopped += (object? sender, StoppedEventArgs e) =>
                {
                    OnPlaybackStopped?.Invoke(e);
                    if (onetime)
                    {
                        this.Dispose();
                    }
                };
            }

            public void Play()
            {
                if (VoiceStream != null) 
                    VoiceStream.Position = 0;
                audioFile = new WaveFileReader(VoiceStream);
                if (outputDevice != null)
                {
                    outputDevice.Init(audioFile);
                    outputDevice.Play();
                }
            }

            public Stream? VoiceStream;
            public WaveOutEvent? outputDevice;
            public WaveFileReader? audioFile;

            public void Dispose()
            {
                audioFile?.Dispose();
                audioFile = null;
                outputDevice?.Dispose();
                outputDevice = null;
                VoiceStream?.Dispose();
                VoiceStream = null;
            }
        }

        private object playing_lock = new object();
        private bool playing_value = false;
        public bool Playing
        {
            get { lock (playing_lock) { return playing_value; } }
            set { lock (playing_lock) { playing_value = value; Console.WriteLine($"Playing : {value}"); } }
        }

        private VoiceInfo? voiceInfo;

        private void PlayVoice(MemoryStream voice)
        {
            try
            {
                voiceInfo = new VoiceInfo(voice);
                voiceInfo.OnPlaybackStopped += (StoppedEventArgs e) =>
                {
                    Console.WriteLine(e.Exception);
                    Playing = false;
                };
                voiceInfo.Play();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PlayVoice Error: {ex.Message}");
                Playing = false;
            }
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