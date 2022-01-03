using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Avira.Common.Acp.AppClient;
using Avira.Common.Core;
using Avira.Messaging;
using Avira.VPN.Core;
using Avira.VPN.Core.Win;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Avira.VPN.OeConnector
{
    [DiContainer.Export(typeof(IAuthenticator))]
    public class OeAuthenticator : IAuthenticator
    {
        private const string clientId = "vpn";

        private const string Host = "backend";

        private readonly IResourceClient<AuthentificationData> resourceClient;

        private readonly AsyncLock queryBackendLock = new AsyncLock();

        private AuthentificationData authentificationData;

        private int authentificationDataRequestTime;

        private int waitForRegisteredUserRetries;

        public string AccessToken
        {
            get
            {
                UpdateAccessTokenIfNecessary().CatchAll();
                return authentificationData?.AccessToken ?? ProductSettings.AccessToken;
            }
        }

        public JObject TokenPayload => GetPayload(AccessToken);

        public string AccessTokenHash
        {
            get
            {
                string text = HashAccessToken(AccessToken);
                if (!string.IsNullOrEmpty(text))
                {
                    return "sha1:" + text;
                }

                return string.Empty;
            }
        }

        public event EventHandler<EventArgs<string>> AccessTokenChanged;

        public OeAuthenticator()
            : this(new ResourceClient<AuthentificationData>(DiContainer.Resolve<IAcpCommunicator>(), "backend",
                "/v2/oauth"))
        {
        }

        public OeAuthenticator(IResourceClient<AuthentificationData> resourceClient)
        {
            this.resourceClient = resourceClient;
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
                Serilog.Log.Warning(exception, "Failed to get payload.");
            }

            return null;
        }

        public bool IsAuthenticated()
        {
            if (TokenPayload == null)
            {
                return false;
            }

            TokenPayload.TryGetValue("anon", out var value);
            if (value != null)
            {
                return !(bool)value;
            }

            return false;
        }

        public async Task UpdateAccessTokenIfNecessary()
        {
            if (!IsTokenInvalid())
            {
                return;
            }

            using (await queryBackendLock.LockAsync())
            {
                if (IsTokenInvalid())
                {
                    if (authentificationData == null)
                    {
                        await RequestNewAccessToken();
                    }
                    else if (!(await RequestAccessTokenUsingRefreshToken()))
                    {
                        await RequestNewAccessToken();
                    }
                }
            }
        }

        internal static string HashAccessToken(string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken))
            {
                return string.Empty;
            }

            using SHA1Managed sHA1Managed = new SHA1Managed();
            byte[] source = sHA1Managed.ComputeHash(Encoding.UTF8.GetBytes(accessToken));
            return string.Join("", source.Select((byte b) => b.ToString("x2")).ToArray());
        }

        private Task<bool> RequestNewAccessToken()
        {
            return QueryBackend(new JObject
            {
                {
                    "grant_type",
                    (JToken)"client_token"
                },
                {
                    "client_id",
                    (JToken)"vpn"
                }
            });
        }

        private async Task<bool> RequestAccessTokenUsingRefreshToken()
        {
            JObject data = new JObject
            {
                {
                    "grant_type",
                    (JToken)"refresh_token"
                },
                {
                    "client_id",
                    (JToken)"vpn"
                },
                {
                    "refresh_token",
                    (JToken)authentificationData.RefreshToken
                }
            };
            string url = DiContainer.GetValue<string>("OeApiUrl") + "oauth";
            string authorization =
                "Basic YXZpcmEvdnBuOjMyZTIzMDU1OWM0ZTA1MWQyZTJkZDRlYTI0NzllMGEzNTY1OTQzMDRlMmNjY2Y3YjQzNThjOTMwOTQwMDQxNzk=";
            IHttpAsyncHelper httpAsyncHelper = HttpAsyncHelper.CreateInstance();
            httpAsyncHelper.Authorization = authorization;
            try
            {
                string text = await httpAsyncHelper.Post(url, data);
                if (IsError(text))
                {
                    Serilog.Log.Warning("RequestAccessTokenUsingRefreshToken failed. Response: " + text);
                    return false;
                }

                UpdateAuthentificationData(JsonConvert.DeserializeObject<AuthentificationData>(text));
                return true;
            }
            catch (Exception exception)
            {
                Serilog.Log.Warning(exception, "RequestAccessTokenUsingRefreshToken failed.");
                return false;
            }
        }

        private bool IsError(string response)
        {
            return JsonConvert.DeserializeObject<JObject>(response)!.GetValue("errors") != null;
        }

        private async Task<bool> QueryBackend(JObject payload)
        {
            if (DiContainer.Resolve<IAcpCommunicator>() == null ||
                !DiContainer.Resolve<IAcpCommunicator>().IsConnected())
            {
                return false;
            }

            try
            {
                Tuple<AuthentificationData, HttpStatusCode?> tuple =
                    await resourceClient.Post(payload.ToString(Formatting.None));
                if (tuple.Item2 != HttpStatusCode.OK)
                {
                    Serilog.Log.Warning(
                        $"Failed to query OE Backend for token. Error: {tuple.Item2} Payload: {payload}");
                    return false;
                }

                UpdateAuthentificationData(tuple.Item1);
            }
            catch (Exception exception)
            {
                Serilog.Log.Warning(exception, "Query OE Backend failed.");
                return false;
            }

            return true;
        }

        private void UpdateAuthentificationData(AuthentificationData data)
        {
            string accessToken = AccessToken;
            authentificationData = data;
            ProductSettings.AccessToken = authentificationData.AccessToken;
            authentificationDataRequestTime = (int)DateTime.Now.ToUnixTimeStamp();
            if (TokenHasChanged(accessToken))
            {
                this.AccessTokenChanged?.Invoke(this, new EventArgs<string>(ProductSettings.AccessToken));
            }
        }

        internal bool TokenHasChanged(string previousToken)
        {
            JObject payload = GetPayload(previousToken);
            JObject payload2 = GetPayload(AccessToken);
            if (GetULongValue(payload2, "uid", 0uL) == GetULongValue(payload, "uid", 0uL))
            {
                if (GetULongValue(payload2, "uid", 0uL) != 0L)
                {
                    return previousToken != AccessToken;
                }

                return false;
            }

            return true;
        }

        private ulong GetULongValue(JObject data, string key, ulong defaultValue = 0uL)
        {
            if (data == null)
            {
                return defaultValue;
            }

            data.TryGetValue(key, out var value);
            if (value == null)
            {
                return defaultValue;
            }

            return (ulong)value;
        }

        private bool IsTokenInvalid()
        {
            if (authentificationData != null)
            {
                return (int)DateTime.Now.ToUnixTimeStamp() >
                       authentificationDataRequestTime + authentificationData.ExpiresIn;
            }

            return true;
        }

        public async Task PollForRegisteredUser()
        {
            waitForRegisteredUserRetries = 0;
            await Task.Run(async delegate
            {
                while (!IsAuthenticated())
                {
                    if (waitForRegisteredUserRetries > 60)
                    {
                        Serilog.Log.Debug("User still not registered. Giving up to.");
                        break;
                    }

                    waitForRegisteredUserRetries++;
                    Serilog.Log.Debug(
                        $"Got anonymous token, refreshing access token. Attempt: {waitForRegisteredUserRetries}");
                    await Task.Delay(1000);
                    await Refresh();
                }
            });
        }

        public Task Refresh()
        {
            authentificationData = null;
            return UpdateAccessTokenIfNecessary();
        }

        public Task<JObject> Login(JObject credentials)
        {
            throw new NotImplementedException();
        }

        public Task Refresh(string token)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public void ApplyTokenData(string tokenData)
        {
            throw new NotImplementedException();
        }

        public string GetInAppLoginUrl()
        {
            throw new NotImplementedException();
        }

        public Task RefreshAccessToken(string token)
        {
            throw new NotImplementedException();
        }
    }
}