using System;

namespace Avira.Acp.Caching.Headers
{
    public class CacheControlHeaderValue
    {
        private static class Parser
        {
            private const string MaxAgeString = "max-age";

            private const string NoCacheString = "no-cache";

            private const string OnlyIfCachedString = "only-if-cached";

            public static CacheControlHeaderValue Parse(string value)
            {
                if (value == null)
                {
                    return new CacheControlHeaderValue();
                }

                TimeSpan? maxAge = GetMaxAge(value);
                if (maxAge.HasValue)
                {
                    return WithMaxAge(maxAge.Value);
                }

                if (value.Contains("no-cache"))
                {
                    return WithNoCache();
                }

                if (value.Contains("only-if-cached"))
                {
                    return WithOnlyIfCached();
                }

                return new CacheControlHeaderValue();
            }

            public static string ConvertToString(CacheControlHeaderValue cacheControlHeaderValue)
            {
                if (cacheControlHeaderValue.MaxAge.HasValue)
                {
                    return string.Format("{0}=", "max-age") + cacheControlHeaderValue.MaxAge.Value.TotalSeconds;
                }

                if (cacheControlHeaderValue.NoCache)
                {
                    return "no-cache";
                }

                if (cacheControlHeaderValue.OnlyIfCached)
                {
                    return "only-if-cached";
                }

                return string.Empty;
            }

            private static TimeSpan? GetMaxAge(string value)
            {
                if (!value.Contains("max-age"))
                {
                    return null;
                }

                TimeSpan? result = null;
                if (int.TryParse(value.Substring("max-age".Length + 1).Trim(), out var result2))
                {
                    result = TimeSpan.FromSeconds(result2);
                }

                return result;
            }
        }

        public TimeSpan? MaxAge { get; }

        public bool NoCache { get; }

        public bool OnlyIfCached { get; }

        public CacheControlHeaderValue()
        {
        }

        private CacheControlHeaderValue(TimeSpan? maxAge, bool noCache, bool onlyIfCahed)
        {
            MaxAge = maxAge;
            NoCache = noCache;
            OnlyIfCached = onlyIfCahed;
        }

        public static CacheControlHeaderValue WithMaxAge(TimeSpan maxAge)
        {
            return new CacheControlHeaderValue(maxAge, noCache: false, onlyIfCahed: false);
        }

        public static CacheControlHeaderValue WithNoCache()
        {
            return new CacheControlHeaderValue(null, noCache: true, onlyIfCahed: false);
        }

        public static CacheControlHeaderValue WithOnlyIfCached()
        {
            return new CacheControlHeaderValue(null, noCache: false, onlyIfCahed: true);
        }

        public static CacheControlHeaderValue Parse(string value)
        {
            return Parser.Parse(value);
        }

        public override string ToString()
        {
            return Parser.ConvertToString(this);
        }
    }
}