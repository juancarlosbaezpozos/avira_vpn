using System;
using System.Collections.Generic;
using Avira.VPN.Core;
using Avira.VPN.Core.Win;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Avira.VpnService
{
    public class StatusProvider : IOeStatusProvider
    {
        private const string BucketControlGroupSuffix = "_cg";

        private const string BucketTestGroupSuffix = "_tg";

        private readonly TimeSpan oeInusePeriod;

        public JObject HeartbeatCustomData
        {
            get
            {
                KnownWifis knownWiFis = new ServicePersistentData().KnownWiFis;
                Ratings ratings = DiContainer.Resolve<Ratings>();
                return new JObject
                {
                    ["version"] = (JToken)ProductSettings.ProductVersion,
                    ["state"] = (JToken)(IsAppInUse() ? "inuse" : "active"),
                    ["license_type"] = "paid",
                    ["remaining_traffic_MB"] = (JToken)GetRemainingTrafficMB().ToString(),
                    ["license_expiration"] = (JToken)FormatDate(DiContainer.GetValue<DateTime>("LicenseExpiration")),
                    ["connected_wifis"] = new JObject
                    {
                        {
                            "1d",
                            (JToken)(knownWiFis?.GetConnectedWifis(1) ?? 0)
                        },
                        {
                            "2d",
                            (JToken)(knownWiFis?.GetConnectedWifis(2) ?? 0)
                        },
                        {
                            "7d",
                            (JToken)(knownWiFis?.GetConnectedWifis(7) ?? 0)
                        },
                        {
                            "30d",
                            (JToken)(knownWiFis?.GetConnectedWifis(30) ?? 0)
                        }
                    },
                    ["sar"] = (JToken)(ratings?.SecurityAfinityRating),
                    ["dar"] = (JToken)(ratings?.DownloadAfinityRating),
                    ["experiments"] = (JToken)GetActiveExperiments()
                };
            }
        }

        public JObject EventsCustomData => new JObject
        {
            ["version"] = (JToken)ProductSettings.ProductVersion,
            ["license_type"] = "paid"
        };

        public StatusProvider(TimeSpan OeInusePeriod)
        {
            oeInusePeriod = OeInusePeriod;
        }

        private bool IsAppInUse()
        {
            if (!WasConnectFeatureUsed())
            {
                return WasAppOpened();
            }

            return true;
        }

        internal static long GetRemainingTrafficMB()
        {
            ulong value = DiContainer.GetValue<ulong>("TrafficLimit");
            ulong value2 = DiContainer.GetValue<ulong>("TrafficUsed");
            if (value != 0L)
            {
                if (value < value2)
                {
                    return (long)(0L - (value2 - value) / 1048576uL);
                }

                return (long)((value - value2) / 1048576uL);
            }

            return 2147483647L;
        }

        private string FormatDate(DateTime date)
        {
            if (date.Year < 1900)
            {
                return string.Empty;
            }

            return date.ToString("s") + "Z";
        }

        private string GetActiveExperiments()
        {
            List<JObject> experiments = new List<JObject>();
            DiContainer.Resolve<IRemoteConfiguration>()?.Buckets?.ForEach(delegate(string b)
            {
                if (b.EndsWith("_cg") && b.Length > "_cg".Length)
                {
                    experiments.Add(new JObject
                    {
                        ["id"] = (JToken)b.Substring(0, b.Length - "_cg".Length),
                        ["group"] = (JToken)"control"
                    });
                }
                else if (b.EndsWith("_tg") && b.Length > "_tg".Length)
                {
                    experiments.Add(new JObject
                    {
                        ["id"] = (JToken)b.Substring(0, b.Length - "_tg".Length),
                        ["group"] = (JToken)"test"
                    });
                }
            });
            return JsonConvert.SerializeObject(experiments);
        }

        public void UpdateLastGuiOpened(DateTime now)
        {
            throw new NotImplementedException();
        }

        public bool WasConnectFeatureUsed()
        {
            IOpenVpn openVpn = DiContainer.Resolve<IOpenVpn>();
            if (openVpn == null || openVpn.ConnectionState != ConnectionState.Connected)
            {
                return DateTime.Now.ToUniversalTime().Subtract(ProductSettings.LastConnect) <= oeInusePeriod;
            }

            return true;
        }

        public bool WasAppOpened()
        {
            return DateTime.Now.Subtract(ProductSettings.LastGuiOpened) <= oeInusePeriod;
        }
    }
}