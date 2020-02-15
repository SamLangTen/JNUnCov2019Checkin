using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;

namespace JNUnCov2019Checkin
{
    class Config
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string EncryptedPassword { get; set; }
        public string Key { get; set; }
        public bool IsManualMode { get; set; }
        public string PostMainTable { get; set; }
        public bool Enabled { get; set; }
        public static List<Config> LoadConfigs(string configPath = "./config.json")
        {
            string jsonText;
            jsonText = File.ReadAllText(configPath);
            return JsonConvert.DeserializeObject<List<Config>>(jsonText);
        }

        public static void SaveConfigs(List<Config> configs, string configPath = "./config.json")
        {
            var jsonText = JsonConvert.SerializeObject(configs);
            File.WriteAllText(configPath, jsonText);
        }
    }
}
