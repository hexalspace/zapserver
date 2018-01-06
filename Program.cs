using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Web.Script.Serialization;

namespace zapserver
{
    class Program
    {
        public static readonly string sqliteFileName = "zapserver.sqlite";

        public static readonly string togglApiKeyKey = "toggl-apiKey";
        public static readonly string togglLastStoppedIdKey = "toggl-lastStoppedId";
        public static readonly string togglLastStoppedPidKey = "toggl-lastStoppedPid";
        public static readonly string togglLastStoppedDescKey = "toggl-lastStoppedDesc";
        public static readonly string togglLastStoppedTagsKey = "toggl-lastStoppedTags";

        public static void Main(string[] args)
        {
            asyncMain(args).Wait();
        }

        public delegate Task requestHandlerDelegate(HttpListenerRequest req, HttpListenerResponse res, IKeyValueStore kvStore);

        public static readonly Dictionary<string, requestHandlerDelegate> routeHandlerDictionary = new Dictionary<string, requestHandlerDelegate>
        {
            {"toggl/stop/", handleTogglStopRequest },
            {"toggl/start/", handleTogglStartRequest },
            {"toggl/resume/", handleTogglResumeRequest },
        };
        

        public static async Task asyncMain(string[] args)
        {
            IKeyValueStore kvStore = new SqliteKeyValueStore(sqliteFileName);
            HttpListener httpListener = new HttpListener();

            foreach (var pair in routeHandlerDictionary)
            {
                httpListener.Prefixes.Add($"http://localhost:8080/{pair.Key}");
            }

            httpListener.Start();

            while (httpListener.IsListening)
            {
                HttpListenerContext context = await httpListener.GetContextAsync();

                var route = context.Request.Url.LocalPath;

                if (route.StartsWith("/"))
                {
                    route = route.Substring(1);
                }

                if (!route.EndsWith("/"))
                {
                    route = $"{route}/";
                }

                var matchingRoute = routeHandlerDictionary.FirstOrDefault(keyValue => route.StartsWith(keyValue.Key));
                if (matchingRoute.Equals(default(KeyValuePair<string, requestHandlerDelegate>)))
                {
                    continue;
                }

                var handlerFunction = matchingRoute.Value;

                try
                {
                    await handlerFunction(context.Request, context.Response, kvStore);
                }
                catch
                {
                    context.Response.StatusCode = 400;
                    context.Response.Close();
                }
            }
        }


        public static async Task handleTogglResumeRequest(HttpListenerRequest req, HttpListenerResponse res, IKeyValueStore kvStore)
        {
            string toggleApiKey = kvStore.getValue<string>(togglApiKeyKey);

            int? lastId = kvStore.getValue<int?>(togglLastStoppedIdKey);
            int? pid = kvStore.getValue<int?>(togglLastStoppedPidKey);
            string description = kvStore.getValue<string>(togglLastStoppedDescKey);
            IEnumerable<string> tags = kvStore.getValue<IEnumerable<string>>(togglLastStoppedTagsKey);

            if (!lastId.HasValue)
            {
                res.StatusCode = 409;
                res.Close();
                return;
            }

            await TogglApi.startTimeEntry(toggleApiKey, description, tags, pid);

            res.StatusCode = 200;
            res.Close();
        }


        public static async Task handleTogglStopRequest(HttpListenerRequest req, HttpListenerResponse res, IKeyValueStore kvStore)
        {
            string toggleApiKey = kvStore.getValue<string>(togglApiKeyKey);

            TogglApi.TimeEntry timeEntry = await TogglApi.getRunningTogglTimeEntryIdAsync(toggleApiKey);

            if (timeEntry == null)
            {
                res.StatusCode = 409;
                res.Close();
                return;
            }

            await TogglApi.stopTimeEntry(toggleApiKey, timeEntry.id);

            kvStore.setValue(togglLastStoppedIdKey, timeEntry.id);
            kvStore.setValue(togglLastStoppedPidKey, timeEntry.pid);
            kvStore.setValue(togglLastStoppedDescKey, timeEntry.description);
            kvStore.setValue(togglLastStoppedTagsKey, timeEntry.tags);
            
            res.StatusCode = 200;
            res.Close();
        }

        private class TimeEntryStart
        {
            public string projectName;
            public string description;
            public IEnumerable<string> tags;
        }

        public static async Task handleTogglStartRequest(HttpListenerRequest req, HttpListenerResponse res, IKeyValueStore kvStore)
        {
            string toggleApiKey = kvStore.getValue<string>(togglApiKeyKey);

            if (!req.HasEntityBody)
            {
                res.StatusCode = 400;
                res.Close();
                return;
            }

            string stringContent;

            using (System.IO.Stream body = req.InputStream)
            {
                using (System.IO.StreamReader reader = new System.IO.StreamReader(body, req.ContentEncoding))
                {
                    stringContent = reader.ReadToEnd();
                }
            }

            var timeEntryStart = javaScriptSerializer.Deserialize<TimeEntryStart>(stringContent);

            await TogglApi.startTimeEntry(toggleApiKey, timeEntryStart.description, timeEntryStart.tags, await TogglApi.getProjectIdAsync(toggleApiKey, timeEntryStart.projectName));

            res.StatusCode = 200;
            res.Close();
        }

        private static readonly JavaScriptSerializer javaScriptSerializer = new JavaScriptSerializer();
    }
}
