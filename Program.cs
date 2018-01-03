using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace zapserver
{
    class Program
    {
        public static readonly string sqliteFileName = "zapserver.sqlite";

        public static void Main(string[] args)
        {
            asyncMain(args).Wait();
        }

        public static async Task asyncMain(string[] args)
        {
            IKeyValueStore kvStore = new SqliteKeyValueStore(sqliteFileName);
            HttpListener httpListener = new HttpListener();

            httpListener.Prefixes.Add("");

            httpListener.Start();

            while (httpListener.IsListening)
            {
                HttpListenerContext context = await httpListener.GetContextAsync();
                HttpListenerRequest request = context.Request;
                HttpListenerResponse response = context.Response;
            }

        }


        public static async Task handleTogglStopRequest(HttpListenerRequest req, HttpListenerResponse res)
        {
            var id = await TogglApi.getRunningTogglTimeEntryIdAsync("");

            if (!id.HasValue)
            {
                return;
            }

            await TogglApi.stopTimeEntry("", id.Value);
        }
    }
}
