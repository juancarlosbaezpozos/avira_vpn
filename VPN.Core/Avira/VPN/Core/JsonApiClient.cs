using System;
using System.Threading.Tasks;
using Avira.Messaging;
using Newtonsoft.Json.Linq;

namespace Avira.VPN.Core
{
    public class JsonApiClient<T> : IApiClient<T> where T : class
    {
        private IApiClient<JObject> httpClient;

        public Func<bool> InputDataValidator;

        [Routing("Data")]
        public T Data
        {
            get
            {
                JObject data = httpClient.Data;
                if (data == null || !data.HasValues || (InputDataValidator != null && !InputDataValidator()))
                {
                    Refresh(string.Empty).CatchAll();
                    return null;
                }

                return ConvertData(data);
            }
        }

        public bool MultipleApiUpdateOnReconnect { get; set; } = true;


        public event EventHandler DataChanged;

        public event EventHandler<EventArgs<T>> dataChangedT;

        [Routing("DataChanged")]
        public event EventHandler<EventArgs<T>> DataChangedT
        {
            add { dataChangedT += value; }
            remove { dataChangedT -= value; }
        }

        public JsonApiClient(IApiClient<JObject> httpClient)
        {
            this.httpClient = httpClient;
            this.httpClient.DataChanged += delegate
            {
                this.DataChanged?.Invoke(this, new EventArgs());
                this.dataChangedT?.Invoke(this, new EventArgs<T>(ConvertData(this.httpClient.Data)));
            };
        }

        public async Task Refresh(string parameters, JObject body = null)
        {
            if (InputDataValidator == null || InputDataValidator())
            {
                await httpClient.Refresh(parameters, body);
            }
        }

        public static T ConvertData(JObject data)
        {
            return ObjectFromPath(data, "data/attributes");
        }

        public static T ObjectFromPath(JObject data, string path)
        {
            string[] array = path.Split('/');
            JObject jObject = data;
            string[] array2 = array;
            foreach (string propertyName in array2)
            {
                if (jObject.TryGetValue(propertyName, out var value))
                {
                    if (value is JArray)
                    {
                        value = ((JArray)value)[0];
                    }

                    jObject = value.ToObject<JObject>();
                    if (jObject == null)
                    {
                        return null;
                    }
                }
            }

            return jObject.ToObject<T>();
        }

        public void Clear()
        {
            httpClient.Clear();
        }

        public void UpdateCache(T data)
        {
            throw new NotImplementedException();
        }

        public Task<string> Get(string uri)
        {
            throw new NotImplementedException();
        }

        public Task<string> Post(string uri, string parameters)
        {
            throw new NotImplementedException();
        }

        public Task<string> Put(string uri, string parameters)
        {
            throw new NotImplementedException();
        }
    }
}