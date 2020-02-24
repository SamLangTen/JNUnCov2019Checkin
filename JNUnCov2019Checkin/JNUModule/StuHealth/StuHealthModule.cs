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

namespace JNUnCov2019Checkin.JNUModule.StuHealth
{
    class StuHealthModule
    {

        public CookieContainer Cookies { get; set; }

        public CheckinState State { get; private set; } = CheckinState.Undetermined;

        public static string EncryptPassword(string plainText, string key)
        {
            var baseText = EncryptUsername(plainText, key);
            baseText = new Regex(Regex.Escape("=")).Replace(baseText, "*", 1);
            return baseText;
        }

        public static string EncryptUsername(string plainText, string key)
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

        public async Task<string> Login(string username, string password, string encryptKey)
        {
            return await Login(username, EncryptPassword(password, encryptKey));
        }

        public async Task<string> Login(string username, string encryptedPassword)
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
            client.DefaultRequestHeaders.Add("Host", "stuhealth.jnu.edu.cn");
            client.DefaultRequestHeaders.Add("Origin", "https://stuhealth.jnu.edu.cn");
            client.DefaultRequestHeaders.Add("Accept-Language", "zh-CN,zh;q=0.9,ja-JP;q=0.8,ja;q=0.7,en;q=0.6");

            await client.GetAsync("https://stuhealth.jnu.edu.cn/");

            client.DefaultRequestHeaders.Remove("Accept");
            client.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
            client.DefaultRequestHeaders.Add("Referer", "https://stuhealth.jnu.edu.cn/");

            using (var postData = new StringContent("{\"username\":\"" + username + "\",\"password\":\"" + encryptedPassword + "\"}"))
            {
                postData.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                var loginJson = JObject.Parse(await (await client.PostAsync("https://stuhealth.jnu.edu.cn/api/user/login", postData)).Content.ReadAsStringAsync());

                var msg = loginJson["meta"]["msg"].Value<string>();

                if (loginJson["meta"]["success"].Value<bool>() == false)
                {
                    throw new StuHealthLoginException(msg);
                }

                if (msg == "登录成功，今天已填写")
                    State = CheckinState.Finished;
                else if (msg == "登录成功，今天未填写")
                    State = CheckinState.Unfinished;

                return loginJson["data"]["jnuid"].Value<string>();
            }


        }

        public async Task Checkin(string encryptedUsername, string mainTable)
        {
            var handler = new HttpClientHandler
            {
                AllowAutoRedirect = false,
                AutomaticDecompression = DecompressionMethods.GZip,
                CookieContainer = Cookies,
                UseCookies = true
            };
            var client = new HttpClient(handler);
            client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/79.0.3945.130 Safari/537.36");
            client.DefaultRequestHeaders.Add("Connection", "keep-alive");
            client.DefaultRequestHeaders.Add("Host", "stuhealth.jnu.edu.cn");
            client.DefaultRequestHeaders.Add("Accept", "application/json, text/plain, */*");
            client.DefaultRequestHeaders.Add("Referer", "https://stuhealth.jnu.edu.cn/");

            using (var postData = new StringContent("{\"jnuId\":\"" + encryptedUsername + "\",\"idType\":\"1\"}"))
            {
                postData.Headers.ContentType = new MediaTypeHeaderValue("application/json");

                await client.PostAsync("https://stuhealth.jnu.edu.cn/api/user/stuinfo", postData);
            }

            using (var postData = new StringContent(mainTable))
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

    enum CheckinState
    {
        Finished,
        Unfinished,
        Undetermined
    }

}
