using System;
using Newtonsoft.Json;
using Serilog;

namespace Avira.VPN.Core.Win
{
    public static class GenericAccessor
    {
        public static T Get<T>(IStorage storage, string key, T defaultValue = default(T))
        {
            string text = string.Empty;
            try
            {
                text = storage.Get(key);
                return (T)(string.IsNullOrEmpty(text)
                    ? ((object)defaultValue)
                    : ((object)JsonConvert.DeserializeObject<T>(text)));
            }
            catch (Exception exception)
            {
                Log.Warning(exception, "Failed to deserialize value for " + key + ":" + text + ".");
                return defaultValue;
            }
        }

        public static void Set<T>(IStorage storage, string key, T value)
        {
            if (value != null)
            {
                storage.Set(key, JsonConvert.SerializeObject(value));
            }
        }

        public static string Get(IStorage storage, string key, string defaultValue = "")
        {
            try
            {
                string text = storage.Get(key);
                if (!string.IsNullOrEmpty(text))
                {
                    return text;
                }

                return defaultValue;
            }
            catch (Exception exception)
            {
                Log.Warning(exception, "Failed to deserialize value.");
                return defaultValue;
            }
        }

        public static void Set(IStorage storage, string key, string value)
        {
            if (value != null)
            {
                storage.Set(key, value);
            }
        }

        public static bool Get(IStorage storage, string key, bool defaultValue = false)
        {
            return bool.Parse(Get(storage, key, defaultValue.ToString()));
        }

        public static void Set(IStorage storage, string key, bool value)
        {
            Set(storage, key, value.ToString());
        }

        public static int Get(IStorage storage, string key, int defaultValue = 0)
        {
            return int.Parse(Get(storage, key, defaultValue.ToString()));
        }

        public static void Set(IStorage storage, string key, int value)
        {
            Set(storage, key, value.ToString());
        }

        public static long Get(IStorage storage, string key, long defaultValue = 0L)
        {
            return long.Parse(Get(storage, key, defaultValue.ToString()));
        }

        public static void Set(IStorage storage, string key, long value)
        {
            Set(storage, key, value.ToString());
        }
    }
}