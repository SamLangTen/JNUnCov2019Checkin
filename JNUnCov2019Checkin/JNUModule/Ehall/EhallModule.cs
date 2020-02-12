using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;

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

        private async Task<HttpResponseMessage> RecurseRedirectVisit(Uri uri)
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

            var response = await client.GetAsync(uri);
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
