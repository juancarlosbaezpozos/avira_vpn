using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avira.VPN.Shared.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Avira.VPN.Core
{
    public class OpenVpnManager : IVpnConnector
    {
        private const int SendTimeout = 3000;

        private const int ReceiveTimeout = 9000;

        private readonly ISocket socket;

        private readonly IFileFactory fileFactory;

        private ITraffic traffic;

        private RegionConnectionSettings connectionSettings;

        private CancellationTokenSource socketListenerCancellationToken;

        private Task socketListenerTask;

        private string userName;

        private string password;

        private ulong lastByteCount;

        private readonly SemaphoreLock socketListenerInitLock = new SemaphoreLock();

        private VpnStatus status = VpnStatus.Disconnected;

        public VpnStatus Status
        {
            get { return status; }
            private set
            {
                status = value;
                this.StatusChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public event EventHandler<EventArgs> StatusChanged;

        public OpenVpnManager(ISocket socket, IFileFactory fileSystemOperations)
        {
            this.socket = socket;
            fileFactory = fileSystemOperations;
        }

        public async Task StartConnectAsync(RegionConnectionSettings connectionSettings, Credentials credentials)
        {
            try
            {
                Log.Information("Connecting VPN...");
                Status = VpnStatus.Connecting;
                this.connectionSettings = connectionSettings;
                userName = credentials.UserName;
                password = credentials.Password;
                if (string.Compare(userName, password) != 0)
                {
                    password = "sha1:" + Cryptography.ComputeSha1(password);
                }

                traffic = DiContainer.Resolve<ITraffic>();
                lastByteCount = 0uL;
                await ConnectWithRetry();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to connect.");
                throw ex;
            }
        }

        private async Task ConnectWithRetry()
        {
            int retry = 0;
            while (true)
            {
                try
                {
                    await ConnectSocket();
                    await InitiateConnection();
                    return;
                }
                catch (Exception ex)
                {
                    await StartDisconnectAsync();
                    retry++;
                    if (retry > 1)
                    {
                        throw ex;
                    }

                    Log.Warning(string.Format("Attempt {0} to connect failed. {1}", new object[2] { retry, ex }));
                    Status = VpnStatus.Connecting;
                    await Task.Delay(2000);
                }
            }
        }

        public async Task StartDisconnectAsync()
        {
            if (Status == VpnStatus.Disconnecting || Status == VpnStatus.Disconnected)
            {
                return;
            }

            Log.Information("Disconnecting VPN...");
            Status = VpnStatus.Disconnecting;
            try
            {
                await StopSocketListener();
                if (socket != null && socket.IsConnected())
                {
                    await SendCommand("signal SIGTERM\r\n");
                    await WaitForResponse(9000);
                }
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Error when disconnecting.");
            }
            finally
            {
                socket.Disconnect();
                Status = VpnStatus.Disconnected;
            }
        }

        private async Task StopSocketListener()
        {
            using (await socketListenerInitLock.AquireLockAsync())
            {
                socketListenerCancellationToken?.Cancel();
                if (socketListenerTask != null)
                {
                    await socketListenerTask;
                }

                socketListenerCancellationToken?.Dispose();
                socketListenerCancellationToken = null;
            }
        }

        private bool ContinueConnectSequence()
        {
            if (Status != VpnStatus.Disconnecting)
            {
                return Status != VpnStatus.Disconnected;
            }

            return false;
        }

        private async Task InitiateConnection()
        {
            if (ContinueConnectSequence())
            {
                await SendCommand("signal SIGHUP\r\n");
            }

            if (ContinueConnectSequence())
            {
                await WaitForResponse(9000);
            }

            if (ContinueConnectSequence())
            {
                await SendCommand("state on\r\n");
            }

            if (ContinueConnectSequence())
            {
                await WaitForResponse(9000);
            }

            if (ContinueConnectSequence())
            {
                await SendCommand("log on\r\n");
            }

            if (ContinueConnectSequence())
            {
                await WaitForResponse(9000);
            }

            if (ContinueConnectSequence())
            {
                await SendCommand("bytecount 1\r\n");
            }

            if (ContinueConnectSequence())
            {
                await WaitForResponse(9000);
            }

            if (ContinueConnectSequence())
            {
                await SendCommand("forget-passwords\r\n");
            }

            if (ContinueConnectSequence())
            {
                await WaitForResponse(9000);
            }

            if (ContinueConnectSequence())
            {
                await StartSocketListener();
            }
        }

        private async Task ConnectSocket()
        {
            Log.Debug("Connecting to unix endpoint socket...");
            for (int retry = 0; retry < 3; retry++)
            {
                if (IsSocketConnected())
                {
                    break;
                }

                try
                {
                    JObject jObject = JsonConvert.DeserializeObject<JObject>(DiContainer.Resolve<ISettings>()
                        ?.Get("openvpn_management", "{ \"host\":\"127.0.0.1\", \"port\":7505 }"));
                    await socket.Connect(jObject["host"]!.ToString(), int.Parse(jObject["port"]!.ToString()));
                }
                catch (Exception exception)
                {
                    Log.Warning(exception, $"Failed to connect socket. Retry {retry}...");
                    await Task.Delay(1000);
                }
            }

            if (!socket.IsConnected())
            {
                throw new Exception("Failed to connect to OpenVPN management interface.");
            }
        }

        private async Task StartSocketListener()
        {
            using (await socketListenerInitLock.AquireLockAsync())
            {
                if (ContinueConnectSequence())
                {
                    Log.Debug("Starting socket listener...");
                    socketListenerCancellationToken = new CancellationTokenSource();
                    socketListenerTask = Task.Factory.StartNew((Func<Task>)async delegate { await SocketListener(); },
                        socketListenerCancellationToken.Token, TaskCreationOptions.None,
                        TaskScheduler.FromCurrentSynchronizationContext());
                }
            }
        }

        private async Task SocketListener()
        {
            while (!socketListenerCancellationToken.IsCancellationRequested)
            {
                try
                {
                    await WaitForResponse(9000, socketListenerCancellationToken.Token);
                }
                catch (Exception exception)
                {
                    if (socketListenerCancellationToken.IsCancellationRequested)
                    {
                        Log.Debug("Cancellation requested. Stopping socket listener...");
                        return;
                    }

                    await ReconnectSocketIfDisconnected();
                    Log.Warning(exception, "Failed to process response.");
                }
            }

            Log.Debug("Socket listener exited...");
        }

        private async Task ReconnectSocketIfDisconnected()
        {
            if (!IsSocketConnected())
            {
                try
                {
                    Log.Warning("Socket is not connected anymore. Trying Socket reconnection.");
                    await ConnectSocket();
                }
                catch (Exception exception)
                {
                    Log.Error(exception, "Failed to reconnect socket after unexpected socket disconnect.");
                }
            }
        }

        private bool IsSocketConnected()
        {
            if (socket != null)
            {
                return socket.IsConnected();
            }

            return false;
        }

        private async Task SendCommand(string command)
        {
            _ = 1;
            try
            {
                Log.Debug("OpenVPN: Sending command " + command.TrimEnd('\n'));
                await ReconnectSocketIfDisconnected();
                await socket.Send(Encoding.UTF8.GetBytes(command), 3000);
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Failed to send command " + command + " to OpenVPN.");
            }
        }

        private async Task WaitForResponse(int timeout = -1,
            CancellationToken cancellationToken = default(CancellationToken))
        {
            byte[] array = await socket.Receive(timeout, cancellationToken);
            if (array.Length == 0)
            {
                return;
            }

            string[] array2 = Regex.Split(Encoding.UTF8.GetString(array, 0, array.Length), "\\r\\n");
            string[] array3 = array2;
            foreach (string text in array3)
            {
                if (!string.IsNullOrEmpty(text))
                {
                    await HandleResponse(text);
                }
            }
        }

        private async Task HandleResponse(string response)
        {
            if (response[0] == '>')
            {
                await HandleCommands(response);
            }
            else
            {
                await CheckCommandStatus(response);
            }
        }

        private async Task HandleCommands(string command)
        {
            Match match = Regex.Match(command, "^>(?<Command>[\\w]+):(?<Body>.*)");
            switch (match?.Groups["Command"]?.Value)
            {
                case "FATAL":
                    Log.Error(command);
                    throw new Exception(command);
                case "STATE":
                    await StateCommand(match.Groups["Body"]?.Value ?? string.Empty);
                    break;
                case "PASSWORD":
                    await AuthenticationCommand(match.Groups["Body"]?.Value ?? string.Empty);
                    break;
                case "HOLD":
                    await HoldCommand(match.Groups["Body"]?.Value ?? string.Empty);
                    break;
                case "BYTECOUNT":
                    BytecountCommand(match.Groups["Body"]?.Value ?? string.Empty);
                    break;
                case "LOG":
                    await LogCommand(match.Groups["Body"]?.Value ?? string.Empty);
                    break;
                case "INFO":
                    Log.Debug("OpenVPN: " + command);
                    break;
                case "REMOTE":
                    await RemoteCommand(match.Groups["Body"]?.Value ?? string.Empty);
                    break;
                default:
                    Log.Error("OpenVPN: Unknown command: " + command);
                    break;
            }
        }

        private async Task LogCommand(string body)
        {
            Log.Debug("OpenVPN: >LOG: " + body);
            if (body.Contains("AUTH: Received control message: AUTH_FAILED"))
            {
                await StartDisconnectAsync();
            }
        }

        private async Task CheckCommandStatus(string response)
        {
            Match regex = Regex.Match(response, "^ERROR:(?<Message>.*)");
            if (regex.Success)
            {
                Log.Error("OpenVPN: " + regex.Groups["Message"]?.Value);
                await StartDisconnectAsync();
                throw new Exception(regex.Groups["Message"]?.Value);
            }

            Log.Debug("OpenVPN: " + response);
        }

        private async Task HoldCommand(string body)
        {
            Log.Debug("OpenVPN: >HOLD: " + body);
            if (body.Contains("Waiting for hold release"))
            {
                await SendCommand("hold release\r\n");
                await WaitForResponse(9000);
            }
        }

        private async Task StateCommand(string body)
        {
            Log.Debug("OpenVPN: >STATE: " + body);
            switch (Regex.Match(body, ",(?<Status>[\\w]+),")?.Groups["Status"]?.Value)
            {
                case "CONNECTING":
                case "RECONNECTING":
                    Status = VpnStatus.Connecting;
                    break;
                case "EXITING":
                    await StartDisconnectAsync();
                    break;
                case "CONNECTED":
                    Status = VpnStatus.Connected;
                    break;
                case "WAIT":
                    break;
                case "AUTH":
                    break;
                case "GET_CONFIG":
                    break;
                case "ASSIGN_IP":
                    break;
                case "ADD_ROUTES":
                    break;
            }
        }

        private async Task AuthenticationCommand(string body)
        {
            Log.Debug("OpenVPN: >PASSWORD: " + body);
            if (body.Contains("Need"))
            {
                await SendCommand("username \"Auth\" " + userName + "\r\npassword \"Auth\" " + password + "\r\n");
                await WaitForResponse(9000);
            }
            else if (body.Contains("Verification Failed"))
            {
                Log.Error("Authentication failed. Disconnecting.");
                await StartDisconnectAsync();
            }
        }

        private async Task RemoteCommand(string body)
        {
            Log.Debug("OpenVPN: >REMOTE: " + body);
            await SendCommand(string.Format("remote MOD {0} {1}\r\n",
                new object[2] { connectionSettings.Host, connectionSettings.Port }));
            await WaitForResponse(9000);
        }

        private void BytecountCommand(string data)
        {
            try
            {
                int num = data.IndexOf(',');
                if (num != -1)
                {
                    ulong num2 = ulong.Parse(data.Substring(0, num));
                    ulong num3 = ulong.Parse(data.Substring(num + 1, data.Length - num - 1));
                    if (lastByteCount == 0L)
                    {
                        lastByteCount = num2 + num3;
                    }

                    ulong num4 = num2 + num3 - lastByteCount;
                    if (num4 != 0L)
                    {
                        num4 = num4 * 95 / 100uL;
                    }

                    lastByteCount = num2 + num3;
                    traffic.Update(num4);
                }
            }
            catch (Exception exception)
            {
                Log.Warning(exception, "Error processing BYTECOUNT.");
            }
        }
    }
}