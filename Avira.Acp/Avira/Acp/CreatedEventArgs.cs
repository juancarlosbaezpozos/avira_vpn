using System;

namespace Avira.Acp
{
    public class CreatedEventArgs<T> : EventArgs
    {
        public T CreatedResource { get; private set; }

        public string ResourceId { get; private set; }

        public string ResourceType { get; private set; }

        public string Owner { get; private set; }

        public CreatedEventArgs(T createdResource, string resourceId, string resourceType)
            : this(string.Empty, createdResource, resourceId, resourceType)
        {
        }

        public CreatedEventArgs(string owner, T createdResource, string resourceId, string resourceType)
        {
            CreatedResource = createdResource;
            ResourceId = resourceId;
            ResourceType = resourceType;
            Owner = owner;
        }
    }
}