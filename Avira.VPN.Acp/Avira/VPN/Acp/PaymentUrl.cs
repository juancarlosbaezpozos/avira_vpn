using System.Collections.Generic;
using Avira.Common.Acp.AppClient;
using Avira.VPN.Core;
using Avira.VPN.Core.Win;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Avira.VPN.Acp
{
    public class PaymentUrl : ResourceClient<List<PaymentUrlData>>
    {
        public PaymentUrl(string filterParams = "")
            : base(DiContainer.Resolve<IAcpCommunicator>(), "backend",
                "/v2/payment-urls?filter[app.service]=vpn&filter[scope]=app&filter[language]=" + GetLanguageCode() +
                filterParams)
        {
        }

        private static string GetLanguageCode()
        {
            return ProductSettings.ProductLanguage.Split('-')[0];
        }

        public override List<PaymentUrlData> DeserializePayload(string payload)
        {
            List<PaymentUrlData> list = new List<PaymentUrlData>();
            JsonConvert.DeserializeObject<JObject>(payload)!.TryGetValue("data", out var value);
            JArray jArray;
            if ((jArray = value as JArray) != null)
            {
                foreach (JToken item in jArray)
                {
                    PaymentUrlData paymentUrlData =
                        ((item as JObject).GetValue("attributes") as JObject)?.ToObject<PaymentUrlData>();
                    if (paymentUrlData != null)
                    {
                        list.Add(paymentUrlData);
                    }
                }

                return list;
            }

            return list;
        }
    }
}