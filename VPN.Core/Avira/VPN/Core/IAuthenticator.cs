using System;
using System.Threading.Tasks;
using Avira.Messaging;
using Newtonsoft.Json.Linq;

namespace Avira.VPN.Core
{
    public interface IAuthenticator
    {
        string AccessToken { get; }

        string AccessTokenHash { get; }

        event EventHandler<EventArgs<string>> AccessTokenChanged;

        Task Refresh();

        Task Refresh(string token);

        bool IsAuthenticated();

        Task<JObject> Login(JObject credentials);

        Task RefreshAccessToken(string token);

        void ApplyTokenData(string tokenData);

        string GetInAppLoginUrl();

        void Clear();
    }
}