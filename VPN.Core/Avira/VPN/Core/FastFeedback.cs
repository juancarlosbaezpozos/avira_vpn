using System;
using System.Collections.Generic;
using Avira.Messaging;
using Avira.VPN.Shared.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Avira.VPN.Core
{
    public class FastFeedback
    {
        private const ulong MinimumTrafficConsumption = 52428800uL;

        private const uint MinimumTimeBetweenNotifications = 1u;

        private const string FastFeedbackDisplayed = "fast_feedback_displayed";

        private const string FastFeedbackId = "fast_feedback_id";

        private ulong trafficWhenUserConnected;

        private string languageId;

        private FastFeedbackData fastFeedbackData;

        private Func<bool> isFastFeedbackEnabled;

        public string FeedbackId => fastFeedbackData?.Id;

        public string DisconnectSource { get; set; }

        [Routing("displayFastFeedback")] public event EventHandler DisplayFastFeedbackDialog;

        public FastFeedback(Func<bool> isFastFeedbackEnabled)
            : this(DiContainer.Resolve<IProductSettings>()?.ProductLanguage, isFastFeedbackEnabled)
        {
        }

        public FastFeedback(string languageId, Func<bool> isFastFeedbackEnabled)
        {
            this.languageId = languageId;
            this.isFastFeedbackEnabled = isFastFeedbackEnabled;
            ITraffic traffic = DiContainer.Resolve<ITraffic>();
            if (traffic != null)
            {
                trafficWhenUserConnected = traffic.TrafficData.UsedTraffic;
            }

            IVpnConnector vpnConnector = DiContainer.Resolve<IVpnConnector>();
            if (vpnConnector != null)
            {
                vpnConnector.StatusChanged += VpnStatusChangedHandler;
            }
        }

        [Routing("sendFastFeedback")]
        public void SendFastFeedback(int rating)
        {
            Tracker.TrackEvent(Tracker.Events.UserFeedback, new Dictionary<string, string>
            {
                { "Feedback Id", fastFeedbackData.Id },
                {
                    "Rating",
                    rating.ToString()
                },
                {
                    "Host",
                    DiContainer.Resolve<IVpnController>()?.LastUsedRegion.Host
                },
                {
                    "Region Id",
                    DiContainer.Resolve<IVpnController>()?.LastUsedRegion.Id
                }
            });
        }

        [Routing("notNowFastFeedback")]
        public void NotNowFastFeedback()
        {
            Tracker.TrackEvent(Tracker.Events.UserFeedbackDismissed,
                new Dictionary<string, string> { { "Feedback Id", fastFeedbackData.Id } });
        }

        [Routing("fastFeedbackStrings")]
        public JObject FastFeedbackStrings()
        {
            if (fastFeedbackData == null || fastFeedbackData.Content == null)
            {
                return new JObject();
            }

            FastFeedbackContentStrings fastFeedbackContentStrings = ((!fastFeedbackData.Content.ContainsKey(languageId))
                ? fastFeedbackData.Content["en-US"]!.ToObject<FastFeedbackContentStrings>()
                : fastFeedbackData.Content[languageId]!.ToObject<FastFeedbackContentStrings>());
            if (fastFeedbackContentStrings == null)
            {
                return new JObject();
            }

            FastFeedbackRatingStrings fastFeedbackRatingStrings =
                fastFeedbackContentStrings.Ratings?.ToObject<FastFeedbackRatingStrings>();
            if (fastFeedbackRatingStrings == null)
            {
                return new JObject();
            }

            return new JObject
            {
                ["title"] = (JToken)fastFeedbackContentStrings.Title,
                ["description"] = (JToken)fastFeedbackContentStrings.Subject,
                ["button_submit"] = (JToken)fastFeedbackContentStrings.ButtonSubmit,
                ["button_cancel"] = (JToken)fastFeedbackContentStrings.ButtonCancel,
                ["ratings"] = new JObject
                {
                    ["one"] = (JToken)fastFeedbackRatingStrings.One,
                    ["two"] = (JToken)fastFeedbackRatingStrings.Two,
                    ["three"] = (JToken)fastFeedbackRatingStrings.Three,
                    ["four"] = (JToken)fastFeedbackRatingStrings.Four,
                    ["five"] = (JToken)fastFeedbackRatingStrings.Five
                }
            };
        }

        public void VpnStatusChangedHandler(object sender, EventArgs e)
        {
            IVpnConnector vpnConnector = DiContainer.Resolve<IVpnConnector>();
            if (vpnConnector != null)
            {
                switch ((!(e is StatusEventArgs)) ? vpnConnector.Status : (e as StatusEventArgs).Status)
                {
                    case VpnStatus.Connected:
                        trafficWhenUserConnected = DiContainer.Resolve<ITraffic>().TrafficData.UsedTraffic;
                        break;
                    case VpnStatus.Disconnected:
                        ShowDialogIfNeeded();
                        break;
                }
            }
        }

        private bool ShouldShowDialog()
        {
            if (!isFastFeedbackEnabled() || DisconnectSource != "GuiDisconnectButton")
            {
                return false;
            }

            DateTime dateTime = JsonConvert.DeserializeObject<DateTime>(DiContainer.Resolve<ISettings>()
                .Get("last_feedback_shown", JsonConvert.SerializeObject(new DateTime(2000, 1, 1))));
            if (DateTime.Now < dateTime.AddDays(1.0))
            {
                return false;
            }

            return !bool.Parse(DiContainer.Resolve<ISettings>().Get("fast_feedback_displayed", "False"));
        }

        private void ShowDialogIfNeeded()
        {
            if (ShouldShowDialog())
            {
                ulong usedTraffic = DiContainer.Resolve<ITraffic>().TrafficData.UsedTraffic;
                if (trafficWhenUserConnected + 52428800 < usedTraffic)
                {
                    Log.Information("Showing FastFeedback dialog...");
                    this.DisplayFastFeedbackDialog?.Invoke(this, EventArgs.Empty);
                    Tracker.TrackEvent(Tracker.Events.UserFeedbackShown,
                        new Dictionary<string, string> { { "Feedback Id", fastFeedbackData.Id } });
                    DiContainer.Resolve<ISettings>().Set("fast_feedback_displayed", true.ToString());
                    DiContainer.Resolve<ISettings>()
                        .Set("last_feedback_shown", JsonConvert.SerializeObject(DateTime.Now));
                }
            }
        }

        public void Update(FastFeedbackData fastFeedbackData)
        {
            this.fastFeedbackData = fastFeedbackData;
            string text = DiContainer.Resolve<ISettings>().Get("fast_feedback_id", string.Empty);
            if (string.IsNullOrEmpty(text) || text != fastFeedbackData.Id)
            {
                DiContainer.Resolve<ISettings>().Set("fast_feedback_id", fastFeedbackData?.Id ?? string.Empty);
                DiContainer.Resolve<ISettings>().Set("fast_feedback_displayed", false.ToString());
            }
        }
    }
}