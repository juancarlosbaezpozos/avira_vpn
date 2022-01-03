using System;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading.Tasks;
using Avira.Acp;
using Avira.Acp.Endpoints;
using Avira.Acp.Endpoints.NamedPipe;
using Avira.Acp.Messages;
using Avira.Acp.Messages.JsonApi;
using ServiceStack.Text;

namespace Avira.Common.Acp.AppClient
{
    public class AcpCommunicator : IAcpCommunicator
    {
        private readonly EndpointRepository endpointRepository = new EndpointRepository();

        private AcpMessageBroker messageBroker;

        private NamedPipeAdapter namedPipeAdapter;

        private DuplexNamedPipeChannel duplexNamedPipeChannel;

        private bool isInitialized;

        private string launcherEndpointId = string.Empty;

        public bool EnableAuthenticodeCheck { get; set; } = true;


        public event EventHandler<EventArgs> Connected;

        public AcpCommunicator(string hostName)
        {
            messageBroker = new AcpMessageBroker(hostName);
        }

        public bool IsConnected()
        {
            if (duplexNamedPipeChannel != null && duplexNamedPipeChannel.IsOpen())
            {
                return isInitialized;
            }

            return false;
        }

        public void RegisterRepository<T>(BaseResourceRepository<T> repository, string path) where T : class
        {
            ResourceLocation resourceLocation = new ResourceLocation
            {
                Host = messageBroker.HostName,
                Path = path
            };
            ResourceProvider<T> provider = new ResourceProvider<T>(repository, resourceLocation, messageBroker);
            messageBroker.RegisterResource(provider.ResourceLocation,
                delegate(Request<T> r) { provider.HandleMessage(r); });
        }

        public async Task<bool> ConnectToLauncher()
        {
            messageBroker.CreateSubscription(new ResourceLocation(messageBroker.HostName, "/endpoints"),
                async delegate(Notification<IEndpoint> n)
                {
                    if (n.Verb == "POST" && n.Payload?.Data?.Attributes?.Host == "launcher")
                    {
                        await GetRequest("launcher", "/resources");
                        launcherEndpointId = n.Payload?.Data?.Id;
                        isInitialized = true;
                        this.Connected?.Invoke(this, new EventArgs());
                    }
                    else if (n.Verb == "DELETE" && n.Payload?.Data?.Id == launcherEndpointId)
                    {
                        isInitialized = false;
                    }
                });
            RegisterRepository(endpointRepository, "/endpoints");
            AviraSignatureAuthenticator authenticationService =
                (EnableAuthenticodeCheck ? new AviraSignatureAuthenticator() : null);
            duplexNamedPipeChannel = new DuplexNamedPipeChannel(Guid.NewGuid().ToString(), CreatePipeSecurity(),
                authenticationService, new NamedPipeAuthTokenExtractor());
            namedPipeAdapter = new NamedPipeAdapter(duplexNamedPipeChannel, new HandshakeProcessor(),
                new RemoteMessageProcessor(messageBroker, new RemoteResourceRegistrator(messageBroker),
                    messageBroker.HostName), endpointRepository, messageBroker.HostName);
            duplexNamedPipeChannel.NamedPipeAdapter = namedPipeAdapter;
            duplexNamedPipeChannel.Start();
            namedPipeAdapter.Connect("launcher", "Avira.Launcher.AcpNamedPipe");
            return await WaitForConnection();
        }

        public async Task<bool> WaitForConnection()
        {
            int tickCount = Environment.TickCount;
            while (namedPipeAdapter.ConnectionState == NamedPipeConnectionState.Connecting &&
                   Environment.TickCount - tickCount < 5000)
            {
                await Task.Delay(10);
            }

            while (!isInitialized && Environment.TickCount - tickCount < 6000)
            {
                await Task.Delay(10);
            }

            return namedPipeAdapter.ConnectionState == NamedPipeConnectionState.Connected;
        }

        private async Task PollAndReconnect()
        {
            int tick = Environment.TickCount;
            while (Environment.TickCount - tick < 60000)
            {
                await Task.Delay(500);
                if (await ConnectToLauncher())
                {
                    break;
                }
            }
        }

        private PipeSecurity CreatePipeSecurity()
        {
            PipeSecurity pipeSecurity = new PipeSecurity();
            pipeSecurity.AddAccessRule(new PipeAccessRule(WindowsIdentity.GetCurrent().User,
                PipeAccessRights.FullControl, AccessControlType.Allow));
            pipeSecurity.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null),
                PipeAccessRights.ReadWrite, AccessControlType.Allow));
            pipeSecurity.AddAccessRule(new PipeAccessRule(
                new SecurityIdentifier(WellKnownSidType.LocalServiceSid, null), PipeAccessRights.ReadWrite,
                AccessControlType.Allow));
            pipeSecurity.AddAccessRule(new PipeAccessRule(
                new SecurityIdentifier(WellKnownSidType.AuthenticatedUserSid, null), PipeAccessRights.Read,
                AccessControlType.Allow));
            return pipeSecurity;
        }

        public static Task<T> AsAsync<T>(Action<Action<T>> target)
        {
            TaskCompletionSource<T> tcs = new TaskCompletionSource<T>();
            try
            {
                target(delegate(T t) { tcs.SetResult(t); });
            }
            catch (Exception exception)
            {
                tcs.SetException(exception);
            }

            return tcs.Task;
        }

        public async Task<Response> GetRequest(string host, string path)
        {
            return await AsAsync(delegate(Action<Response> r)
            {
                messageBroker.DispatchRequest(Request.Create("GET", host, path), r.Invoke);
            });
        }

        public async Task<Response> PostRequest(string host, string path, string payload)
        {
            JsonObject payload2 = JsonObject.Parse(payload);
            Request request = Request.Create("POST", host, path, payload2);
            return await AsAsync(delegate(Action<Response> r) { messageBroker.DispatchRequest(request, r.Invoke); });
        }

        public string Subscribe(string host, string path, NotificationHandler notificationHandler)
        {
            return messageBroker.CreateSubscription(new ResourceLocation(host, path), notificationHandler);
        }

        public void Unsubscribe(string subscriptionId)
        {
            messageBroker.RemoveSubscription(subscriptionId);
        }

        public void Dispose()
        {
            if (duplexNamedPipeChannel != null)
            {
                duplexNamedPipeChannel.Dispose();
            }
        }
    }
}