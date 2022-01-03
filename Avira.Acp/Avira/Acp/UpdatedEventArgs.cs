using System;

namespace Avira.Acp
{
    public class UpdatedEventArgs<T> : EventArgs
    {
        public T UpdatedResource { get; private set; }

        public string ResourceId { get; private set; }

        public string ResourceType { get; private set; }

        public UpdatedEventArgs(T updatedResource, string resourceId, string resourceType)
        {
            UpdatedResource = updatedResource;
            ResourceId = resourceId;
            ResourceType = resourceType;
        }
    }
}