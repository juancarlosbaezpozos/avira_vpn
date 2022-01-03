using System;
using System.Linq;
using System.Net;
using System.Threading;

namespace Avira.Common.Core.Networking
{
    public class InternetConnectionMonitor : IInternetConnectionMonitor
    {
        private const string NameOfDnsMsftncsiCom = "dns.msftncsi.com";

        private const string NcsiExpectedResponse = "Microsoft NCSI";

        private readonly IPAddress addressOfDnsMsftncsiCom = IPAddress.Parse("131.107.255.255");

        private readonly Uri ncsiWebRequestUri = new Uri("http://www.msftncsi.com/ncsi.txt");

        private readonly IDnsResolver dnsResolver;

        private readonly IHttpRequestor httpRequestor;

        private readonly INetworkStatusListener networkStatusListener;

        private readonly object statusLock = new object();

        private InternetConnectionStatus lastStatus;

        public InternetConnectionStatus LastKnownStatus
        {
            get
            {
                if (lastStatus == InternetConnectionStatus.Unknown)
                {
                    UpdateCurrentConnectionStatus();
                }
                else
                {
                    InitializeAsync();
                }

                return lastStatus;
            }
        }

        public InternetConnectionStatus CurrentStatus => GetCurrentConnectionState();

        public event EventHandler StatusChanged;

        public InternetConnectionMonitor()
            : this(new DnsResolver(), new HttpRequestor(allowUnsecureConnections: true), new NetworkStatusListener())
        {
        }

        internal InternetConnectionMonitor(IDnsResolver dnsResolver, IHttpRequestor httpRequestor,
            INetworkStatusListener networkStatusListener = null)
        {
            this.dnsResolver = dnsResolver;
            this.httpRequestor = httpRequestor;
            this.networkStatusListener = networkStatusListener;
            if (this.networkStatusListener != null)
            {
                this.networkStatusListener.StatusChanged += NetworkStatusListener_StatusChanged;
            }
        }

        public void InitializeAsync()
        {
            ThreadPool.QueueUserWorkItem(delegate { UpdateCurrentConnectionStatus(); });
        }

        private void NetworkStatusListener_StatusChanged(object sender, EventArgs e)
        {
            UpdateCurrentConnectionStatus();
        }

        private void UpdateCurrentConnectionStatus()
        {
            bool flag = false;
            lock (statusLock)
            {
                InternetConnectionStatus currentConnectionState = GetCurrentConnectionState();
                if (lastStatus != currentConnectionState)
                {
                    lastStatus = currentConnectionState;
                    flag = true;
                }
            }

            if (flag)
            {
                this.StatusChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private InternetConnectionStatus GetCurrentConnectionState()
        {
            IPAddress[] ipAdresses = dnsResolver.GetIpAdresses("dns.msftncsi.com");
            if (ipAdresses != null && ipAdresses.Contains(addressOfDnsMsftncsiCom) &&
                httpRequestor.GetResponse(ncsiWebRequestUri) == "Microsoft NCSI")
            {
                return InternetConnectionStatus.Connected;
            }

            return InternetConnectionStatus.Disconnected;
        }
    }
}