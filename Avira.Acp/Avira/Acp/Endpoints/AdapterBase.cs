using Avira.Acp.Messages;

namespace Avira.Acp.Endpoints
{
    public class AdapterBase : IAdapter
    {
        protected readonly IRemoteMessageProcessor RemoteMessageProcessor;

        protected readonly IChannel Channel;

        protected readonly string ViaHeaderValue;

        public AdapterBase(IChannel channel, IRemoteMessageProcessor remoteMessageProcessor, string localHost)
        {
            Channel = channel;
            RemoteMessageProcessor = remoteMessageProcessor;
            ViaHeaderValue = "acp/1.0 " + localHost;
        }

        public virtual void Send(Response response)
        {
            response.Headers.Append("Via", ViaHeaderValue);
            Channel.Send(response);
        }

        public virtual void Send(Request request)
        {
            request.Headers.Append("Via", ViaHeaderValue);
            Channel.Send(request);
        }

        public virtual void Send(Notification notification)
        {
            notification.Headers.Append("Via", ViaHeaderValue);
            Channel.Send(notification);
        }
    }
}