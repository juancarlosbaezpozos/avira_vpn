using Avira.Acp.Messages;
using Avira.Acp.Messages.JsonApi;

namespace Avira.Acp
{
    public abstract class BaseResourceProvider<T> : IResourceProvider where T : class
    {
        protected readonly IAcpMessageBroker MessageBroker;

        public ResourceLocation ResourceLocation { get; }

        protected BaseResourceProvider(ResourceLocation resourceLocation, IAcpMessageBroker messageBroker)
        {
            MessageBroker = messageBroker;
            ResourceLocation = resourceLocation;
        }

        public abstract void HandleMessage(Request request);

        protected void RepositoryOnCreated(object sender, CreatedEventArgs<T> createdEventArgs)
        {
            SingleResourceDocument<T> payload = new SingleResourceDocument<T>
            {
                Data = new Resource<T>
                {
                    Id = createdEventArgs.ResourceId,
                    Type = createdEventArgs.ResourceType,
                    Attributes = createdEventArgs.CreatedResource
                }
            };
            MessageBroker.DispatchNotification(
                Notification.Create("POST", ResourceLocation.Path, ResourceLocation.Host, payload),
                createdEventArgs.Owner);
        }

        protected void RepositoryOnDeleted(object sender, DeletedEventArgs deletedEventArgs)
        {
            SingleResourceDocument<T> payload = new SingleResourceDocument<T>
            {
                Data = new Resource<T>
                {
                    Id = deletedEventArgs.ResourceId,
                    Type = deletedEventArgs.ResourceType
                }
            };
            MessageBroker.DispatchNotification(
                Notification.Create("DELETE", ResourceLocation.Path, ResourceLocation.Host, payload),
                deletedEventArgs.Owner);
        }

        protected void RepositoryOnUpdated(object sender, UpdatedEventArgs<T> updatedEventArgs)
        {
            SingleResourceDocument<T> payload = new SingleResourceDocument<T>
            {
                Data = new Resource<T>
                {
                    Id = updatedEventArgs.ResourceId,
                    Type = updatedEventArgs.ResourceType,
                    Attributes = updatedEventArgs.UpdatedResource
                }
            };
            MessageBroker.DispatchNotification(Notification.Create("PUT", ResourceLocation.Path, ResourceLocation.Host,
                payload));
        }

        protected string GetId(string path)
        {
            if (path.Length <= ResourceLocation.Path.Length || path.Contains("?"))
            {
                return string.Empty;
            }

            return GetSubpath(path, '/');
        }

        protected string GetFilter(string path)
        {
            if (path.Length <= ResourceLocation.Path.Length)
            {
                return string.Empty;
            }

            return GetSubpath(path, '?');
        }

        protected string GetSubpath(string path, char trim)
        {
            string text = path.Substring(ResourceLocation.Path.Length).Trim(trim);
            if (text.Length != 0)
            {
                return text;
            }

            return string.Empty;
        }
    }
}