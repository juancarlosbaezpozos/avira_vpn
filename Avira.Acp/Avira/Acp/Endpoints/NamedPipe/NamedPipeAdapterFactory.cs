using System;
using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;

namespace Avira.Acp.Endpoints.NamedPipe
{
    public class NamedPipeAdapterFactory
    {
        private readonly IAcpMessageBroker acpMessageBroker;

        private readonly IEndpointRepository endpointRepository;

        public NamedPipeAdapterFactory(IAcpMessageBroker acpMessageBroker)
            : this(acpMessageBroker, CreateEndpointRepository(acpMessageBroker))
        {
        }

        public NamedPipeAdapterFactory(IAcpMessageBroker acpMessageBroker, IEndpointRepository endpointRepository)
        {
            this.acpMessageBroker = acpMessageBroker;
            this.endpointRepository = endpointRepository;
        }

        public INamedPipeAdapter Create()
        {
            return Create(new WellKnownSidType[2]
            {
                WellKnownSidType.LocalSystemSid,
                WellKnownSidType.BuiltinAdministratorsSid
            });
        }

        public INamedPipeAdapter Create(WellKnownSidType[] allowedSidTypes,
            INamedPipeAuthenticationService authenticationService = null,
            INamedPipeAuthTokenExtractor namedPipeAuthTokenExtractor = null)
        {
            DuplexNamedPipeChannel duplexNamedPipeChannel = new DuplexNamedPipeChannel(Guid.NewGuid().ToString(),
                CreatePipeSecurity(allowedSidTypes), authenticationService, namedPipeAuthTokenExtractor);
            NamedPipeAdapter result = (NamedPipeAdapter)(duplexNamedPipeChannel.NamedPipeAdapter = new NamedPipeAdapter(
                duplexNamedPipeChannel, new HandshakeProcessor(),
                new RemoteMessageProcessor(acpMessageBroker, new RemoteResourceRegistrator(acpMessageBroker),
                    acpMessageBroker.HostName), endpointRepository, acpMessageBroker.HostName));
            duplexNamedPipeChannel.Start();
            return result;
        }

        private static PipeSecurity CreatePipeSecurity(WellKnownSidType[] allowedSidTypes)
        {
            PipeSecurity pipeSecurity = new PipeSecurity();
            foreach (WellKnownSidType sidType in allowedSidTypes)
            {
                pipeSecurity.AddAccessRule(new PipeAccessRule(new SecurityIdentifier(sidType, null),
                    PipeAccessRights.ReadWrite, AccessControlType.Allow));
            }

            return pipeSecurity;
        }

        private static EndpointRepository CreateEndpointRepository(IAcpMessageBroker acpMessageBroker)
        {
            EndpointRepository obj = new EndpointRepository();
            ResourceLocation resourceLocation = new ResourceLocation
            {
                Host = acpMessageBroker.HostName,
                Path = "/endpoints"
            };
            ResourceProvider<IEndpoint> resourceProvider =
                new ResourceProvider<IEndpoint>(obj, resourceLocation, acpMessageBroker);
            acpMessageBroker.RegisterResource(resourceLocation, resourceProvider.HandleMessage);
            return obj;
        }
    }
}