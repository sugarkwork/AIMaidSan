﻿using AIMaidSan.Properties;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
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

        public string Expression
        {
            get; set;
        }

        public AICharacter(string name)
        {
            Name = name;
            Expression = "default";
        }

        public async Task<string> Taking()
        {
            try
            {
                using HttpClient client = new HttpClient();
                var apiKey = Settings.Default.API;
                if (string.IsNullOrEmpty(apiKey))
                {
                    apiKey = File.ReadAllText("key.txt").Trim();
                }
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                // JSONファイルからデータを読み込む
                string jsonData = await Task.Run(() =>
                {
                    return JsonExtensionDataAttribute(File.ReadAllText(TalkJson));
                });
                await Console.Out.WriteLineAsync(jsonData);

                var response = await client.PostAsync(BaseUrl, new StringContent(jsonData, Encoding.UTF8, "application/json"));

                await Console.Out.WriteLineAsync($"{response.IsSuccessStatusCode} - {response.StatusCode}");
                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();

                    var jsonObject = JObject.Parse(responseBody);
#pragma warning disable CS8602 // null 参照の可能性があるものの逆参照です。
                    string functionArgumentsString = jsonObject["choices"][0]["message"]["function_call"]["arguments"].ToString();
                    var functionArgumentsObject = JObject.Parse(functionArgumentsString);
                    try
                    {
                        if (functionArgumentsObject.ContainsKey("expression"))
                        {
                            Expression = functionArgumentsObject["expression"].ToString();
                        }
                    }
                    catch { }
                    return functionArgumentsObject["talk"].ToString();
#pragma warning restore CS8602 // null 参照の可能性があるものの逆参照です。
                }
                else
                {
                    Console.WriteLine($"GPTTest Error 1: {BaseUrl} => {response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                await Console.Out.WriteLineAsync($"GPTTest Error 2: {ex.ToString()}");
            }
            return string.Empty;
        }

        private string JsonExtensionDataAttribute(string jsonData)
        {
            Console.WriteLine(jsonData);
            // 読み取った JSON データを JObject に変換する
            var jsonObject = JsonConvert.DeserializeObject<JObject>(jsonData);

            // JObject の特定のプロパティを変更する
            if (jsonObject != null)
            {
                jsonObject["model"] = "gpt-3.5-turbo-0613";
                jsonObject["messages"] = new JArray {
                    new JObject { ["role"] = "system", ["content"] = File.ReadAllText(Path.Combine(BaseDirname, "profile.txt")).Replace("\r", "") + $"\n\nToday is {DateTime.Now.ToString("g")}" },
                    new JObject { ["role"] = "user", ["content"] = "Please output a greeting in Japanese to your husband. Please be concerned about your husband's health and work." }, };

                // JObject を JSON データに戻す
                string updatedJsonData = jsonObject.ToString(Newtonsoft.Json.Formatting.Indented);
                return updatedJsonData;
            }

            return string.Empty;
        }
    }
}