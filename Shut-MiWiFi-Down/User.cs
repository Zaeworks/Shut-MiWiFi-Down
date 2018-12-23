using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ShutMiWiFiDown
{
    public class User
    {
        const string urlStopPppoePost = "/api/xqnetwork/pppoe_stop";

        const string UrlHome = "/cgi-bin/luci/web/home";
        const string UrlLoginPost = "/cgi-bin/luci/api/xqsystem/login";
        protected string UrlStopPppoePost => "/cgi-bin/luci" + Token + urlStopPppoePost;

        public string Password { get; set; }

        public string UrlHost { get; set; }

        private CookieContainer _cookies;
        public CookieContainer Cookies => _cookies;

        protected HttpClient Client = null;

        protected string Token = ""; // "/;stok="

        protected User() { }

        public static User Create(string password, string host, CookieContainer cookies = null)
        {
            cookies = cookies ?? new CookieContainer();
            var handler = new HttpClientHandler() { CookieContainer = cookies };
            var user = new User() { Password = password, UrlHost = host };
            user.Client = new HttpClient(handler) { BaseAddress = new Uri(user.UrlHost) };
            user._cookies = cookies;
            return user;
        }

        public async Task LoginAsync()
        {
            var doc = await GetDocumentAsync(UrlHome);
            var script = doc.DocumentNode.SelectSingleNode(@"//script[19]").InnerText;

            var key = Regex.Match(script, @"key: '(.*)'").Groups[1].Value;
            var mac = Regex.Match(script, @"deviceId = '(.*)'").Groups[1].Value;

            var type = 0;
            var time = (DateTime.UtcNow.Ticks - 621355968000000000) / 10000000;
            var random = (int)(new Random().NextDouble() * 9000 + 1000);

            var nonce = string.Join("_", type, mac, time, random);
            var pass = SHA1(nonce + SHA1(Password + key));

            var postData = BuildPostData(
                "username", "admin",
                "password", pass,
                "logtype", "2",
                "nonce", nonce);

            var response = await Client.PostAsync(UrlLoginPost, postData);

            if (response.StatusCode != HttpStatusCode.OK)
                throw new HttpRequestException("POST response code: " + (int)response.StatusCode);

            var text = await response.Content.ReadAsStringAsync();
            var token = Regex.Match(text, "\"token\":\"(.*)\"").Groups[1].Value;
            if (token.Length > 0)
                Token = "/;stok=" + token;
            else
                throw new Exception("Login failed");
        }

        #region [Utils]
        public static string SHA1(string text)
        {
            var sha1 = new System.Security.Cryptography.SHA1Managed();
            var hash = sha1.ComputeHash(Encoding.ASCII.GetBytes(text));
            return string.Join("", hash.Select(c => c.ToString("x2")));
        }

        protected async Task<HttpResponseMessage> GetAsync(string requestUri)
        {
            var response = await Client.GetAsync(requestUri);
            if (response.IsSuccessStatusCode)
                return response;
            throw new HttpRequestException("GET response code: " + (int)response.StatusCode);
        }

        protected async Task<HtmlDocument> GetDocumentAsync(string requestUri)
        {
            var response = await GetAsync(requestUri);
            var str = await response.Content.ReadAsStringAsync();
            var document = new HtmlDocument();
            document.LoadHtml(str);
            return document;
        }

        protected static FormUrlEncodedContent BuildPostData(string key1, string value1, params string[] keyValues)
        {
            var collection = new List<KeyValuePair<string, string>>();
            collection.Add(new KeyValuePair<string, string>(key1, value1));
            for (int i = 0; i < keyValues.Length; i += 2)
                collection.Add(new KeyValuePair<string, string>(keyValues[i], keyValues[i + 1]));
            return new FormUrlEncodedContent(collection);
        }
        #endregion

    }
}
