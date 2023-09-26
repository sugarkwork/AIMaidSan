using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace AIMaidSan
{
    public class AudioControl
    {
        public delegate void StartAudio(string text);
        public event StartAudio? startAudio;

        private VoiceVox voiceVox;
        public AudioControl(VoiceVox voiceVox)
        {
            this.voiceVox = voiceVox;
        }

        public async Task Speak(string text)
        {
            while (Playing)
            {
                await Task.Delay(1000);
            }
            Playing = true;

            var stream = await this.voiceVox.GetAudioStream(text);

            try
            {
                if (startAudio != null)
                {
                    await Task.Run(() =>
                    {
                        startAudio(text);
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            if (stream != null)
            {
                await Task.Run(() =>
                {
                    PlayVoice(stream);
                });
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
                    Console.WriteLine($"Audio Stop : {e}");
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
    }

    public class VoiceInfo : IDisposable
    {
        public delegate void PlaybackStopped(StoppedEventArgs e);

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
}
