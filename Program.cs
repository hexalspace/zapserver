using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

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

                if (!routeHandlerDictionary.ContainsKey(route))
                {
                    continue;
                }

                var handlerFunction = routeHandlerDictionary[route];
                await handlerFunction(context.Request, context.Response, kvStore);
            }
        }


        public static async Task handleTogglResumeRequest(HttpListenerRequest req, HttpListenerResponse res, IKeyValueStore kvStore)
        {
            string toggleApiKey = kvStore.getValue(togglApiKeyKey);

            int? lastId = kvStore.getValueAsNullableInt(togglLastStoppedIdKey);
            int? pid = kvStore.getValueAsNullableInt(togglLastStoppedPidKey);
            string description = kvStore.getValue(togglLastStoppedDescKey);
            string tagString = kvStore.getValue(togglLastStoppedTagsKey);
            IEnumerable<string> tags = null;

            if (!lastId.HasValue)
            {
                res.StatusCode = 409;
                res.Close();
                return;
            }

            if (tagString != null)
            {
                tags = tagString.Split(',');
            }

            await TogglApi.startTimeEntry(toggleApiKey, description, tags, pid);

            kvStore.remove(togglLastStoppedIdKey);
            kvStore.remove(togglLastStoppedPidKey);
            kvStore.remove(togglLastStoppedDescKey);
            kvStore.remove(togglLastStoppedTagsKey);

            res.StatusCode = 200;
            res.Close();
        }


        public static async Task handleTogglStopRequest(HttpListenerRequest req, HttpListenerResponse res, IKeyValueStore kvStore)
        {
            string toggleApiKey = kvStore.getValue(togglApiKeyKey);

            TogglApi.TimeEntry timeEntry = await TogglApi.getRunningTogglTimeEntryIdAsync(toggleApiKey);

            if (timeEntry == null)
            {
                res.StatusCode = 409;
                res.Close();
                return;
            }

            await TogglApi.stopTimeEntry(toggleApiKey, timeEntry.id);

            kvStore.remove(togglLastStoppedIdKey);
            kvStore.remove(togglLastStoppedPidKey);
            kvStore.remove(togglLastStoppedDescKey);
            kvStore.remove(togglLastStoppedTagsKey);

            kvStore.setValue(togglLastStoppedIdKey, timeEntry.id.ToString());

            if (timeEntry.pid.HasValue)
            {
                kvStore.setValue(togglLastStoppedPidKey, timeEntry.pid.Value.ToString());
            }

            if (timeEntry.description != null)
            {
                kvStore.setValue(togglLastStoppedDescKey, timeEntry.description);
            }
            
            if (timeEntry.tags != null)
            {
                kvStore.setValue(togglLastStoppedTagsKey, string.Join(",", timeEntry.tags));
            }
            
            res.StatusCode = 200;
            res.Close();
        }
    }
}
