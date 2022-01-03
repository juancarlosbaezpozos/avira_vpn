using System;
using System.Threading;
using Serilog;

namespace Avira.VpnService
{
    public class NotificationSource : INotificationSource
    {
        public EventWaitHandle SourceClosed { get; set; }

        public event EventHandler<FatalNotification> FatalReceived;

        public event EventHandler<StateNotification> StateReceived;

        public event EventHandler<HoldNotification> HoldReceived;

        public event EventHandler<AuthNotification> AuthReceived;

        public event EventHandler<ByteCountNotification> ByteCountReceived;

        public event EventHandler<LogNotification> LogReceived;

        public event EventHandler ReadyReceived;

        public NotificationSource(IManagementEndpoint source)
        {
            source.MessageReceived += OnMessageReceived;
            source.StreamClosed += OnStreamClosed;
            SourceClosed = new ManualResetEvent(initialState: false);
        }

        private static void Parse(string buffer, out string body, out string type)
        {
            if (!buffer.StartsWith(">"))
            {
                type = "UNKNOWN";
                body = buffer;
                return;
            }

            int num = buffer.IndexOf(':');
            if (num == -1)
            {
                throw new Exception("[error] Message is corrupted : " + buffer);
            }

            body = buffer.Substring(num + 1);
            type = buffer.Substring(1, num - 1);
        }

        private void OnMessageReceived(object sender, ManagementMessage msg)
        {
            try
            {
                FireNotification(msg.Data);
            }
            catch (Exception exception)
            {
                Log.Warning(exception, "Unable to fire notification.");
            }
        }

        private void OnStreamClosed(object sender, EventArgs msg)
        {
            Log.Information("OpenVpn: Stream Closed.");
            SourceClosed.Set();
        }

        private void FireNotification(string buffer)
        {
            Parse(buffer, out var body, out var type);
            switch (type)
            {
                case "FATAL":
                    this.FatalReceived?.Invoke(this, new FatalNotification(body));
                    break;
                case "STATE":
                    this.StateReceived?.Invoke(this, new StateNotification(body));
                    break;
                case "PASSWORD":
                    this.AuthReceived?.Invoke(this, new AuthNotification(body));
                    break;
                case "HOLD":
                    this.HoldReceived?.Invoke(this, new HoldNotification(body));
                    break;
                case "BYTECOUNT":
                    this.ByteCountReceived?.Invoke(this, new ByteCountNotification(body));
                    break;
                case "LOG":
                    this.LogReceived?.Invoke(this, new LogNotification(body));
                    break;
                case "INFO":
                    this.ReadyReceived?.Invoke(this, null);
                    break;
                default:
                    Log.Information("OpenVpn: Unknown notification type : " + buffer);
                    break;
            }
        }
    }
}