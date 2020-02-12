using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Net;
using System.Threading.Tasks;
using System.IO;
using System.Text.RegularExpressions;

namespace JNUnCov2019Checkin.JNUModule.ICAS
{
    class ICASModule
    {
        public CookieContainer Cookies { get; set; }

        public async Task Login(string username, string password)
        {
            HttpWebRequest loginPage = WebRequest.CreateHttp("https://icas.jnu.edu.cn/cas/login");
            loginPage.CookieContainer = Cookies;
            loginPage.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            loginPage.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/79.0.3945.130 Safari/537.36";
            loginPage.KeepAlive = true;
            loginPage.Host = "icas.jnu.edu.cn";
            var content = await new StreamReader((await loginPage.GetResponseAsync()).GetResponseStream()).ReadToEndAsync();
            var ltRegex = new Regex("(?<=\\<input type\\=\\\"hidden\\\" id\\=\\\"lt\\\" name\\=\\\"lt\\\" value\\=\\\")[^\\s]*(?=\\\" \\/\\>)");
            var lt = ltRegex.Match(content).Value;
            var exeRegex = new Regex("(?<=\\<input type\\=\\\"hidden\\\" name\\=\\\"execution\\\" value\\=\\\")[^\\s]*(?=\\\" \\/\\>)");
            var exe = exeRegex.Match(content).Value;

            HttpWebRequest loginPagePost = WebRequest.CreateHttp("https://icas.jnu.edu.cn/cas/login");
            loginPagePost.CookieContainer = Cookies;
            loginPagePost.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8,application/signed-exchange;v=b3;q=0.9";
            loginPagePost.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/79.0.3945.130 Safari/537.36";
            loginPagePost.KeepAlive = true;
            loginPagePost.Host = "icas.jnu.edu.cn";
            loginPagePost.Referer = "https://icas.jnu.edu.cn/cas/login";
            loginPagePost.ContentType = "application/x-www-form-urlencoded";
            loginPagePost.AllowAutoRedirect = true;

            username = username.Trim().Replace("*", "");
            var postText = $"rsa={DESUtil.StrEnc(username + password + lt, "1", "2", "3")}&ul={username.Length}&pl={password.Length}&lt={lt}&execution={exe}&_eventId=submit";
            var postData = Encoding.UTF8.GetBytes(postText);

            loginPagePost.Method = "POST";
            loginPagePost.ContentLength = postData.Length;
            (await loginPagePost.GetRequestStreamAsync()).Write(postData, 0, postData.Length);

            try
            {
                await loginPagePost.GetResponseAsync();
            }
            catch (WebException ex)
            {
                if (ex.Status != WebExceptionStatus.ProtocolError)
                    throw new ICASLoginException();
                return;
            }
            throw new ICASLoginException();

        }


    }
}
