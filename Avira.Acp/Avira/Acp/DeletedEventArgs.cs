using System;

namespace Avira.Acp
{
    public class DeletedEventArgs : EventArgs
    {
        public string ResourceId { get; private set; }

        public string ResourceType { get; private set; }

        public string Owner { get; set; }

        public DeletedEventArgs(string resourceId, string resourceType)
            : this(string.Empty, resourceId, resourceType)
        {
        }

        public DeletedEventArgs(string owner, string resourceId, string resourceType)
        {
            ResourceId = resourceId;
            ResourceType = resourceType;
            Owner = owner;
        }
    }
}