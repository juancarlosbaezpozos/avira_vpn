using System;
using System.Collections.Generic;
using System.Net;
using Avira.Acp.Messages.JsonApi;

namespace Avira.Acp
{
    public abstract class BaseResourceRepositoryAsync<T> : IResourceRepositoryAsync<T> where T : class
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

        public virtual void Create(Resource<T> resource, Action<Resource<T>> responseCallback,
            Action<Exception> errorCallback)
        {
            throw new ResourceCreateException(HttpStatusCode.MethodNotAllowed);
        }

        public virtual void Read(string id, Action<Resource<T>> responseCallback, Action<Exception> errorCallback)
        {
            throw new ResourceReadException(HttpStatusCode.MethodNotAllowed);
        }

        public virtual void ReadAll(string filter, Action<List<Resource<T>>> responseCallback,
            Action<Exception> errorCallback)
        {
            throw new ResourceReadException(HttpStatusCode.MethodNotAllowed);
        }

        public virtual void Update(string id, Resource<T> resource, Action<Resource<T>> responseCallback,
            Action<Exception> errorCallback)
        {
            throw new ResourceUpdateException(HttpStatusCode.MethodNotAllowed);
        }

        public virtual void UpdateAll(string filter, Resource<T> resource, Action<List<Resource<T>>> responseCallback,
            Action<Exception> errorCallback)
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