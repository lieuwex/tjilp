using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using tjilp;

namespace TinyTwitter
{
    public class OAuthInfo
    {
        public OAuthInfo(string consumerKey, string consumerSecret, string accessToken, string accessSecret)
        {
            this.ConsumerKey = consumerKey;
            this.ConsumerSecret = consumerSecret;
            this.AccessToken = accessToken;
            this.AccessSecret = accessSecret;
        }

        public string ConsumerKey { get; set; }
        public string ConsumerSecret { get; set; }
        public string AccessToken { get; set; }
        public string AccessSecret { get; set; }
    }

    public class Tweet
    {
        public long Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UserName { get; set; }
        public string ScreenName { get; set; }
        public string Text { get; set; }
    }

    public static class TinyTwitter
    {
        public static void UpdateStatus(OAuthInfo oauth, string message) { new RequestBuilder(oauth, "POST", "https://api.twitter.com/1.1/statuses/update.json").AddParameter("status", message).Execute(); }

        #region RequestBuilder
        class RequestBuilder
        {
            const string VERSION = "1.0";
            const string SIGNATURE_METHOD = "HMAC-SHA1";
            readonly IDictionary<string, string> customParameters;
            readonly string method;
            readonly OAuthInfo oauth;
            readonly string url;

            public RequestBuilder(OAuthInfo oauth, string method, string url)
            {
                this.oauth = oauth;
                this.method = method;
                this.url = url;
                this.customParameters = new Dictionary<string, string>();
            }

            public RequestBuilder AddParameter(string name, string value)
            {
                this.customParameters.Add(name, value.EncodeRFC3986());
                return this;
            }

            public WebResponse Execute()
            {
                var timespan = GetTimestamp();
                var nonce = CreateNonce();
                var parameters = new Dictionary<string, string>(this.customParameters);
                this.AddOAuthParameters(parameters, timespan, nonce);
                var signature = this.GenerateSignature(parameters);
                var headerValue = this.GenerateAuthorizationHeaderValue(parameters, signature);
                var request = (HttpWebRequest)WebRequest.Create(this.GetRequestUrl());
                request.Method = this.method;
                request.ContentType = "application/x-www-form-urlencoded";
                request.Headers.Add("Authorization", headerValue);
                this.WriteRequestBody(request);
                try
                {
                    return request.GetResponse();
                }
                catch (WebException e)
                {
                    var response = (HttpWebResponse)e.Response;
                    if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.BadRequest)
                    {
                        File.Delete(MainWindow.FullTokensPath);
                        File.WriteAllText(MainWindow.FullTweetDraftPath, MainWindow.This.InputBox.Text.Trim());
                        Process.Start(AppDomain.CurrentDomain.FriendlyName);
                        Environment.Exit(0);
                        return null;
                    }
                    throw;
                }
            }

            void WriteRequestBody(HttpWebRequest request)
            {
                if (this.method == "GET")
                    return;
                var requestBody = Encoding.ASCII.GetBytes(this.GetCustomParametersString());
                using(var stream = request.GetRequestStream())
                    stream.Write(requestBody, 0, requestBody.Length);
            }

            string GetRequestUrl()
            {
                if (this.method != "GET" || this.customParameters.Count == 0)
                    return this.url;
                return string.Format("{0}?{1}", this.url, this.GetCustomParametersString());
            }

            string GetCustomParametersString() { return this.customParameters.Select(x => string.Format("{0}={1}", x.Key, x.Value)).Join("&"); }
            string GenerateAuthorizationHeaderValue(IEnumerable<KeyValuePair<string, string>> parameters, string signature) { return new StringBuilder("OAuth ").Append(parameters.Concat(new KeyValuePair<string, string>("oauth_signature", signature)).Where(x => x.Key.StartsWith("oauth_")).Select(x => string.Format("{0}=\"{1}\"", x.Key, x.Value.EncodeRFC3986())).Join(",")).ToString(); }

            string GenerateSignature(IEnumerable<KeyValuePair<string, string>> parameters)
            {
                StringBuilder dataToSign = new StringBuilder().Append(this.method).Append("&").Append(this.url.EncodeRFC3986()).Append("&").Append(parameters.OrderBy(x => x.Key).Select(x => string.Format("{0}={1}", x.Key, x.Value)).Join("&").EncodeRFC3986());
                var signatureKey = string.Format("{0}&{1}", this.oauth.ConsumerSecret.EncodeRFC3986(), this.oauth.AccessSecret.EncodeRFC3986());
                var sha1 = new HMACSHA1(Encoding.ASCII.GetBytes(signatureKey));
                var signatureBytes = sha1.ComputeHash(Encoding.ASCII.GetBytes(dataToSign.ToString()));
                return Convert.ToBase64String(signatureBytes);
            }

            void AddOAuthParameters(IDictionary<string, string> parameters, string timestamp, string nonce)
            {
                parameters.Add("oauth_version", VERSION);
                parameters.Add("oauth_consumer_key", this.oauth.ConsumerKey);
                parameters.Add("oauth_nonce", nonce);
                parameters.Add("oauth_signature_method", SIGNATURE_METHOD);
                parameters.Add("oauth_timestamp", timestamp);
                parameters.Add("oauth_token", this.oauth.AccessToken);
            }

            static string GetTimestamp() { return ((int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds).ToString(); }
            static string CreateNonce() { return new Random().Next(0x0000000, 0x7fffffff).ToString("X8"); }
        }
        #endregion
    }

    public static class TinyTwitterHelperExtensions
    {
        public static string Join<T>(this IEnumerable<T> items, string separator) { return string.Join(separator, items.ToArray()); }
        public static IEnumerable<T> Concat<T>(this IEnumerable<T> items, T value) { return items.Concat(new[] {value}); }

        public static string EncodeRFC3986(this string value)
        {
            // From Twitterizer http://www.twitterizer.net/
            if (string.IsNullOrEmpty(value))
                return string.Empty;
            var encoded = Uri.EscapeDataString(value);
            return Regex.Replace(encoded, "(%[0-9a-f][0-9a-f])", c => c.Value.ToUpper()).Replace("(", "%28").Replace(")", "%29").Replace("$", "%24").Replace("!", "%21").Replace("*", "%2A").Replace("'", "%27").Replace("%7E", "~");
        }
    }
}