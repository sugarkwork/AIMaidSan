using System.Collections.Concurrent;
using System.IO;

namespace AIMaidSan
{
    internal class JsonMemory
    {
        public string? Path { get; set; }
        public bool AutoSave { get; set; }

        private ConcurrentDictionary<string, ConcurrentDictionary<string, string>>? Memory = null;
        private System.Text.Json.JsonSerializerOptions options;

        public JsonMemory(string? filepath = null, bool autosave = false)
        {
            Path = filepath;
            AutoSave = autosave;

            options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };

            if (Path != null)
            {
                Load();
            }
        }

        public void Load(string? path = null)
        {
            string? load_path = path ?? Path;
            if (load_path != null && File.Exists(load_path))
            {
                Memory = System.Text.Json.JsonSerializer.Deserialize<ConcurrentDictionary<string, ConcurrentDictionary<string, string>>>(File.ReadAllText(load_path));
            }
        }

        private void CheckMemory(string section)
        {
            if (Memory == null)
            {
                Memory = new ConcurrentDictionary<string, ConcurrentDictionary<string, string>>();
            }
            if (!Memory.ContainsKey(section))
            {
                Memory[section] = new ConcurrentDictionary<string, string>();
            }
        }

        public T? Get<T>(string section, string key)
        {
            CheckMemory(section);
#pragma warning disable CS8602 // null 参照の可能性があるものの逆参照です。
            if (Memory[section].ContainsKey(key))
            {
                return System.Text.Json.JsonSerializer.Deserialize<T>(Memory[section][key]);
            }
#pragma warning restore CS8602 // null 参照の可能性があるものの逆参照です。
            return default;
        }

        public void Set<T>(string section, string key, T val)
        {
            CheckMemory(section);
#pragma warning disable CS8602 // null 参照の可能性があるものの逆参照です。
            Memory[section][key] = System.Text.Json.JsonSerializer.Serialize<T>(val);
#pragma warning restore CS8602 // null 参照の可能性があるものの逆参照です。

            if(AutoSave)
            {
                Save();
            }
        }
        public void Save(string? path = null)
        {
            string? output_path = path ?? Path;
            if (output_path != null)
            {
                File.WriteAllText(output_path, System.Text.Json.JsonSerializer.Serialize(Memory, options));
            }
        }
    }
}
