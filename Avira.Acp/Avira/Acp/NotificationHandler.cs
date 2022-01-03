using Avira.Acp.Messages;
using Avira.Acp.Messages.JsonApi;

namespace Avira.Acp
{
    public delegate void NotificationHandler(Notification notification);

    public delegate void NotificationHandler<T>(Notification<T> notification);
}