using Avira.Acp.Messages;

namespace Avira.Acp
{
    public class TransparentProxy : IProxy
    {
        public void InterceptRequest(Request request, RequestHandler requestHandler, ResponseHandler responseHandler)
        {
            requestHandler(request);
        }

        public void InterceptResponse(Response response, Request request, ResponseHandler responseHandler)
        {
            responseHandler(response);
        }

        public void InterceptNotification(Notification notification, NotificationHandler notificationHandler)
        {
            notificationHandler(notification);
        }
    }
}