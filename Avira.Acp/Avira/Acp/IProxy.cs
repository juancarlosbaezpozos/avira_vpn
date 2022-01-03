using Avira.Acp.Messages;

namespace Avira.Acp
{
    public interface IProxy
    {
        void InterceptRequest(Request request, RequestHandler requestHandler, ResponseHandler responseHandler);

        void InterceptResponse(Response response, Request request, ResponseHandler responseHandler);

        void InterceptNotification(Notification notification, NotificationHandler notificationHandler);
    }
}