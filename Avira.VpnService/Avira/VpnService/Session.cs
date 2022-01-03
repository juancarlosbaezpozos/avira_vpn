using System;
using System.Text.RegularExpressions;
using System.Threading;
using Avira.VPN.Core.Win;
using Avira.VpnService.Properties;
using Avira.Win.Messaging;
using Serilog;

namespace Avira.VpnService
{
    public class Session : ISession
    {
        private IOpenVpnClient client;

        private ConnectionState state;

        private bool stopWasRequested;

        private INotificationSource source;

        public ulong TrafficUsedTotal { get; private set; }

        public string TlsLog { get; private set; }

        public ConnectionState State
        {
            get { return state; }
            internal set
            {
                if (state != value)
                {
                    state = value;
                    PropagateState();
                }
            }
        }

        public ErrorType LastErrorType { get; private set; }

        public string LastError { get; private set; }

        public Credentials Credentials { get; set; }

        public event EventHandler<Status> StateChanged;

        public event EventHandler<TrafficChangedEventArgs> TrafficChanged;

        public Session()
        {
            LastError = string.Empty;
            LastErrorType = ErrorType.NoError;
            Credentials = new Credentials
            {
                UserId = () => GeneratedDeviceInfo.GetClientId(),
                Password = () => GeneratedDeviceInfo.GetDeviceId()
            };
        }

        public void Bind(IOpenVpnExe process, INotificationSource notifications, IOpenVpnClient client)
        {
            if (State != ConnectionState.Connecting)
            {
                throw new Exception("Session wasn't started yet.");
            }

            process.Exited += OnOpenVpnExit;
            process.Output += HandleOutputLogNotification;
            this.client = client;
            notifications.StateReceived += HandleStateNotification;
            notifications.HoldReceived += HandleHoldNotification;
            notifications.AuthReceived += HandleAuthNotification;
            notifications.FatalReceived += HandleFatalNotification;
            notifications.ByteCountReceived += HandleByteCountNotification;
            notifications.LogReceived += HandleLogNotification;
            notifications.ReadyReceived += HandleReadyNotification;
            source = notifications;
        }

        public void Start()
        {
            stopWasRequested = false;
            if (State == ConnectionState.Disconnected)
            {
                LastError = string.Empty;
                LastErrorType = ErrorType.NoError;
                State = ConnectionState.Connecting;
            }
        }

        public void Stop()
        {
            if (State == ConnectionState.Connected)
            {
                State = ConnectionState.Disconnecting;
                stopWasRequested = true;
            }
        }

        private static ErrorType ParseErrors(string reason)
        {
            if (new Regex("^TCP: connect to (.*) failed").IsMatch(reason))
            {
                return ErrorType.NetworkError;
            }

            return ErrorType.GeneralError;
        }

        private static ErrorType ParseSigterm(string reason)
        {
            string pattern = "SIGTERM\\[(?<type>.*),(?<reason>.*)\\]";
            foreach (Match item in Regex.Matches(reason, pattern, RegexOptions.IgnoreCase))
            {
                Log.Information(string.Format("SIGTERM detected : type={0}, reason={1}", item.Groups["type"],
                    item.Groups["reason"]));
                if (item.Groups["type"].Value == "soft")
                {
                    switch (item.Groups["reason"].Value)
                    {
                        case "ping-restart":
                        case "ping-reset":
                            return ErrorType.PingReset;
                        case "connection-reset":
                            return ErrorType.ServerError;
                        case "decryption-error":
                            return ErrorType.DecryptionError;
                        case "tls-error":
                            return ErrorType.TlsError;
                        case "auth-failure":
                            return ErrorType.AuthError;
                    }
                }
            }

            return ErrorType.NoError;
        }

        public void Disconnect()
        {
            LastErrorType = ErrorType.NoError;
            LastError = string.Empty;
            State = ConnectionState.Disconnected;
        }

        private void OnOpenVpnExit(object sender, EventArgs args)
        {
            Log.Debug("OpenVpn process exited");
            source.SourceClosed.WaitOne();
            if (!stopWasRequested)
            {
                Log.Debug("Stop was not requested, change state to disconnected");
                State = ConnectionState.Disconnected;
            }
        }

