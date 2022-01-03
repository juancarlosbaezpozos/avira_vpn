using System;
using System.Collections.Generic;
using System.Net;
using Avira.Acp.Messages.JsonApi;

namespace Avira.Acp
{
    public abstract class BaseResourceRepository<T> : IResourceRepository<T> where T : class
    {
        public event EventHandler<CreatedEventArgs<T>> Created;

        public event EventHandler<DeletedEventArgs> Deleted;

        public event EventHandler<UpdatedEventArgs<T>> Updated;

        protected void OnCreated(CreatedEventArgs<T> createdEventArgs)
        {
            this.Created?.Invoke(this, createdEventArgs);
        }

        protected void OnDeleted(DeletedEventArgs deletedEventArgs)
        {
            this.Deleted?.Invoke(this, deletedEventArgs);
        }

        protected void OnUpdated(UpdatedEventArgs<T> updatedEventArgs)
        {
            this.Updated?.Invoke(this, updatedEventArgs);
        }

        public virtual Resource<T> Create(Resource<T> resource)
        {
            throw new ResourceCreateException(HttpStatusCode.MethodNotAllowed);
        }

        public virtual Resource<T> Read(string id)
        {
            throw new ResourceReadException(HttpStatusCode.MethodNotAllowed);
        }

        public virtual List<Resource<T>> ReadAll(string filter)
        {
            throw new ResourceReadException(HttpStatusCode.MethodNotAllowed);
        }

        public virtual Resource<T> Update(string id, Resource<T> resource)
        {
            throw new ResourceUpdateException(HttpStatusCode.MethodNotAllowed);
        }

        public virtual List<Resource<T>> UpdateAll(string filter, Resource<T> resource)
        {
            throw new ResourceUpdateException(HttpStatusCode.MethodNotAllowed);
        }

        public virtual void Delete(string id)
        {
            throw new ResourceDeleteException(HttpStatusCode.MethodNotAllowed);
        }

        public virtual void DeleteAll(string filter)
        {
            throw new ResourceDeleteException(HttpStatusCode.MethodNotAllowed);
        }
    }
}