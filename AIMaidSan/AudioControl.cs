using NAudio.Wave;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace AIMaidSan
{
    public class AudioControl
    {
        public delegate void StartAudio(string text);
        public event StartAudio? StartAudioEvent;

        private VoiceVox voiceVox;
        public AudioControl(VoiceVox voiceVox)
        {
            Console.WriteLine("@@@@@@@@@@@@@@@@@AudioControl");
            this.voiceVox = voiceVox;
        }

        public async Task SpeakMulti(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            foreach (string line in text.Split('。'))
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
                var trim_line = line.Trim();
                await Speak(trim_line);
            }
        }

        private SemaphoreSlim speakSemaphore = new(1);

        public async Task Speak(string? text)
        {
            if (string.IsNullOrEmpty(text)) return;

            while (Playing)
            {
                await Task.Delay(500);
            }

            var stream = await this.voiceVox.GetAudioStream(text);

            try
            {
                if (StartAudioEvent != null)
                {
                    await Task.Run(() =>
                    {
                        StartAudioEvent(text);
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            if (stream != null)
            {
                await speakSemaphore.WaitAsync();
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
                    Console.WriteLine($"Audio Stop : {e.Exception}");
                    Playing = false;
                    speakSemaphore.Release();
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
