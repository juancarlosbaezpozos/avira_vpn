using System.IO.Pipes;
using Avira.Acp.Endpoints.NamedPipe.Monitor;

namespace Avira.Acp.Endpoints.NamedPipe
{
    public class NamedPipeListenerFactory : INamedPipeListenerFactory
    {
        private readonly string pipeName;

        private readonly INamedPipeAuthenticationService authenticationService;

        private readonly INamedPipeAuthTokenExtractor namedPipeAuthTokenExtractor;

        private readonly PipeSecurity pipeSecurity;

        private readonly IAcpMessageBroker acpMessageBroker;

        private readonly IEndpointRepository endpointRepository;

        public NamedPipeListenerFactory(string pipeName, INamedPipeAuthenticationService authenticationService,
            INamedPipeAuthTokenExtractor namedPipeAuthTokenExtractor, IAcpMessageBroker acpMessageBroker,
            IEndpointRepository endpointRepository, bool adminOnly, bool allowGuestsAccount)
        {
            this.pipeName = pipeName;
            this.authenticationService = authenticationService;
            this.namedPipeAuthTokenExtractor = namedPipeAuthTokenExtractor;
            this.acpMessageBroker = acpMessageBroker;
            this.endpointRepository = endpointRepository;
            pipeSecurity = PipeSecurityFactory.CreatePipeSecurity(allowGuestsAccount, adminOnly);
        }

        public INamedPipeListener CreateListener(bool closeNamedpipe)
        {
            DuplexNamedPipeChannel duplexNamedPipeChannel = new DuplexNamedPipeChannel(pipeName, pipeSecurity,
                authenticationService, namedPipeAuthTokenExtractor, closeNamedpipe);
            NamedPipeAdapter namedPipeAdapter2 = (NamedPipeAdapter)(duplexNamedPipeChannel.NamedPipeAdapter =
                new NamedPipeAdapter(duplexNamedPipeChannel, new HandshakeProcessor(),
                    new RemoteMessageProcessor(acpMessageBroker, new RemoteResourceRegistrator(acpMessageBroker),
                        acpMessageBroker.HostName), endpointRepository, new NamedPipeAvailabilityMonitorFactory(),
                    acpMessageBroker.HostName, reconnect: false));
            return duplexNamedPipeChannel;
        }
    }
}