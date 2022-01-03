using System.Collections.Generic;
using System.Linq;
using Avira.Acp.Caching.Headers;
using ServiceStack.Text;

namespace Avira.Acp.Messages
{
    public class HeaderCollection : IHeaderCollection
    {
        private const string CacheControlHeaderName = "Cache-Control";

        private const string AviraCacheControlHeaderName = "X-Avira-Cache-Control";

        private readonly object headersLock = new object();

        private IDictionary<string, string> headers;

        private CacheControlHeaderValue cacheControlHeaderValue;

        private CacheControlHeaderValue aviraCacheControlHeaderValue;

        public CacheControlHeaderValue CacheControl
        {
            get
            {
                return cacheControlHeaderValue ??
                       (cacheControlHeaderValue = CacheControlHeaderValue.Parse(Get("Cache-Control")));
            }
            set
            {
                cacheControlHeaderValue = value;
                Set("Cache-Control", cacheControlHeaderValue.ToString());
            }
        }

        public CacheControlHeaderValue AviraCacheControl
        {
            get
            {
                return aviraCacheControlHeaderValue ?? (aviraCacheControlHeaderValue =
                    CacheControlHeaderValue.Parse(Get("X-Avira-Cache-Control")));
            }
            set
            {
                aviraCacheControlHeaderValue = value;
                Set("X-Avira-Cache-Control", aviraCacheControlHeaderValue.ToString());
            }
        }

        public HeaderCollection(IDictionary<string, string> headers)
        {
            this.headers = headers;
        }

        public static string SerializeToJson(HeaderCollection headerCollection)
        {
            return headerCollection.SerializeToJson();
        }

        public static HeaderCollection DeserealizeFromJson(string json)
        {
            return new HeaderCollection(JsonSerializer.DeserializeFromString<Dictionary<string, string>>(json));
        }

        public string Get(string name)
        {
            lock (headersLock)
            {
                if (headers == null)
                {
                    return null;
                }

                string value;
                return headers.TryGetValue(name, out value) ? value : null;
            }
        }

        public void Append(string name, string value)
        {
            lock (headersLock)
            {
                if (headers == null)
                {
                    headers = new Dictionary<string, string>();
                }

                if (headers.TryGetValue(name, out var value2) && value2 != null)
                {
                    if (!value2.Split(',').Any((string v) => v.Trim() == value))
                    {
                        headers[name] = value2 + ", " + value;
                    }
                }
                else
                {
                    headers[name] = value;
                }
            }
        }

        public bool Contains(string name, string value)
        {
            return Get(name)?.Split(',').Any((string h) => h.Trim().Equals(value)) ?? false;
        }

        public bool Contains(string name)
        {
            return !string.IsNullOrEmpty(Get(name));
        }

        private void Set(string name, string value)
        {
            lock (headersLock)
            {
                if (headers == null)
                {
                    headers = new Dictionary<string, string>();
                }

                headers[name] = value;
            }
        }

        private string SerializeToJson()
        {
            lock (headersLock)
            {
                if (headers == null)
                {
                    return "null";
                }

                return JsonSerializer.SerializeToString(headers);
            }
        }
    }
}