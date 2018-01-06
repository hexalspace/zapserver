using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Web.Script.Serialization;
using System.Linq;

namespace zapserver
{
    class TogglApi
    {
        public static readonly string defaultWorkspaceName = "Default";

        private static void prepareDefaultsForRequest(HttpRequestMessage req, string togglApiKey)
        {
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes($"{togglApiKey}:api_token")));
            req.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }

        private class TimeEntryResponse
        {
            public TimeEntry data;
        }

        public class TimeEntry
        {
            public int id;
            public int? pid;
            public string description;
            public IEnumerable<string> tags;
        }

        public static async Task<TimeEntry> getRunningTogglTimeEntryIdAsync(string togglApiKey)
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri("https://www.toggl.com/api/v8/time_entries/current"),
            };

            prepareDefaultsForRequest(request, togglApiKey);

            var result = await httpClient.SendAsync(request);
            var stringResult = await result.Content.ReadAsStringAsync();

            try
            {
                var timeEntryResponse = javaScriptSerializer.Deserialize<TimeEntryResponse>(stringResult);
                return timeEntryResponse.data;
            }
            catch
            {
                return null;
            }
        }

        public class Workspace
        {
            public int id;
            public string name;
        }

        public static async Task<int?> getDefaultWorkspace(string togglApiKey)
        {
            return await getWorkspaceId(togglApiKey, defaultWorkspaceName);
        }

        public static async Task<int?> getWorkspaceId(string togglApiKey, string workspaceName)
        {
            if (string.IsNullOrEmpty(workspaceName))
            {
                return null;
            }

            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://www.toggl.com/api/v8/workspaces"),
            };

            prepareDefaultsForRequest(request, togglApiKey);

            var result = await httpClient.SendAsync(request);
            var stringResult = await result.Content.ReadAsStringAsync();

            try
            {
                var workspacesResponse = javaScriptSerializer.Deserialize<IEnumerable<Workspace>>(stringResult);
                return workspacesResponse.FirstOrDefault(p => p.name == workspaceName)?.id;
            }
            catch
            {
                return null;
            }
        }

        public class Project
        {
            public int id;
            public string name;
        }

        private static async Task<IEnumerable<Project>> getProjectsForWorkspace(string togglApiKey, int workspaceId)
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://www.toggl.com/api/v8/workspaces/{workspaceId}/projects"),
            };

            prepareDefaultsForRequest(request, togglApiKey);

            var result = await httpClient.SendAsync(request);
            var stringResult = await result.Content.ReadAsStringAsync();

            try
            {
                return javaScriptSerializer.Deserialize<IEnumerable<Project>>(stringResult);
            }
            catch
            {
                return null;
            }
        }

        public static async Task<int?> getProjectIdAsync(string togglApiKey, string projectName)
        {
            if (string.IsNullOrEmpty(projectName))
            {
                return null;
            }

            var projects = await getProjectsForWorkspace(togglApiKey, (await getDefaultWorkspace(togglApiKey)).Value);
            return projects.FirstOrDefault(p => p.name == projectName)?.id;
        }

        public static async Task stopTimeEntry(string togglApiKey, int timeEntryId)
        {
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Put,
                RequestUri = new Uri($"https://www.toggl.com/api/v8/time_entries/{timeEntryId}/stop"),
            };

            prepareDefaultsForRequest(request, togglApiKey);

            var result = await httpClient.SendAsync(request);
            return;
        }

        private class TimeEntryRequest
        {
            public TimeEntryInternal time_entry;
        }

        private class TimeEntryInternal
        {
            public int? pid;
            public string description;
            public IEnumerable<string> tags;
            public string created_with;
        }

        public static async Task startTimeEntry(string togglApiKey, string description, IEnumerable<string> tags, int? pid)
        {
            var jsonContent = new TimeEntryRequest { time_entry = new TimeEntryInternal { pid = pid, description = description, tags = tags, created_with = "zapserver" } };
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri($"https://www.toggl.com/api/v8/time_entries/start"),
                Content = new StringContent(javaScriptSerializer.Serialize(jsonContent), Encoding.UTF8, "application/json")
            };

            prepareDefaultsForRequest(request, togglApiKey);

            var result = await httpClient.SendAsync(request);
            return;
        }

        private static readonly HttpClient httpClient = new HttpClient();
        private static readonly JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
    }
}
