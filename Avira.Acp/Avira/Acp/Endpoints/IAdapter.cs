using Avira.Acp.Messages;

namespace Avira.Acp.Endpoints
{
    public interface IAdapter
    {
        void Send(Response response);

        void Send(Request request);

        void Send(Notification notification);
    }
}