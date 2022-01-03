using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Avira.VPN.Core.Win
{
    public class JsonStorage
    {
        private readonly JObject data;

        public JsonStorage(string jsonFilePath)
        {
            try
            {
                data = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(jsonFilePath));
            }
            catch (Exception)
            {
                data = new JObject();
            }
        }

        public JsonStorage(JObject data)
        {
            this.data = data;
        }

        public string Get(string propertyName, string defaultValue = "")
        {
            if (data.TryGetValue(propertyName, out var value))
            {
                return value.ToString();
            }

            return defaultValue;
        }

        public List<string> GetList(string propertyName)
        {
            if (data.TryGetValue(propertyName, out var value))
            {
                return value.ToObject<List<string>>();
            }

            return new List<string>();
        }
    }
}