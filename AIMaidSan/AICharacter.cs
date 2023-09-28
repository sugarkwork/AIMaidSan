using AIMaidSan.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace AIMaidSan
{
    public class AICharacter
    {
        private const string BaseUrl = "https://api.openai.com/v1/chat/completions";

        public string JsonDirname
        {
            get
            {
                return "json";
            }
        }
        public string TalkJson
        {
            get
            {
                return Path.Combine(JsonDirname, "talk.json");
            }
        }
        public string AsobiCheckJson
        {
            get
            {
                return Path.Combine(JsonDirname, "asobi_or_work.json");
            }
        }
        public string ProfilePath
        {
            get
            {
                return Path.Combine(BaseDirname, "profile.txt");
            }
        }

        public string Name { get; set; }

        public string BaseDirname
        {
            get
            {
                return $"Characters/{Name}/";
            }
        }

        public string BaseImage
        {
            get
            {
                return Path.Combine(BaseDirname, "base.png");
            }
        }

        public string MemoryDir
        {
            get
            {
                return Path.GetFullPath(Path.Combine(Environment.ExpandEnvironmentVariables("%APPDATA%"), $"AiMaidSan/{Name}/Memory/"));
            }
        }

        public string MemoryPath
        {
            get
            {
                return Path.GetFullPath(Path.Combine(MemoryDir, "memory.json"));
            }
        }

        public string Expression
        {
            get; set;
        }

        private string GetFileContent(string filePath)
        {
            return File.ReadAllText(filePath, Encoding.UTF8);
        }
        private async Task<string?> GetFileContentAsync(string filePath)
        {
            return await File.ReadAllTextAsync(filePath, Encoding.UTF8);
        }

        private JsonMemory Memory;

        public string APIKey { get; set; }

        public AICharacter(string name, string apiKey)
        {
            Name = name;
            Expression = "default";
            APIKey = apiKey;

            if (!Directory.Exists(MemoryDir))
            {
                Directory.CreateDirectory(MemoryDir);
            }
            Memory = new JsonMemory(MemoryPath, true);
        }

        public async Task<string> ShortResponce()
        {
            if (string.IsNullOrWhiteSpace(APIKey)) { return ""; }

            string shortResponce = "shortResponce";
            string talk_key = $"short_log_{DateTime.Now.DayOfWeek}_{DateTime.Now.Hour}";

            // return cache data
            var talk_message = Memory.Get<string>(shortResponce, talk_key);
            if (!string.IsNullOrWhiteSpace(talk_message))
            {
                return talk_message;
            }

            try
            {
                var prompt = "It seems that your master has something to ask you. Please output a short reply in Japanese. \nYou don't need to say your name.";
                string jsonData = Generate(GetFileContent(TalkJson), prompt, true);

                await Console.Out.WriteLineAsync(jsonData);

                try
                {
                    var jsonObj = await PostGPT(jsonData);

                    Expression = GetString(jsonObj, "expression", "default");
                    var talk_log = GetString(jsonObj, "talk");
                    Memory.Set(shortResponce, talk_key, talk_log);
                    return talk_log;
                }
                catch (Exception ex)
                {
                    await Console.Out.WriteLineAsync(ex.Message);
                }
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync($"GPTTest Error 2: {ex.ToString()}");
            }
            return string.Empty;
        }

        public async Task<string> FreeTalk(string prompt)
        {
            string jsonData = Generate(GetFileContent(TalkJson), prompt, true);
            await Console.Out.WriteLineAsync(jsonData);
            try
            {
                var jsonObj = await PostGPT(jsonData);

                Expression = GetString(jsonObj, "expression", "default");
                var talk_log = GetString(jsonObj, "talk");
                return talk_log;
            }
            catch (Exception ex) { await Console.Out.WriteLineAsync(ex.ToString()); }
            return string.Empty;
        }

        public async Task<string> Taking()
        {
            string talk_key = $"talk_log_{DateTime.Now.DayOfWeek}_{DateTime.Now.Hour}";
            if (string.IsNullOrWhiteSpace(APIKey)) { return ""; }

            // return cache data
            var talk_message = Memory.Get<string>("talk", talk_key);
            if (!string.IsNullOrWhiteSpace(talk_message))
            {
                return talk_message;
            }

            try
            {
                var last_greeting = Memory.Get<DateTime>("talk", "last_greeting");
                Console.WriteLine($"last greeting : {last_greeting}");
                var greeting = "Please output a greeting in Japanese to your husband.";
                if (last_greeting.Day == DateTime.Now.Day)
                {
                    greeting = "";
                    Console.WriteLine("greeting skip");
                }
                var prompt = $"{greeting} Please be considerate of your husband's health and work in Japanese.You don't need to give your name.".Trim();

                string jsonData = Generate(GetFileContent(TalkJson), prompt, true);

                await Console.Out.WriteLineAsync(jsonData);

                try
                {
                    var jsonObj = await PostGPT(jsonData);

                    Expression = GetString(jsonObj, "expression", "default");
                    var talk_log = GetString(jsonObj, "talk");
                    Memory.Set("talk", talk_key, talk_log);
                    return talk_log;
                }
                catch (Exception ex)
                {
                    await Console.Out.WriteLineAsync(ex.Message);
                }
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync($"GPTTest Error 2: {ex.ToString()}");
            }
            return string.Empty;
        }

        public async Task<Dictionary<string, int>?> WorkCheck(string porcess_info)
        {
            string asobicheck = "asobicheck";
            var data = Memory.Get<Dictionary<string, int>?>(asobicheck, porcess_info);
            if (data != null)
            {
                return data;
            }

            await Console.Out.WriteLineAsync(porcess_info);
            var fileData = await GetFileContentAsync(AsobiCheckJson);
            if (fileData == null) { return null; }

            var prompt = $"The following information is about processes running on your PC. Predicts whether the user is working or not, and predicts whether the user is slacking off at work (unit: %)\n\n{porcess_info}".Trim();

            var jsonData = Generate(fileData, prompt, false, true);
            if (jsonData == null) { return null; }

            var jsonObj = await PostGPT(jsonData);
            var working = GetString(jsonObj, "working", "0");
            var slacking = GetString(jsonObj, "slacking", "0");

            var result = new Dictionary<string, int>();
            result["working"] = int.Parse(working);
            result["slacking"] = int.Parse(slacking);

            Memory.Set(asobicheck, porcess_info, result);

            await Console.Out.WriteLineAsync($"{working} / {slacking}");

            return result;
        }

        private string GetString(JObject? json, string key, string default_value = "")
        {
            Console.WriteLine($"GetString {json}");
            if (json != null && json.ContainsKey(key) && json[key] != null)
            {
                var data = json[key];
                if (data != null)
                {
                    return data.ToString();
                }
            }
            return default_value;
        }

        private async Task<JObject?> PostGPT(string jsonData)
        {
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {APIKey}");
            var response = await client.PostAsync(BaseUrl, new StringContent(jsonData, Encoding.UTF8, "application/json"));

            await Console.Out.WriteLineAsync($"PostGPT : {jsonData}");

            await Console.Out.WriteLineAsync($"Success : {response.IsSuccessStatusCode} / Status Code: {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }
            var responseBody = await response.Content.ReadAsStringAsync();
            await Console.Out.WriteLineAsync("Responce : ");
            await Console.Out.WriteLineAsync(responseBody);

            var jsonObject = JObject.Parse(responseBody);
#pragma warning disable CS8602 // null 参照の可能性があるものの逆参照です。
            try
            {
                var message = jsonObject["choices"][0]["message"];
                await Console.Out.WriteLineAsync($"message = {message}");

                if (message["function_call"] != null)
                {
                    await Console.Out.WriteLineAsync("function_call");
                    string functionArgumentsString = message["function_call"]["arguments"].ToString();
                    var functionArgumentsObject = JObject.Parse(functionArgumentsString);
                    return functionArgumentsObject;
                }
                else if (message["content"] != null)
                {
                    return JObject.Parse(message["content"].ToString());
                }
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync($"PostGPT Error: {ex}");
            }
#pragma warning restore CS8602 // null 参照の可能性があるものの逆参照です。

            return null;
        }

        private string Generate(string jsonData, string prompt, bool insertProfile = false, bool gpt4 = false)
        {
            // 読み取った JSON データを JObject に変換する
            var jsonObject = JsonConvert.DeserializeObject<JObject>(jsonData);

            // JObject の特定のプロパティを変更する
            if (jsonObject != null)
            {
                string profdata = string.Empty;
                if (insertProfile)
                {
                    profdata = $"{GetFileContent(ProfilePath).Replace("\r", "")}\n\nCurrent date and time = {DateTime.Now.ToString("g")}\n\n";
                }
                if (gpt4)
                {
                    jsonObject["model"] = "gpt-4-0613";
                }
                else
                {
                    jsonObject["model"] = "gpt-3.5-turbo-0613";
                }
                jsonObject["messages"] = new JArray {
                    new JObject { ["role"] = "user", ["content"] = $"{profdata}{prompt}".Trim() },
                };

                Memory.Set("talk", "last_greeting", DateTime.Now);

                // JObject を JSON データに戻す
                string updatedJsonData = jsonObject.ToString(Formatting.Indented);
                return updatedJsonData;
            }

            return string.Empty;
        }
    }
}
