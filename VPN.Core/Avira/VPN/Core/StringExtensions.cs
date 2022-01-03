using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Avira.VPN.Core
{
    public static class StringExtensions
    {
        public static bool IsValidJson(this string value)
        {
            value = value.Trim();
            if ((value.StartsWith("{", StringComparison.Ordinal) && value.EndsWith("}", StringComparison.Ordinal)) ||
                (value.StartsWith("[", StringComparison.Ordinal) && value.EndsWith("]", StringComparison.Ordinal)))
            {
                try
                {
                    JToken.Parse(value);
                    return true;
                }
                catch (JsonReaderException)
                {
                }
                catch (Exception)
                {
                }
            }

            return false;
        }
    }
}