using System;
using System.Collections.Generic;
using Avira.Acp.Messages.JsonApi;

namespace Avira.Acp
{
    public interface IResourceRepositoryAsync<T> where T : class
    {
        event EventHandler<CreatedEventArgs<T>> Created;

        event EventHandler<DeletedEventArgs> Deleted;

        event EventHandler<UpdatedEventArgs<T>> Updated;

        void Create(Resource<T> resource, Action<Resource<T>> responseCallback, Action<Exception> errorCallback);

        void Read(string id, Action<Resource<T>> responseCallback, Action<Exception> errorCallback);

        void ReadAll(string filter, Action<List<Resource<T>>> responseCallback, Action<Exception> errorCallback);

        void Update(string id, Resource<T> resource, Action<Resource<T>> responseCallback,
            Action<Exception> errorCallback);

        void UpdateAll(string filter, Resource<T> resource, Action<List<Resource<T>>> responseCallback,
            Action<Exception> errorCallback);

        void Delete(string id);

        void DeleteAll(string filter);
    }
}