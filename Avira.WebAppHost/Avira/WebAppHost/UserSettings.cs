using System;
using System.Reflection;
using Avira.VPN.Core.Win;
using Avira.Win.Messaging;
using Microsoft.Win32;
using Newtonsoft.Json;
using Serilog;

namespace Avira.WebAppHost
{
    public class UserSettings
    {
        internal class UserSettingsData
        {
            [JsonProperty(PropertyName = "autoStart")]
            public bool RunAtStartup { get; set; }
        }

        private const string CurrentUserRunKey = "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run";

        public static bool RunAtStartup
        {
            get
            {
                using RegistryKey registryKey =
                    Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run",
                        writable: false);
                return registryKey?.GetValue(ProductSettings.ProductName) != null;
            }
            set
            {
                using RegistryKey registryKey =
                    Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run",
                        writable: true);
                if (value)
                {
                    string location = Assembly.GetExecutingAssembly().Location;
                    if (!string.IsNullOrEmpty(location))
                    {
                        registryKey?.SetValue(ProductSettings.ProductName, location + " /hide");
                    }
                }
                else if (registryKey != null && registryKey.GetValue(ProductSettings.ProductName) != null)
                {
                    registryKey.DeleteValue(ProductSettings.ProductName);
                }
            }
        }

        public Message HandleRequest(Message message)
        {
            string requestType = GetRequestType(message);
            if (!(requestType == "get"))
            {
                if (requestType == "set")
                {
                    Log.Information($"Setting UserSettings: {message.Params}");
                    RunAtStartup = JsonConvert.DeserializeObject<UserSettingsData>(message.Params.ToString())!
                        .RunAtStartup;
                    return Message.CreateResponse(message, "OK");
                }

                return Message.CreateFailedResponse(message, JsonRpcErrors.InvalidParams);
            }

            UserSettingsData data = new UserSettingsData
            {
                RunAtStartup = RunAtStartup
            };
            return Message.CreateResponse(message, Message.ToJObject(data));
        }

        private static string GetRequestType(Message message)
        {
            string text = message.Method.Remove(0, "userSettings".Length + 1);
            int num = text.IndexOf("/", StringComparison.Ordinal);
            if (num == 0)
            {
                throw new ArgumentException("Invalid user settings message.");
            }

            return text.Substring(num + 1);
        }
    }
}