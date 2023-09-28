using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.NetworkInformation;

namespace AIMaidSan
{
    public class VoiceVox
    {
        public int Port = 50021;
        private bool _ready = false;

        private Process? voiceVoxProcess;
        private string BaseUrl = "http://localhost";

        private const int LINE_LIMIT = 100;
        private const int START_PORT = 50021;
        private const int MAX_PORT_OFFSET = 30000;

        public delegate void VoiceVoxReady(bool success);
        public event VoiceVoxReady? VoiceVoxReadyEvent;
        private int speaker = 58;

        private bool _enable;
        public bool Enable
        {
            get
            {
                return _enable;
            }
            set
            {
                if (value == false && _enable == true)
                {
                    ExitVoiceVox();
                }
                if (value == true && _enable == false)
                {
                    if (!CheckVoiceVox().Result)
                    {
                        Port = FindAvailablePort();
                        var dirPath = Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\Programs\VOICEVOX\");
                        var _ = StartProcessAndGetOutputAsync(dirPath, System.IO.Path.Combine(dirPath, "run.exe"), $"--host localhost --port {Port}");
                    }
                }
                _enable = value;
            }
        }

        public async Task StartVoiceVox(int speaker = 58, bool serviceStart = true)
        {
            this.speaker = speaker;
            await Task.Run(() => {
                Enable = serviceStart;
            });
            if (!serviceStart)
            {
                await Task.Run(() =>
                {
                    VoiceVoxReadyEvent?.Invoke(false);
                });
            }
        }

        public async Task<bool> CheckVoiceVox()
        {
            var targetUrl = new Uri($"{BaseUrl}:{Port}/version");
            try
            {
                using HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
                string content = await client.GetStringAsync(targetUrl);
                await Console.Out.WriteLineAsync($"VoiceVox Version : {content}");

                _enable = true;

                await Task.Run(() =>
                {
                    VoiceVoxReadyEvent?.Invoke(true);
                });

                return true;
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync($"CheckVoiceVox エラー: {targetUrl} => {ex.Message}");
            }
            return false;
        }

        public async Task<MemoryStream?> GetAudioStream(string textContent)
        {
            if (textContent.Length > 512)
            {
                Console.WriteLine($"Truncate String: length = {textContent.Length}");
                textContent = textContent[..512];
            }
            await Console.Out.WriteLineAsync($"Speak: {textContent}");
            if (!Enable) { return null; }

            var queryResponse = await SendRequest($"{BaseUrl}:{Port}/audio_query?speaker={this.speaker}&text={WebUtility.UrlEncode(textContent)}");
            if (queryResponse == null || queryResponse.Length == 0)
            {
                return null;
            }
            var queryJson = Encoding.UTF8.GetString(queryResponse);
            var audioResponse = await SendRequest($"{BaseUrl}:{Port}/synthesis?speaker={this.speaker}&enable_interrogative_upspeak=true", queryJson, "application/json");

            return new MemoryStream(audioResponse.Select(b => b).ToArray());
        }

        public async Task<byte[]> SendRequest(string url, string content = "", string contentType = "text/plain")
        {
            try
            {
                await Console.Out.WriteLineAsync($"SendRequest: {url}");
                HttpClient httpClient = new();
                var response = await httpClient.PostAsync(url, new StringContent(content, Encoding.UTF8, contentType));
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsByteArrayAsync();
            }
            catch (SocketException ex)
            {
                await Console.Out.WriteLineAsync($"SendRequest SocketException: {ex.Message}");
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync($"SendRequest Error: {ex.Message}");
            }
            return Array.Empty<byte>();
        }

        public void ExitVoiceVox()
        {
            if (voiceVoxProcess != null && !voiceVoxProcess.HasExited)
            {
                voiceVoxProcess.Kill();
                voiceVoxProcess.WaitForExit();
            }
            voiceVoxProcess = null;
        }

        public List<string> Logs = new List<string>();
        private readonly object listLock = new object();
        private void AddDataToList(List<string> list, string? data)
        {
            if (data != null)
            {
                lock (listLock)
                {
                    if (list.Count >= LINE_LIMIT)
                    {
                        list.RemoveAt(0);
                    }
                    if (!_ready && (data.Contains("Application startup complete") || data.Contains("emitting double-array: 100%") || data.Contains("done!")))
                    {
                        _ready = true;

                        var _ = Task.Run(() =>
                        {
                            VoiceVoxReadyEvent?.Invoke(true);
                        });
                    }
                    list.Add(data);
                }
            }
        }

        private async Task StartProcessAndGetOutputAsync(string dirPath, string filename, string arguments)
        {
            _ready = false;
            await Task.Run(() =>
            {
                try
                {
                    Process process = new Process();
                    voiceVoxProcess = process;

                    process.StartInfo.WorkingDirectory = dirPath;
                    process.StartInfo.FileName = filename;
                    process.StartInfo.Arguments = arguments;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = true;

                    process.OutputDataReceived += (sender, data) =>
                    {
                        AddDataToList(Logs, data.Data);
                    };

                    process.ErrorDataReceived += (sender, data) =>
                    {
                        AddDataToList(Logs, data.Data);
                    };

                    process.Start();
                    process.BeginOutputReadLine();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            });
        }

        private int FindAvailablePort()
        {
            var usedPorts = GetUsedPorts();

            for (int offset = 0; offset < MAX_PORT_OFFSET; offset++)
            {
                int potentialPort = START_PORT - offset;
                if (!usedPorts.Contains(potentialPort))
                {
                    return potentialPort;
                }
            }

            throw new ApplicationException("No available port found within the specified range.");
        }

        private HashSet<int> GetUsedPorts()
        {
            var properties = IPGlobalProperties.GetIPGlobalProperties();
            var connections = properties.GetActiveTcpConnections();

            var usedPorts = new HashSet<int>();
            foreach (var connection in connections)
            {
                usedPorts.Add(connection.LocalEndPoint.Port);
            }

            return usedPorts;
        }
    }
}
