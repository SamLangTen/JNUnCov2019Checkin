using System;
using System.Net.Http;
using System.Threading.Tasks;
using JNUnCov2019Checkin.Config;
using Newtonsoft.Json.Linq;

namespace JNUnCov2019Checkin.ValidationHelper
{
    class AKarinValidationHelper : ValidationHelper
    {
        private GlobalConfig config { get; set; }

        public AKarinValidationHelper(GlobalConfig config)
        {
            this.config = config;
        }

        public override async Task<string> GetValidationAsync()
        {
            string akarinToken = Environment.GetEnvironmentVariable("JNUCHECKIN_AKARINVALID_TOKEN");
            string akarinEndpoint = Environment.GetEnvironmentVariable("JNUCHECKIN_AKARINVALID_ENDPOINT");
            if (akarinToken == null && config.ExtraConfigs.ContainsKey("AkarinValidationToken"))
                akarinToken = config.ExtraConfigs["AkarinValidationToken"].ToString();
            if (akarinEndpoint == null && config.ExtraConfigs.ContainsKey("AkarinValidationEndpoint"))
                akarinEndpoint = config.ExtraConfigs["AkarinValidationEndpoint"].ToString();

            var client = new HttpClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {akarinToken}");
            String rtnString = await (await client.PostAsync(akarinEndpoint + "/refreshToken", null)).Content.ReadAsStringAsync();
            var jobj = JObject.Parse(rtnString);
            var token = jobj["validation_token"].ToString();
            return token;
        }
    }
}
