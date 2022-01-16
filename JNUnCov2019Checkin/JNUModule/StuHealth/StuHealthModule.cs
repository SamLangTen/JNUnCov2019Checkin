using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.IO;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using JNUnCov2019Checkin.ValidationHelper;

namespace JNUnCov2019Checkin.JNUModule.StuHealth
{
    /// <summary>
    /// Represent an interface to access student health website
    /// </summary>
    class StuHealthModule : JNUModuleBase
    {
        public StuHealthModule()
        {
            Cookies = new CookieContainer();
        }

        /// <summary>
        /// Check-in statement of current logined user.
        /// </summary>
        public CheckinState State { get; private set; } = CheckinState.Undetermined;

        /// <summary>
        /// Encrypt user's password with specified key by method that StuHealth website specified.
        /// </summary>
        /// <param name="plainText">Plain password</param>
        /// <param name="key">Encryption key</param>
        /// <returns>Encrpyted password</returns>
        public static string EncryptPassword(string plainText, string key)
        {
            var baseText = EncryptUsername(plainText, key);
            baseText = new Regex(Regex.Escape("=")).Replace(baseText, "*", 1);
            return baseText;
        }

        private static string EncryptUsername(string plainText, string key)
        {
            byte[] encrypted;

            using (var myAes = new AesManaged
            {
                Key = Encoding.UTF8.GetBytes(key),
                Mode = CipherMode.CBC,
                Padding = PaddingMode.PKCS7,
                IV = Encoding.UTF8.GetBytes(key)
            })
            {

                var encryptor = myAes.CreateEncryptor();
                using var msEncrypt = new MemoryStream();
                using var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
                using (var swEncrypt = new StreamWriter(csEncrypt))
                {
                    swEncrypt.Write(plainText);
                }
                encrypted = msEncrypt.ToArray();
            }
            var baseText = Convert.ToBase64String(encrypted);
            baseText = baseText.Replace("+", "-").Replace("/", "_");
            return baseText;
        }

        /// <summary>
        /// Get encryption key from frontend of StuHealth website
        /// </summary>
        /// <returns>Encryption key</returns>
        public static async Task<string> GetEncryptionKey(string useragent = "")
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.GZip,
                UseCookies = true
            };
            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9");
            client.DefaultRequestHeaders.Add("User-Agent", useragent);
            client.DefaultRequestHeaders.Add("Connection", "keep-alive");
            client.DefaultRequestHeaders.Add("Host", "stuhealth.jnu.edu.cn");
            client.DefaultRequestHeaders.Add("Origin", "https://stuhealth.jnu.edu.cn");
            client.DefaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.9,ja-JP;q=0.8,ja;q=0.7,en;q=0.6");

            var mainPageContent = await (await client.GetAsync("https://stuhealth.jnu.edu.cn/")).Content.ReadAsStringAsync();
            var bundleName = new Regex("(?<=src\\=\\\")main\\.[^\\.]*\\.bundle\\.js(?=\\\")").Match(mainPageContent).Value;

            var jsContent = await (await client.GetAsync($"https://stuhealth.jnu.edu.cn/{bundleName}")).Content.ReadAsStringAsync();
            var encryptionKey = new Regex("(?<=CRYPTOJSKEY\\=\\\")[^\\\"]*(?=\\\")").Match(jsContent).Value;

