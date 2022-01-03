using Avira.Acp.Caching.Headers;

namespace Avira.Acp.Messages
{
    public interface IHeaderCollection
    {
        CacheControlHeaderValue CacheControl { get; set; }

        CacheControlHeaderValue AviraCacheControl { get; set; }

        string Get(string name);

        void Append(string name, string value);

        bool Contains(string name, string value);

        bool Contains(string name);
    }
}