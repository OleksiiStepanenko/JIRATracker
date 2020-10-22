// // <copyright>3Shape A/S</copyright>

using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace JiraTracker
{
    public class HTTPRequests
    {
        private const string EndPoint = "https://jira.3shape.com";
        private const string Api = "rest/api/2";
        private const string RequestFormat = "{0}/{1}/{2}";

        public Func<string> UserProvider { get; set; }
        public Func<string> PasswordProvider { get; set; }

        public bool Request(string request, out string result)
        {
            byte[] cred = Encoding.UTF8.GetBytes(UserProvider() + ":" + PasswordProvider());
            HttpClient client = new HttpClient { BaseAddress = new Uri(EndPoint) };
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(cred));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            HttpResponseMessage message = client.GetAsync(string.Format(RequestFormat, EndPoint, Api, request)).Result;
            result = message.IsSuccessStatusCode ? message.Content.ReadAsStringAsync().Result : null;
            return result != null;
        }
    }
}