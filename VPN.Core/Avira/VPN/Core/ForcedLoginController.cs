using System;
using Avira.Messaging;

namespace Avira.VPN.Core
{
    public class ForcedLoginController
    {
        private IAuthenticator authenticator;

        public event EventHandler<EventArgs<string>> UrlChanged;

        public ForcedLoginController()
            : this(DiContainer.Resolve<IAuthenticator>())
        {
        }

        public ForcedLoginController(IAuthenticator authenticator)
        {
            this.authenticator = authenticator;
            this.authenticator.AccessTokenChanged += delegate
            {
                this.UrlChanged?.Invoke(this, new EventArgs<string>(GetUrl()));
            };
        }

        public string GetUrl()
        {
            if (string.IsNullOrEmpty(authenticator?.AccessToken))
            {
                string text = null;
                if (DiContainer.Resolve<ISettings>()?.Get("ShowFtu", "true") == "true")
                {
                    DiContainer.Resolve<ISettings>()?.Set("ShowFtu", "false");
                    text = DiContainer.Resolve<IUserManagement>()?.FtuUrl;
                }

                if (string.IsNullOrEmpty(text))
                {
                    text = authenticator.GetInAppLoginUrl();
                }

                return text;
            }

            return null;
        }
    }
}