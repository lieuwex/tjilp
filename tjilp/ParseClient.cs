using System;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace tjilp
{
    public class ParseClient : IParse
    {
        const string API_URL = "https://api.parse.com/1";
        const string APP_OPEN_URL = API_URL + "/events/AppOpened";
        const string CREATE_OBJECT_URL = API_URL + "/classes/{0}";
        const string CUSTOM_EVENT_URL = API_URL + "/events/{0}";

        public ParseClient(string applicationId, string restApiKey)
        {
            this.ApplicationId = applicationId;
            this.RestApiKey = restApiKey;
        }

        string ApplicationId { get; set; }
        string RestApiKey { get; set; }

        public async Task TrackAppOpened() { await this.sendString(APP_OPEN_URL, "POST", "{}"); }
        public async Task SendObject(string className, object obj) { await this.sendString(string.Format(CREATE_OBJECT_URL, className), "POST", JsonConvert.SerializeObject(obj)); }
        public async Task TrackCustomEvent(string eventName, object dimensions)
        {
            await this.sendString(string.Format(CUSTOM_EVENT_URL, eventName), "POST", JsonConvert.SerializeObject(new
            {
                dimensions
            }));
        }

        async Task<string> sendString(string url, string method, string value)
        {
            if (url == null) throw new ArgumentNullException("url");
            if (value == null) throw new ArgumentNullException("value");

            using(var client = new WebClient())
            {
                client.Headers[HttpRequestHeader.ContentType] = "application/json";
                client.Headers.Add("X-Parse-Application-Id", this.ApplicationId);
                client.Headers.Add("X-Parse-REST-API-Key", this.RestApiKey);
                return await client.UploadStringTaskAsync(url, method, value);
            }
        }
    }
}