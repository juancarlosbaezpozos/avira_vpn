using Avira.Acp.Messages;

namespace Avira.Acp.Endpoints
{
    public interface IChannel
    {
        void Send(Request request);

        void Send(Response message);

        void Send(Notification message);
    }
}