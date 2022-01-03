using System;
using System.Threading.Tasks;
using Avira.Common.Acp.AppClient;
using Avira.VPN.Core;

namespace Avira.VPN.Acp
{
    public class OeBackendPinger : ResourceClient<string>
    {
        public OeBackendPinger()
            : base(DiContainer.Resolve<IAcpCommunicator>(), "backend", "/v2/ping")
        {
        }

        public void Ping(TimeSpan repeatInterval, TimeSpan duration)
        {
            Ping(repeatInterval, DateTime.Now.Add(duration));
        }

        private void Ping(TimeSpan interval, DateTime endingTime)
        {
            Get().CatchAll();
            if (DateTime.Now < endingTime)
            {
                Task.Delay(interval).ContinueWith(delegate { Ping(interval, endingTime); });
            }
        }

        public override string DeserializePayload(string payload)
        {
            return "";
        }
    }
}