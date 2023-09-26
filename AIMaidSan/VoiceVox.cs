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
    internal class VoiceVox
    {
        private int port = 50021;
        private bool ready = false;

        private Process? voiceVoxProcess;
        private string BaseUrl = "http://localhost";

        private const int LINE_LIMIT = 100;
        private const int START_PORT = 50021;
        private const int MAX_PORT_OFFSET = 30000;

        public delegate void VoiceVoxReady();
        public event VoiceVoxReady? voiceVoxReady;
        private int speaker = 58;

        public async Task StartVoiceVox(int speaker = 58)
        {
            this.speaker = speaker;
            if (!await CheckVoiceVox())
            {
                port = FindAvailablePort();
                var dirPath = Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\Programs\VOICEVOX\");
                await StartProcessAndGetOutputAsync(dirPath, System.IO.Path.Combine(dirPath, "run.exe"), $"--host localhost --port {port}");
            }
        }

        public async Task<bool> CheckVoiceVox()
        {
            var targetUrl = new Uri($"{BaseUrl}:{port}/version");
            try
            {
                using HttpClient client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
                string content = await client.GetStringAsync(targetUrl);
                await Console.Out.WriteLineAsync($"VoiceVox Version : {content}");

                ready = true;
                await Task.Run(() =>
                {
                    if (voiceVoxReady != null)
                    {
                        voiceVoxReady();
                    }
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
            await Console.Out.WriteLineAsync($"Speak: {textContent}");
            if (!ready) { return null; }

            var queryResponse = await SendRequest($"{BaseUrl}:{port}/audio_query?speaker={this.speaker}&text={WebUtility.UrlEncode(textContent)}");
            if (queryResponse == null || queryResponse.Length == 0)
            {
                return null;
            }
            var queryJson = Encoding.UTF8.GetString(queryResponse);
            var audioResponse = await SendRequest($"{BaseUrl}:{port}/synthesis?speaker={this.speaker}&enable_interrogative_upspeak=true", queryJson, "application/json");

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
                voiceVoxProcess = null;
            }
        }

        public List<string> outputLines = new List<string>();
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
                    if (!ready && (data.Contains("Application startup complete") || data.Contains("emitting double-array: 100%") || data.Contains("done!")))
                    {
                        ready = true;
                        if(voiceVoxReady != null)
                        {
                            voiceVoxReady();
                        }
                    }
                    list.Add(data);
                }
            }
        }

        private readonly object listLock = new object();
        private async Task StartProcessAndGetOutputAsync(string dirPath, string filename, string arguments)
        {
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
                        AddDataToList(outputLines, data.Data);
                    };

                    process.ErrorDataReceived += (sender, data) =>
                    {
                        AddDataToList(outputLines, data.Data);
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
