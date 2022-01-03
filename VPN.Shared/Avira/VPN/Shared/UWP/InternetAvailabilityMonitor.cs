using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using Avira.VPN.Core;
using Serilog;

namespace Avira.VPN.Shared.UWP
{
    public class InternetAvailabilityMonitor : IInternetAvailabilityMonitor
    {
        private const string NcsiExpectedResponse = "Microsoft NCSI";

        private const string NameOfDnsMsftncsiCom = "dns.msftncsi.com";

        private readonly IPAddress addressOfDnsMsftncsiCom = IPAddress.Parse("131.107.255.255");

        private readonly Uri ncsiWebRequestUri = new("http://www.msftncsi.com/ncsi.txt");

        public bool IsInternetAvailable { get; set; }

        public event EventHandler InternetConnected;

        public InternetAvailabilityMonitor()
        {
            CheckInternetConectivity().CatchAll();
            NetworkChange.NetworkAddressChanged += NetworkChangeHandler;
        }

        private async Task CheckInternetConectivity()
        {
            bool lastKnownState = IsInternetAvailable;

            try
            {
                IPAddress[] array = await Dns.GetHostAddressesAsync(NameOfDnsMsftncsiCom);
                if (array != null && array.Contains(addressOfDnsMsftncsiCom))
                {
                    IsInternetAvailable = await GetResponse(ncsiWebRequestUri) == NcsiExpectedResponse;
                }
            }
            catch (Exception exception)
            {
                Log.Debug(exception, "Failed to check internet conectivity.");
                IsInternetAvailable = false;
            }

            if (lastKnownState != IsInternetAvailable && IsInternetAvailable)
            {
                this.InternetConnected?.Invoke(this, EventArgs.Empty);
            }
        }

        private async Task<string> GetResponse(Uri requestUri)
        {
            using HttpClient httpClient = new();
            var httpResponseMessage = await httpClient.GetAsync(requestUri);
            if (!httpResponseMessage.IsSuccessStatusCode)
            {
                return string.Empty;
            }

            return await httpResponseMessage.Content.ReadAsStringAsync();
        }

        private void NetworkChangeHandler(object sender, EventArgs e)
        {
            CheckInternetConectivity().CatchAll();
        }
    }
}