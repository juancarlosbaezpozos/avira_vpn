using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.ServiceModel;
using System.Threading;
using System.Threading.Tasks;
using Avira.Common.Core;
using Avira.VPN.Core;
using Avira.VPN.Core.Win;
using Serilog;

namespace Avira.VpnService
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public sealed class OpenVpn : IDisposable, IOpenVpn
    {
        private const string ManagementServer = "127.0.0.1";

        private readonly object sessionWriteLock = new object();

        private readonly TapAdapter tapAdapter = new TapAdapter(new PathWhiteList());

        private ManagementEndpoint endpoint;

        private NotificationSource notifications;

        private OpenVpnClient client;

        private Session session = new Session();

        private OpenVpnExe openVpnExe;

        private RemoteConnectionSettings latestSelectedRegion;

        private ulong tapAdapterBaseTraffic;

        private ulong sessionBaseTraffic;

        private uint reconnectAttempts;

        private bool isTriggeredByAutoconnect;

        public Credentials Credentials { get; set; }

        public ConnectionState ConnectionState { get; private set; }

        public ulong SessionTotalTraffic => tapAdapter.Used - tapAdapterBaseTraffic;

        public Status Status => session.ToStatus();

        public event EventHandler<Status> StateChangedNotification;

        public event EventHandler<TrafficChangedEventArgs> TrafficChanged;

        public void Connect(RemoteConnectionSettings selectedRegion, bool isTriggeredByAutoconnect = false)
        {
            lock (sessionWriteLock)
            {
                if (session.State != 0 || !CheckForNetworkConnection())
                {
                    return;
                }

                try
                {
                    StartSession();
                    TrackConnectEvent(selectedRegion, isTriggeredByAutoconnect);
                    CleanOpenVpnAbandonedProcesses();
                    PrepareTapAdapterForConnecting();
                    if (ProductSettings.ExtraLogging)
                    {
                        LogAdaptersConfiguration();
                    }

                    int port = FindAvailablePort();
                    openVpnExe = new OpenVpnExe("127.0.0.1", port);
                    openVpnExe.Start(selectedRegion, tapAdapter.Name);
                    latestSelectedRegion = selectedRegion;
                    this.isTriggeredByAutoconnect = isTriggeredByAutoconnect;
                    AttachSessionToOpenVpn(port);
                }
                catch (Exception exception)
                {
                    Serilog.Log.Error(exception, "Connect failed.");
                    StopOpenVpn();
                    session?.Disconnect();
                    HandleSpecificExceptions(exception);
                }
            }
        }

        private int FindAvailablePort()
        {
            return Enumerable.Range(23000, 25000).First((int p) =>
                IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().All((IPEndPoint l) => l.Port != p));
        }

        private void LogAdaptersConfiguration()
        {
            ProcessRunner processRunner = new ProcessRunner(new ProcessWrapper(), new PathWhiteList());
            processRunner.FileName = FileSystem.MakeFullPath(ProductSettings.OpenVpnPath);
            processRunner.Arguments = "--show-adapters";
            processRunner.WorkingDirectory = FileSystem.MakeFullPath("OpenVpn");
            processRunner.OutputDataReceived += delegate(object s, DataReceivedEventArgs e)
            {
                Serilog.Log.Debug(e.Data);
            };
            processRunner.StartAndWaitForExit();
        }

        private void TrackConnectEvent(RemoteConnectionSettings userSelectedRegion, bool isTriggeredByAutoconnect)
        {
            int num = userSelectedRegion.Latency;
            if (num <= 0)
            {
                num = RegionsLatency.GetPing(userSelectedRegion.Uri);
            }

            Dictionary<string, string> dictionary =
                CreateConnectTrackingParameters(userSelectedRegion, isTriggeredByAutoconnect);
            dictionary.Add("Latency", num.ToString());
            Tracker.TrackEvent(Tracker.Events.Connect, dictionary);
        }

        private void TrackConnectedEvent()
        {
            Dictionary<string, string> properties =
                CreateConnectTrackingParameters(latestSelectedRegion, isTriggeredByAutoconnect);
            Tracker.TrackEvent(Tracker.Events.Connected, properties);
        }

        private Dictionary<string, string> CreateConnectTrackingParameters(RemoteConnectionSettings userSelectedRegion,
            bool isTriggeredByAutoconnect)
        {
            return new Dictionary<string, string>
            {
                { "Region", userSelectedRegion.Id },
                { "Uri", userSelectedRegion.Uri },
                {
                    "Port",
                    userSelectedRegion.Port.ToString()
                },
                { "Protocol", userSelectedRegion.Protocol },
                {
                    "Autoconnect",
                    isTriggeredByAutoconnect.ToString()
                },
                { "Trigger Source", userSelectedRegion.TriggerSource }
            };
        }

        private void CleanOpenVpnAbandonedProcesses()
        {
            new AbandonedProcess(FileSystem.MakeFullPath(ProductSettings.OpenVpnPath)).CleanRunningInstances();
        }

        private void HandleSpecificExceptions(Exception exception)
        {
            VpnException ex = exception as VpnException;
            if (ex != null)
            {
                NotifyError(ex.ErrorType);
            }
        }

        private void PrepareTapAdapterForConnecting()
        {
            PathWhiteList whitelist = new PathWhiteList();
            TapDriver tapDriver = new TapDriver(new ProcessWrapper(), whitelist, DiContainer.Resolve<ISettings>());
            Serilog.Log.Information("Checking if tap driver is installed...");
            tapDriver.LogStatus();
            if (tapDriver.IsUpdateRequired())
            {
                tapDriver.Uninstall();
                tapAdapter.ResetInterface();
                tapAdapterBaseTraffic = tapAdapter.Used;
            }

            if (!tapDriver.IsInstalled())
            {
                InstallTapDriver(tapDriver);
            }
            else if (!tapDriver.IsRunning())
            {
                tapAdapter.Enable();
            }

            try
            {
                tapAdapter.PrepareConfiguration();
            }
            catch (Exception exception)
            {
                Serilog.Log.Warning(exception, "Failed to prepare Tap driver configuration");
            }
        }

        private void InstallTapDriver(TapDriver tapDriver)
        {
            try
            {
                tapDriver.Install();
            }
            catch (VpnException ex) when (ex.ErrorType == ErrorType.TapAdapterRestartRequired)
            {
                throw;
            }
            catch (Exception exception)
            {
                Serilog.Log.Error(exception, "Failed to install the Tap driver.");
                tapDriver.Uninstall();
                throw;
            }

            Serilog.Log.Information("Tap driver installed successfuly.");
        }

        private bool CheckForNetworkConnection()
        {
            Serilog.Log.Debug("Checking for network connection availability...");
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                return true;
            }

            Serilog.Log.Information("No network available.");
            NotifyError(ErrorType.NetworkError);
            return false;
        }

        private void NotifyError(ErrorType errorType)
        {
            Status e = new Status
            {
                Error = errorType
            };
            ConnectionState = ConnectionState.Disconnected;
            this.StateChangedNotification?.Invoke(this, e);
        }

        private void StartSession()
        {
            session = new Session
            {
                Credentials = Credentials
            };
            session.StateChanged += OnStateChangedNotification;
            session.TrafficChanged += OnTrafficChanged;
            sessionBaseTraffic = 0uL;
            tapAdapterBaseTraffic = tapAdapter.Used;
            session.Start();
        }

        private void ConnectToManagementInterface(int port, int retry = 0)
        {
            try
            {
                Serilog.Log.Debug("Trying to attach session to OpenVpn: Number of retries: " + retry);
                endpoint = new ManagementEndpoint("127.0.0.1", port);
                Serilog.Log.Debug(string.Format("Created ManagementEndpoint for {0} and port {1}", "127.0.0.1", port));
            }
            catch (Exception ex)
            {
                Serilog.Log.Warning("Error connecting to management interface: " + ex.ToString());
                if (retry > 1)
                {
                    throw;
                }

                Thread.Sleep(5000);
                ConnectToManagementInterface(port, ++retry);
            }
        }

        private void AttachSessionToOpenVpn(int port)
        {
            ConnectToManagementInterface(port);
            notifications = new NotificationSource(endpoint);
            client = new OpenVpnClient(endpoint);
            session.Bind(openVpnExe, notifications, client);
            endpoint.ErrorReceived += delegate(object source, ManagementMessage message)
            {
                Serilog.Log.Error("OpenVpn error received.");
                Serilog.Log.Warning(message.Data);
                Disconnect();
                NotifyError(ErrorType.GeneralError);
            };
            endpoint.Start();
        }

        public void Disconnect()
        {
            lock (sessionWriteLock)
            {
                if (session.State != 0)
                {
                    TrackDisconnectEvent();
                    session.Stop();
                    StopOpenVpn();
                    session.Disconnect();
                }
            }
        }

        public void ResetSessionTraffic()
        {
            sessionBaseTraffic = tapAdapter.Used - tapAdapterBaseTraffic;
        }

        private void TrackDisconnectEvent()
        {
            if (session.State == ConnectionState.Connected)
            {
                Tracker.TrackEvent(Tracker.Events.Disconnect);
            }
        }

        private void OnStateChangedNotification(object sender, Status eventArgs)
        {
            Serilog.Log.Debug($"[!] OpenVpn: {eventArgs.NewState}, {eventArgs.Error}, {eventArgs.Message}\n");
            if (eventArgs.NewState == ConnectionState.Connected)
            {
                tapAdapter.ChooseMetric();
                TrackConnectedEvent();
            }

            bool num = TryReconnecting(ref eventArgs);
            if (eventArgs.NewState == ConnectionState.Disconnecting)
            {
                eventArgs.Error = ErrorType.NoError;
            }

            ConnectionState = eventArgs.NewState;
            this.StateChangedNotification?.Invoke(sender, eventArgs);
            if (num)
            {
                Reconnect();
                return;
            }

            if (eventArgs.NewState == ConnectionState.Connected)
            {
                reconnectAttempts = 0u;
            }

            try
            {
                TrackConnectionError(eventArgs);
                if (eventArgs.NewState == ConnectionState.Disconnected && eventArgs.Error == ErrorType.ConnectedError)
                {
                    StopOpenVpn();
                }

                if (eventArgs.NewState == ConnectionState.Connected ||
                    eventArgs.NewState == ConnectionState.Disconnected)
                {
                    Task.Run(delegate { DiContainer.Resolve<Traffic>().Refresh(); });
                }
            }
            catch (Exception)
            {
            }
        }

        private void TrackReconnectEvent(string cause, string message)
        {
            Tracker.TrackEvent(Tracker.Events.Reconnect, new Dictionary<string, string>
            {
                { "Cause", cause },
                { "Message", message }
            });
        }

        private bool TryReconnecting(ref Status eventArgs)
        {
            if (eventArgs.NewState != 0)
            {
                return false;
            }

            if (isTriggeredByAutoconnect && !ProductSettings.KillSwitchUserSetting)
            {
                Serilog.Log.Debug("Supressing reconnection because connection was wifi and killswitch is disabled");
                return false;
            }

            if (TryReconnectDueToOpenVpnError(eventArgs.Error))
            {
                TrackReconnectEvent("Error", eventArgs.Message);
                if (eventArgs.Error == ErrorType.AuthError)
                {
                }

                eventArgs.NewState = ConnectionState.Connecting;
                eventArgs.Error = ErrorType.NoError;
                reconnectAttempts++;
                return true;
            }

            if (!RetryWithFallbackProtocol(eventArgs.Error))
            {
                return false;
            }

            TrackReconnectEvent("ProtocolBlocked",
                "Protocol: " + latestSelectedRegion.Protocol + " Port: " + latestSelectedRegion.Port);
            eventArgs.Error = ErrorType.UdpErrorReconnecting;
            return true;
        }

        private bool RetryWithFallbackProtocol(ErrorType error)
        {
            if (error != ErrorType.GeneralError || !string.Equals(latestSelectedRegion.Protocol, "udp") ||
                latestSelectedRegion.FallbackProtocol == null)
            {
                return false;
            }

            Serilog.Log.Information("Retrying with Fallback protocol (" + latestSelectedRegion.FallbackProtocol + ").");
            latestSelectedRegion.Port = latestSelectedRegion.FallbackPort;
            latestSelectedRegion.Protocol = latestSelectedRegion.FallbackProtocol;
            latestSelectedRegion.TlsHadshakeWindow = 60;
            return true;
        }

        private void Reconnect()
        {
            Serilog.Log.Information($"Trying to reconnect. Attempt {reconnectAttempts}.");
            reconnectAttempts++;
            ThreadPool.QueueUserWorkItem(delegate { Connect(latestSelectedRegion); });
        }

        private bool TryReconnectDueToOpenVpnError(ErrorType error)
        {
            if ((error == ErrorType.PingReset || error == ErrorType.DecryptionError || error == ErrorType.ServerError ||
                 error == ErrorType.AuthError || error == ErrorType.TlsError) && reconnectAttempts < 1)
            {
                return latestSelectedRegion != null;
            }

            return false;
        }

        private void TrackConnectionError(Status vpnStatus)
        {
            if (vpnStatus.NewState != 0 || vpnStatus.Error == ErrorType.NoError)
            {
                return;
            }

            Tracker.TrackEvent(Tracker.Events.ConnectionError, new Dictionary<string, string>
            {
                { "Message", vpnStatus.Message },
                {
                    "Error",
                    vpnStatus.Error.ToString()
                },
                { "Protocol", latestSelectedRegion.Protocol },
                {
                    "Port",
                    latestSelectedRegion.Port.ToString()
                },
                { "Uri", latestSelectedRegion.Uri }
            });
            try
            {
                if (vpnStatus.Message.StartsWith("NETSH"))
                {
                    throw new Exception("NETSH ERROR:\noutput\n" + openVpnExe?.ChildLog);
                }

                if (vpnStatus.Error == ErrorType.TlsError)
                {
                    throw new Exception("TLS ERROR:\nsession\n" + session?.TlsLog + "\noutput\n" + openVpnExe?.TlsLog);
                }
            }
            catch (Exception exception)
            {
                Serilog.Log.Error(exception, "Connect failed.");
            }
        }

        private void StopOpenVpn()
        {
            openVpnExe?.Stop();
            openVpnExe = null;
            tapAdapter?.RestoreMetric();
        }

        private void OnTrafficChanged(object sender, TrafficChangedEventArgs currentSessionTrafficEventArgs)
        {
            ulong num = tapAdapter.Used - tapAdapterBaseTraffic;
            if (num >= sessionBaseTraffic)
            {
                currentSessionTrafficEventArgs.UsedInBytes = num - sessionBaseTraffic;
                this.TrafficChanged?.Invoke(this, currentSessionTrafficEventArgs);
            }
        }

        public void Dispose()
        {
            if (endpoint != null)
            {
                endpoint.Dispose();
            }

            if (openVpnExe != null)
            {
                openVpnExe.Dispose();
            }
        }
    }
}