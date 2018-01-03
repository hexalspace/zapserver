using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Web.Script.Serialization;

namespace zapserver
{
    class TogglApi
    {
        private static void prepareDefaultsForRequest(HttpRequestMessage req, string togglApiKey)
        {
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes($"{togglApiKey}:api_token")));
            req.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        } 

        public static async Task<int?> getRunningTogglTimeEntryIdAsync(string togglApiKey)
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("https://www.toggl.com/api/v8/time_entries/current"),
            };

            prepareDefaultsForRequest(request, togglApiKey);

            var result = await m_httpClient.SendAsync(request);
            var stringResult = await result.Content.ReadAsStringAsync();

            var jss = new JavaScriptSerializer();
            var dict = jss.Deserialize<Dictionary<string, dynamic>>(stringResult);
            int runningTimer;

            try
            {
                runningTimer = dict["data"]["id"];
            }
            catch
            {
                return null;
            }

            return runningTimer;
        }

        public static async Task stopTimeEntry(string togglApiKey, int timeEntryId)
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Put,
                RequestUri = new Uri($"https://www.toggl.com/api/v8/time_entries/{timeEntryId}/stop"),
            };

            prepareDefaultsForRequest(request, togglApiKey);

            var result = await m_httpClient.SendAsync(request);
            return;
        }

        private static readonly HttpClient m_httpClient = new HttpClient();
    }
}