        private void HandleReadyNotification(object sender, EventArgs e)
        {
            Thread.Sleep(100);
            ConfigureNotifications();
            client.SetVerbosityLevel(Settings.Default.OpenVpnVerbosity);
        }

        private void HandleByteCountNotification(object sender, ByteCountNotification e)
        {
            if (State == ConnectionState.Connected)
            {
                TrafficUsedTotal = e.Ingoing + e.Outgoing;
                this.TrafficChanged?.Invoke(this, new TrafficChangedEventArgs
                {
                    UsedInBytes = TrafficUsedTotal
                });
            }
        }

        private void HandleHoldNotification(object sender, HoldNotification e)
        {
            if (e.Reason.StartsWith("Waiting for hold release"))
            {
                client.Release();
            }
        }

        private void HandleLogNotification(object sender, LogNotification e)
        {
            if (e.Reason.IndexOf("tls", StringComparison.InvariantCultureIgnoreCase) != -1)
            {
                TlsLog = TlsLog + "\n" + e.Reason;
            }

            switch (e.Type)
            {
                case LogNotification.LogType.Error:
                    if (!e.Reason.StartsWith("RESOLVE: Cannot resolve host address:"))
                    {
                        Log.Warning(e.Reason);
                        LastErrorType = ParseErrors(e.Reason);
                        LastError = e.Reason;
                    }

                    break;
                case LogNotification.LogType.Fatal:
                    Log.Error(e.Reason);
                    break;
                case LogNotification.LogType.Warning:
                    Log.Warning(e.Reason);
                    break;
                default:
                    HandleSigterm(e.Reason);
                    break;
            }
        }

        private void HandleOutputLogNotification(object sender, EventArgs<string> log)
        {
            HandleSigterm(log.Value);
        }

        private void HandleSigterm(string data)
        {
            ErrorType errorType = ParseSigterm(data);
            if (errorType != 0)
            {
                LastErrorType = errorType;
            }

            Log.Information(data);
        }

        private void HandleStateNotification(object sender, StateNotification e)
        {
            switch (e.StateType)
            {
                case StateNotification.Type.Connected:
                    if (e.Reason == "ERROR")
                    {
                        State = ConnectionState.Disconnected;
                        LastErrorType = ErrorType.ConnectedError;
                    }
                    else
                    {
                        State = ConnectionState.Connected;
                    }

                    break;
                case StateNotification.Type.Reconnecting:
                    if (State != ConnectionState.Connected)
                    {
                        throw new Exception("Reconnecting in Disconnected state!");
                    }

                    State = ConnectionState.Connecting;
                    break;
                case StateNotification.Type.Exiting:
                    if (State == ConnectionState.Connected)
                    {
                        State = ConnectionState.Disconnecting;
                    }

                    break;
            }
        }

        public void PropagateState()
        {
            Log.Debug($"[!] Session: {State.ToString()}, {LastErrorType}, {LastError}\n");
            this.StateChanged?.Invoke(null, ToStatus());
        }

        public Status ToStatus()
        {
            return new Status(State, LastErrorType, LastError);
        }

        private void ConfigureNotifications()
        {
            client.EnableStateNotification();
            client.EnableByteCountNotification(1);
            client.EnableLogging();
        }

        private void HandleAuthNotification(object sender, AuthNotification e)
        {
            switch (e.Type)
            {
                case AuthNotification.AuthTypes.Auth:
                    client.Auth(Credentials.UserId(), Credentials.Password());
                    break;
                case AuthNotification.AuthTypes.Failed:
                    State = ConnectionState.Disconnected;
                    LastErrorType = ErrorType.GeneralError;
                    LastError = e.Reason;
                    break;
            }
        }

        private void HandleFatalNotification(object sender, FatalNotification e)
        {
            try
            {
                LastErrorType = ErrorType.Fatal;
                LastError = e.Reason;
                if (!e.Reason.StartsWith("CreateFile failed on TAP device"))
                {
                    if (e.Reason.StartsWith("RESOLVE: Cannot resolve host address:"))
                    {
                        LastErrorType = ErrorType.DnsError;
                        throw new Exception("Cannot resolve host address : " + e.Reason);
                    }

                    throw new Exception(e.Reason);
                }

                Log.Warning("CreateFile failed on TAP device.");
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Fatal connection error.");
            }

            State = ConnectionState.Disconnected;
        }
    }
}