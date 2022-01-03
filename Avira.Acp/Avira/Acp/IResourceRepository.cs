using System;
using System.Collections.Generic;
using Avira.Acp.Messages.JsonApi;

namespace Avira.Acp
{
    public interface IResourceRepository<T> where T : class
    {
        event EventHandler<CreatedEventArgs<T>> Created;

        event EventHandler<DeletedEventArgs> Deleted;

        event EventHandler<UpdatedEventArgs<T>> Updated;

        Resource<T> Create(Resource<T> resource);

        Resource<T> Read(string id);

        List<Resource<T>> ReadAll(string filter);

        Resource<T> Update(string id, Resource<T> resource);

        List<Resource<T>> UpdateAll(string filter, Resource<T> resource);

        void Delete(string id);

        void DeleteAll(string filter);
    }
}