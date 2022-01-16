using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace JNUnCov2019Checkin.Config
{
    class GlobalConfig
    {
        public int CheckinInterval { get; set; }
        [DefaultValue("Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/88.0.4324.146 Safari/537.36")]
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Populate)]
        public string GlobalUserAgent { get; set; }

        public string ValidationHelper { get; set; }
        public int RetryTimes { get; set; }
        public List<SubConfig> Configs { get; set; }

        [JsonExtensionData]
        public IDictionary<string, JToken> ExtraConfigs { get; set; } = new Dictionary<string, JToken>();

        public static GlobalConfig LoadConfigs(string configPath = "./config.json")
        {
            string jsonText;
            jsonText = File.ReadAllText(configPath);
            return JsonConvert.DeserializeObject<GlobalConfig>(jsonText);
        }

        public static void SaveConfigs(GlobalConfig configs, string configPath = "./config.json")
        {
            var jsonText = JsonConvert.SerializeObject(configs);
            File.WriteAllText(configPath, jsonText);
        }
    }
}
