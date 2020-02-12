using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace JNUnCov2019Checkin.JNUModule.Ehall
{
    class EhallModule
    {
        public CookieContainer Cookies { get; set; }

        public async Task Login(string checkLoginName)
        {
            var loginResponse = await RecurseRedirectVisit(new Uri("https://ehall.jnu.edu.cn"));
            string content = await loginResponse.Content.ReadAsStringAsync();
            if (content.Contains(checkLoginName))
                return;
            else
                throw new EhallLoginException();
        }

        public async Task StudentnCov2019Checkin(string boundFields, string formData)
        {
            var handler = new HttpClientHandler();
            handler.AllowAutoRedirect = false;
            handler.AutomaticDecompression = DecompressionMethods.GZip;
            handler.CookieContainer = Cookies;
            handler.UseCookies = true;
            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/79.0.3945.130 Safari/537.36");
            client.DefaultRequestHeaders.Add("Connection", "keep-alive");

            string csrfToken;
            string stepId;
            string timestamp;
            string instanceId;
            string entities0;

            var sbContent = await (await client.GetAsync("https://ehall.jnu.edu.cn/infoplus/form/XNYQSB/start")).Content.ReadAsStringAsync();
            csrfToken = new Regex("(?<=itemscope\\=\\\"csrfToken\\\" content\\=\\\")[^\\s]*(?=\\\")").Match(sbContent).Value;

            using (var postData = new FormUrlEncodedContent(new Dictionary<string, string>() {
                {"idc","XNYQSB" },
                {"csrfToken",csrfToken },
                {"formData","{\"_VAR_URL\":\"https://ehall.jnu.edu.cn/infoplus/form/XNYQSB/start\",\"_VAR_URL_Attr\":\"{}\"}" },
                {"release","" }
            }.ToList()))
            {
                var interfaceStartContentJson = JObject.Parse(await (await client.PostAsync("https://ehall.jnu.edu.cn/infoplus/interface/start", postData)).Content.ReadAsStringAsync());
                entities0 = interfaceStartContentJson["entities"][0].Value<string>();
                stepId = new Regex("(?<=https\\:\\/\\/ehall.jnu.edu.cn\\/infoplus\\/form\\/)\\d*(?=\\/render)").Match(entities0).Value;
            }

            await client.GetAsync(entities0);
            await client.GetAsync("https://ehall.jnu.edu.cn/infoplus/alive");

            client.DefaultRequestHeaders.Referrer = new Uri(entities0);

            using (var postData = new FormUrlEncodedContent(new Dictionary<string, string>() {
                {"stepId",stepId },
                {"csrfToken",csrfToken },
                {"instanceId","" },
                {"admin","false" },
                {"rand",(new Random().NextDouble() * 999).ToString() },
                {"width","1920" },
                {"lang","zh" }
            }.ToList()))
            {

                var interfaceRenderContentJson = JObject.Parse(await (await client.PostAsync("https://ehall.jnu.edu.cn/infoplus/interface/render", postData)).Content.ReadAsStringAsync());
                instanceId = interfaceRenderContentJson["entities"][0]["step"]["instanceId"].Value<string>();
                timestamp = interfaceRenderContentJson["entities"][0]["step"]["timestamp"].Value<string>();
            }

            using (var postData = new FormUrlEncodedContent(new Dictionary<string, string>() {
                {"stepId",stepId },
                {"csrfToken",csrfToken },
                {"lang","zh" }
            }.ToList()))
            {
                await client.PostAsync($"https://ehall.jnu.edu.cn/infoplus/interface/instance/{instanceId}/progress", postData);
            }

            using (var postData = new FormUrlEncodedContent(new Dictionary<string, string> {
                {"stepId",stepId },
                {"actionId","1" },
                {"formData",formData },
                {"timestamp",timestamp },
                {"rand",(new Random().NextDouble()*999).ToString() },
                {"boundFields",boundFields },
                {"csrfToken",csrfToken },
                {"lang","zh" }
            }.ToList()))
            {
                await client.PostAsync("https://ehall.jnu.edu.cn/infoplus/interface/listNextStepsUsers", postData);
            }

            using (var postData = new FormUrlEncodedContent(new Dictionary<string, string> {
                { "remark","" },
                { "nextUsers","{}" },
                {"stepId",stepId },
                {"actionId","1" },
                {"formData",formData },
                {"timestamp",timestamp },
                {"rand",(new Random().NextDouble()*999).ToString() },
                {"boundFields",boundFields },
                {"csrfToken",csrfToken },
                {"lang","zh" }
            }.ToList()))
            {
                var doActionJson = JObject.Parse(await (await client.PostAsync("https://ehall.jnu.edu.cn/infoplus/interface/doAction", postData)).Content.ReadAsStringAsync());
                if (doActionJson["ecode"].Value<string>() == "SUCCEED")
                    return;
                else
                    throw new EhallStudentNCov2019CheckinException();
            }

        }

        public async Task<DateTime?> GetLastEventTime(string eventName)
        {
            var handler = new HttpClientHandler();
            handler.AllowAutoRedirect = false;
            handler.AutomaticDecompression = DecompressionMethods.GZip;
            handler.CookieContainer = Cookies;
            handler.UseCookies = true;
            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/79.0.3945.130 Safari/537.36");
            client.DefaultRequestHeaders.Add("Connection", "keep-alive");

            using (var postData = new FormUrlEncodedContent(new Dictionary<string, string>() {
                { "limit","10"},
                {"start","0" }
            }.ToList()))
            {
                await client.PostAsync("https://ehall.jnu.edu.cn/taskcenter/api/me/processes/cc?limit=10&start=0", postData);
                var json = JObject.Parse(await (await client.PostAsync("https://ehall.jnu.edu.cn/taskcenter/api/me/processes/done?limit=10&start=0", postData)).Content.ReadAsStringAsync());
                var firstEvent = json["entities"].Children().FirstOrDefault(t => t["app"]["name"].Value<string>() == eventName);
                if(firstEvent!=null)
                {
                    System.DateTime startTime = new System.DateTime(1970, 1, 1);
                    DateTime dt = startTime.AddSeconds(long.Parse(firstEvent["create"].Value<string>()));
                    return dt;
                }
                else
                {
                    return null;
                }
            }

        }

        private async Task<HttpResponseMessage> RecurseRedirectVisit(Uri uri)
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.GZip,
                CookieContainer = Cookies,
                UseCookies = true
            };
            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/79.0.3945.130 Safari/537.36");
            client.DefaultRequestHeaders.Add("Connection", "keep-alive");

            var response = await client.GetAsync(uri);
            handler.Dispose();
            client.Dispose();
            if (response.StatusCode == HttpStatusCode.Redirect && response.Headers.Location != null)
            {
                return await RecurseRedirectVisit(response.Headers.Location);
            }
            else
            {
                return response;
            }
        }

    }
}
