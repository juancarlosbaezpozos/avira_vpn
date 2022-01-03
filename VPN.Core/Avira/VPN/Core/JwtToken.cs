using System;
using System.Collections.Generic;
using System.Text;
using JWT;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Avira.VPN.Core
{
    public class JwtToken
    {
        public static string Create(Dictionary<string, object> payload, string secret)
        {
            return JsonWebToken.Encode(payload, secret, JwtHashAlgorithm.HS256);
        }

        public static bool IsAnonymousToken(string token)
        {
            JObject payload = GetPayload(token);
            if (payload == null)
            {
                return false;
            }

            payload.TryGetValue("anon", out var value);
            if (value != null)
            {
                return (bool)value;
            }

            return false;
        }

        public static JObject GetPayload(string jwtToken)
        {
            if (string.IsNullOrEmpty(jwtToken))
            {
                return null;
            }

            try
            {
                string[] array = jwtToken.Split('.');
                if (array.Length >= 2)
                {
                    int num = ((array[1].Length % 4 != 0) ? (4 - array[1].Length % 4) : 0);
                    for (int i = 0; i < num; i++)
                    {
                        array[1] += "=";
                    }

                    byte[] array2 = Convert.FromBase64String(array[1]);
                    return JObject.Parse(Encoding.UTF8.GetString(array2, 0, array2.Length));
                }
            }
            catch (Exception exception)
            {
                Log.Warning(exception, "Failed to get token payload.");
            }

            return null;
        }
    }
}