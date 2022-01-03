using System;
using System.Runtime.Serialization;
using Avira.Acp.Endpoints.NamedPipe.Monitor;
using Avira.Acp.Logging;
using Avira.Acp.Messages;
using Avira.Acp.Messages.JsonApi;

namespace Avira.Acp.Endpoints.NamedPipe
{
    [DataContract]
    public class NamedPipeAdapter : AdapterBase, IEndpoint, IEndpointData, INamedPipeAdapter
    {
        private readonly ILogger logger = LoggerFacade.GetCurrentClassLogger();

        private readonly IDuplexNamedPipeChannel namedPipeChannel;

        private readonly IHandshakeProcessor handshakeProcessor;

        private readonly IEndpointRepository endpointRepository;

        private readonly INamedPipeAvailabilityMonitorFactory namedPipeAvailabilityMonitorFactory;

        private readonly string localHost;

        private INamedPipeAvailabilityMonitor namedPipeAvailabilityMonitor;

        [DataMember(Name = "host")] public string Host { get; private set; }

        [DataMember(Name = "channel_type")] public string ChannelType => "named-pipe";

        [DataMember(Name = "named_pipe")] public string NamedPipeName => namedPipeChannel.ReceiverPipeName;

        [IgnoreDataMember] public NamedPipeConnectionState ConnectionState { get; private set; }

        private string ServerPipeName { get; set; }

        public NamedPipeAdapter(IDuplexNamedPipeChannel namedPipeChannel, IHandshakeProcessor handshakeProcessor,
            IRemoteMessageProcessor remoteMessageProcessor, IEndpointRepository endpointRepository, string localHost)
            : this(namedPipeChannel, handshakeProcessor, remoteMessageProcessor, endpointRepository,
                new NamedPipeAvailabilityMonitorFactory(), localHost, reconnect: true)
        {
        }

        public NamedPipeAdapter(IDuplexNamedPipeChannel namedPipeChannel, IHandshakeProcessor handshakeProcessor,
            IRemoteMessageProcessor remoteMessageProcessor, IEndpointRepository endpointRepository,
            INamedPipeAvailabilityMonitorFactory namedPipeAvailabilityMonitorFactory, string localHost, bool reconnect)
            : base(namedPipeChannel, remoteMessageProcessor, localHost)
        {
            this.namedPipeChannel = namedPipeChannel;
            this.handshakeProcessor = handshakeProcessor;
            this.endpointRepository = endpointRepository;
            this.namedPipeAvailabilityMonitorFactory = namedPipeAvailabilityMonitorFactory;
            this.localHost = localHost;
            if (!reconnect)
            {
                return;
            }

            this.namedPipeChannel.Restarted += delegate
            {
                if (ServerPipeName != null)
                {
                    Reconnect();
                }
            };
        }

        public void Connect(string remoteHost, string serverPipeName)
        {
            Host = remoteHost;
            ServerPipeName = serverPipeName;
            namedPipeAvailabilityMonitor?.Stop();
            namedPipeAvailabilityMonitor = namedPipeAvailabilityMonitorFactory.Create(serverPipeName);
            namedPipeAvailabilityMonitor.StatusChanged +=
                delegate(object sender, NamedPipeAvailabilityChangedEventArgs args)
                {
                    logger.Info($"Named pipe status changed: {args.Status}");
                    if (args.Status == NamedPipeStatus.Available)
                    {
                        try
                        {
                            ConnectAndSendHandshake();
                            namedPipeAvailabilityMonitor.Stop();
                        }
                        catch (Exception ex)
                        {
                            logger.Warn("Failed to reconnect. " + ex.Message);
                            namedPipeAvailabilityMonitor.Reset();
                        }
                    }
                };
            Reconnect();
        }

        public void Reconnect()
        {
            if (Host == null || ServerPipeName == null)
            {
                throw new Exception("Cannot reconnect because no connection was established before.");
            }

            if (ConnectionState != 0)
            {
                logger.Info($"Skip reconnect. Current connection state: {ConnectionState}");
                return;
            }

            ConnectionState = NamedPipeConnectionState.Connecting;
            namedPipeAvailabilityMonitor.Start();
        }

        public void ProcessMessages()
        {
            bool flag = false;
            try
            {
                DoHandshake();
                flag = true;
                RemoteMessageProcessor.Initialize(Host, this);
                ConnectionState = NamedPipeConnectionState.Connected;
                while (namedPipeChannel.IsOpen())
                {
                    string json = namedPipeChannel.Receive();
                    Message acpMessage = AcpMessageSerializer.Instance.DeserializeFromJson(json);
                    RemoteMessageProcessor.ProcessMessage(acpMessage, Send);
                }
            }
            finally
            {
                ConnectionState = NamedPipeConnectionState.Disconnected;
                if (flag)
                {
                    RemoteMessageProcessor.UnregisterRemoteResources();
                    endpointRepository.Remove(this);
                }

                logger.Info($"Named pipe {NamedPipeName} got disconnected");
            }
        }

        public void Close()
        {
            RemoteMessageProcessor.UnregisterRemoteResources();
            namedPipeChannel.Close();
            ConnectionState = NamedPipeConnectionState.Disconnected;
        }

        private void ConnectAndSendHandshake()
        {
            namedPipeChannel.Connect(ServerPipeName);
            Request<HandshakeRequestData> request =
                handshakeProcessor.CreateHandshakeRequest(localHost, Host, namedPipeChannel.ReceiverPipeName);
            Send(request);
        }

        private void DoHandshake()
        {
            if (ConnectionState == NamedPipeConnectionState.Connecting)
            {
                ProcessHandshakeResponse();
            }
            else
            {
                ProcessHandshakeRequest();
            }
        }

        private void ProcessHandshakeRequest()
        {
            Request<HandshakeRequestData> request = DeserealizeHandshakeRequest(namedPipeChannel.Receive());
            Host = request.PayloadDataAttributes.Host;
            string endpointId = endpointRepository.Add(this);
            HandshakeResult handshakeResult = handshakeProcessor.ProcessRequest(request, endpointId);
            namedPipeChannel.Connect(handshakeResult.NamedPipeName);
            Send(handshakeResult.Response);
        }

        private void ProcessHandshakeResponse()
        {
            handshakeProcessor.ProcessResponse(DeserilizeHandshakeResponse(namedPipeChannel.Receive()));
            endpointRepository.Add(this);
        }

        private Request<HandshakeRequestData> DeserealizeHandshakeRequest(string message)
        {
            Request<HandshakeRequestData> request = new Request<HandshakeRequestData>(
                AcpMessageSerializer.Instance.DeserializeFromJson(message) ??
                throw new Exception("Unable to deserialize handshake message: " + message));
            if (request.PayloadDataAttributes == null)
            {
                throw new Exception("Unable to deserialize handshake message payload: " + message);
            }

            return request;
        }

        private Response DeserilizeHandshakeResponse(string message)
        {
            return new Response(AcpMessageSerializer.Instance.DeserializeFromJson(message) ??
                                throw new SerializationException("Unable to deserialize handshake response: " +
                                                                 message));
        }
    }
}