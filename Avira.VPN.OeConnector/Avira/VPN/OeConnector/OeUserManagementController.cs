using System;
using System.Linq;
using System.Threading.Tasks;
using Avira.Common.Acp.AppClient;
using Avira.VPN.Acp;
using Avira.VPN.Core;
using Avira.VPN.Core.Win;
using Avira.Win.Messaging;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Avira.VPN.OeConnector
{
    [DiContainer.Export(typeof(IUserManagementController))]
    public class OeUserManagementController : IUserManagementController
    {
        private readonly IResourceClient<UserProfileData> userProfile;

        private UserProfileData lastUserProfileData = new UserProfileData();

        private readonly IResourceClient<JObject> licenseClient;

        public Action RunAfterTrialActivation { get; set; }

        public Action RunAfterUserProfileChanged { get; set; }

        [Routing("userProfile")]
        public UserProfileData UserProfile
        {
            get
            {
                try
                {
                    UserProfileData item = userProfile.Get().Result.Item1;
                    if ((item != null && item.FirstName != null) || (item != null && item.LastName != null))
                    {
                        PollForRegisteredToken().CatchAll();
                    }

                    return item;
                }
                catch (Exception exception)
                {
                    Log.Debug(exception, "Retrieving UserProfile failed.");
                }

                return null;
            }
        }

        [Routing("userProfileChanged", true)] public event EventHandler<EventArgs<UserProfileData>> UserProfileChanged;

        public OeUserManagementController()
            : this(new Avira.Common.Acp.AppClient.UserProfile(DiContainer.Resolve<IAcpCommunicator>()))
        {
        }

        public OeUserManagementController(IResourceClient<UserProfileData> userProfile)
        {
            this.userProfile = userProfile;
            licenseClient =
                new ResourceClient<JObject>(DiContainer.Resolve<IAcpCommunicator>(), "backend", "/v2/licenses");
            licenseClient.Subscribe(delegate { RunAfterUserProfileChanged?.Invoke(); });
            UserProfileChanged += delegate(object sender, EventArgs<UserProfileData> args)
            {
                RunAfterUserProfileChanged?.Invoke();
                if (args.Value?.FirstName != null || args.Value?.LastName != null)
                {
                    PollForRegisteredToken().CatchAll();
                }
            };
            this.userProfile.Subscribe(delegate(UserProfileData d) { InvokeUserProfileChanged(d); });
            AcpCommunicator acpCommunicator = DiContainer.Resolve<IAcpCommunicator>() as AcpCommunicator;
            if (acpCommunicator != null)
            {
                acpCommunicator.Connected += delegate { UpdateUserProfile(); };
                if (acpCommunicator.IsConnected())
                {
                    InvokeUserProfileChanged(UserProfile);
                }
            }
        }

        private void InvokeUserProfileChanged(UserProfileData userProfileData)
        {
            Log.Debug("Got UserProfileChanged. User: " + userProfileData?.FirstName + " " + userProfileData?.LastName);
            if (userProfileData?.FirstName == lastUserProfileData?.FirstName &&
                userProfileData?.LastName == lastUserProfileData?.LastName)
            {
                Log.Debug("UserProfileChanged recievied with the same user name.");
                return;
            }

            lastUserProfileData = userProfileData;
            this.UserProfileChanged?.Invoke(this, new EventArgs<UserProfileData>(userProfileData));
        }

        private void UpdateUserProfile()
        {
            Task.Run(async delegate
            {
                await Task.Delay(1000);
                InvokeUserProfileChanged(UserProfile);
            });
        }

        [Routing("activateTrial")]
        public void ActivateTrial()
        {
            StartTrialFlow().CatchAll();
        }

        private async Task StartTrialFlow()
        {
            PaymentUrlData paymentUrlData = (await new TrialPaymentUrl().Get()).Item1
                ?.Where((PaymentUrlData x) => x.Operation == "upgrade" && x.Target == "web").FirstOrDefault();
            if (paymentUrlData == null)
            {
                Log.Warning("Activate trial: No trial payment url received.");
                return;
            }

            Log.Debug("open browser with trial url.");
            DesktopShell.ShellExecute(paymentUrlData.Url, null, null);
            new OeBackendPinger().Ping(TimeSpan.FromSeconds(35.0), TimeSpan.FromMinutes(10.0));
        }

        private async Task PollForRegisteredToken()
        {
            IAuthenticator authenticator = DiContainer.Resolve<IAuthenticator>();
            if (authenticator is OeAuthenticator)
            {
                await ((OeAuthenticator)authenticator).PollForRegisteredUser();
            }
        }
    }
}