            return encryptionKey;
        }

        /// <summary>
        /// Login to StuHealth Website
        /// </summary>
        /// <param name="username">JNU Id</param>
        /// <param name="password">Plain password</param>
        /// <param name="encryptKey">Encryption key used to encrypt password in StuHealth website frontend</param>
        /// <returns>Username encrypted by server</returns>
        public async Task<string> Login(string username, string password, string encryptKey, string validationToken)
        {
            return await Login(username, EncryptPassword(password, encryptKey), validationToken);
        }

        /// <summary>
        /// Login to StuHealth Website
        /// </summary>
        /// <param name="username">JNU Id</param>
        /// <param name="encryptedPassword">Encrypted password</param>
        /// <returns>Username encrypted by server</returns>
        public async Task<string> Login(string username, string encryptedPassword, string validationToken)
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
            client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            client.DefaultRequestHeaders.Add("Connection", "keep-alive");
            client.DefaultRequestHeaders.Add("Host", "stuhealth.jnu.edu.cn");
            client.DefaultRequestHeaders.Add("Origin", "https://stuhealth.jnu.edu.cn");
            client.DefaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.9,ja-JP;q=0.8,ja;q=0.7,en;q=0.6");

            await client.GetAsync("https://stuhealth.jnu.edu.cn/");

            client.DefaultRequestHeaders.Remove("Accept");
            client.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
            client.DefaultRequestHeaders.Add("Referer", "https://stuhealth.jnu.edu.cn/");

            using (var postData = new StringContent("{\"username\":\"" + username + "\",\"password\":\"" + encryptedPassword + "\",\"validate\":\"" + validationToken + "\"}"))
            {
                postData.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                var loginJson = JObject.Parse(await (await client.PostAsync("https://stuhealth.jnu.edu.cn/api/user/login", postData)).Content.ReadAsStringAsync());

                var msg = loginJson["meta"]["msg"].Value<string>();

                if (loginJson["meta"]["success"].Value<bool>() == false)
                {
                    throw new StuHealthLoginException(msg);
                }

                if (msg == "登录成功，填写相隔小于6小时")
                {
                    throw new StuHealthCheckinLessThanSixHourException();
                }

                if (msg == "登录成功，今天已填写")
                    State = CheckinState.Finished;
                else if (msg == "登录成功，今天未填写")
                    State = CheckinState.Unfinished;

                return loginJson["data"]["jnuid"].Value<string>();
            }


        }

        /// <summary>
        /// Get last checkin form
        /// </summary>
        /// <param name="encryptedUsername">Username encrypted by server</param>
        /// <returns>data submitted in last time</returns>
        public async Task<string> GetLastCheckin(string encryptedUsername)
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.GZip,
                CookieContainer = Cookies,
                UseCookies = true
            };
            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            client.DefaultRequestHeaders.Add("Connection", "keep-alive");
            client.DefaultRequestHeaders.Add("Host", "stuhealth.jnu.edu.cn");
            client.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
            client.DefaultRequestHeaders.Add("Referer", "https://stuhealth.jnu.edu.cn/");

            var checkinId = 0;
            using (var postData = new StringContent("{\"jnuid\":\"" + encryptedUsername + "\"}"))
            {
                postData.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                var checkinfoJson = JObject.Parse(await (await client.PostAsync("https://stuhealth.jnu.edu.cn/api/user/stucheckin", postData)).Content.ReadAsStringAsync());

                if (checkinfoJson["data"]["checkinInfo"].HasValues == false)
                    throw new StuHealthLastCheckinNotFoundException();

                var checkNode = checkinfoJson["data"]["checkinInfo"].First;
                while (checkNode["id"].Value<string>() == null && checkNode.Next != null)
                    checkNode = checkNode.Next;

                if (checkNode["id"].Value<string>() == null)
                    throw new StuHealthLastCheckinNotFoundException();

                checkinId = checkNode["id"].Value<int>();
            }

            var lastDataTable = "";
            using (var postData = new StringContent("{\"jnuid\":\"" + encryptedUsername + "\",\"id\":\"" + checkinId.ToString() + "\"}"))
            {
                postData.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                var checkinJson = JObject.Parse(await (await client.PostAsync("https://stuhealth.jnu.edu.cn/api/user/review", postData)).Content.ReadAsStringAsync());

                //Check if there is a checkin today
                var lastCheckinDate = DateTime.Parse(checkinJson["data"]["mainTable"]["declareTime"].Value<string>());
                var nowTimeCST = TimeZoneInfo.ConvertTime(DateTime.Now, TimeZoneInfo.Utc);
                nowTimeCST = nowTimeCST.AddHours(8);
                if (lastCheckinDate.Year == nowTimeCST.Year && lastCheckinDate.Month == nowTimeCST.Month && lastCheckinDate.Date == nowTimeCST.Date)
                    State = CheckinState.Finished;
                else
                    State = CheckinState.Unfinished;


                lastDataTable = checkinJson["data"].ToString();
            }

            return lastDataTable;
        }

        /// <summary>
        /// Do a checkin
        /// </summary>
        /// <param name="encryptedUsername">Username encrypted by server</param>
        /// <param name="dataTable">A specified form that contains student's information. It can be fetch by <see cref="StuHealth.StuHealthModule.GetLastCheckin(string)"/> function</param>
        /// <seealso cref="StuHealth.StuHealthModule.GetLastCheckin(string)"/>
        public async Task Checkin(string encryptedUsername, string dataTable)
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.GZip,
                CookieContainer = Cookies,
                UseCookies = true
            };
            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("User-Agent", UserAgent);
            client.DefaultRequestHeaders.Add("Connection", "keep-alive");
            client.DefaultRequestHeaders.Add("Host", "stuhealth.jnu.edu.cn");
            client.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
            client.DefaultRequestHeaders.Add("Referer", "https://stuhealth.jnu.edu.cn/");

            using (var postData = new StringContent("{\"jnuId\":\"" + encryptedUsername + "\",\"idType\":\"1\"}"))
            {
                postData.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                await client.PostAsync("https://stuhealth.jnu.edu.cn/api/user/stuinfo", postData);
            }


            var dataTableJson = JObject.Parse(dataTable);

            var postDataJson = JObject.Parse("{\"mainTable\":" + dataTableJson["mainTable"].ToString() + ",\"secondTable\":" + dataTableJson["secondTable"].ToString() + ",\"jnuid\":\"" + encryptedUsername + "\"}");
            postDataJson["mainTable"]["id"] = null;
            postDataJson["mainTable"]["declareTime"] = DateTime.Now.ToString("yyyy-MM-dd");
            postDataJson["secondTable"]["mainId"] = null;
            postDataJson["secondTable"]["id"] = null;

            using (var postData = new StringContent(postDataJson.ToString()))
            {
                postData.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                var writeJson = JObject.Parse(await (await client.PostAsync("https://stuhealth.jnu.edu.cn/api/write/main", postData)).Content.ReadAsStringAsync());

                if (writeJson["meta"]["success"].Value<bool>() == true)
                    return;
                else
                    throw new StuHealthCheckinException(writeJson["meta"]["msg"].Value<string>());
            }

        }
    }

    /// <summary>
    /// Checkin State
    /// </summary>
    enum CheckinState
    {
        /// <summary>
        /// User has finished today's checkin.
        /// </summary>
        Finished,
        /// <summary>
        /// User has not finished today's checkin.
        /// </summary>
        Unfinished,
        /// <summary>
        /// User has not logined so that module doesn't know whether user has checkin today.
        /// </summary>
        Undetermined
    }

}
