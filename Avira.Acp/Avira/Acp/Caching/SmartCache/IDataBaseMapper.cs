using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Avira.Acp.Common;
using Avira.Acp.Messages.JsonApi;

namespace Avira.Acp.Caching.SmartCache
{
    public interface IDataBaseMapper<T>
    {
        SmartCacheEntry<T> MapResponse(Response<JsonObjectWrapper> response);

        IEnumerable<SmartCacheEntry<T>> MapResponseCollection(CollectionResponse<JsonObjectWrapper> response);

        SmartCacheEntry<T> MapNotification(Notification<JsonObjectWrapper> notification);

        Expression<Func<SmartCacheEntry<T>, bool>> Get(string id);

        Expression<Func<SmartCacheEntry<T>, bool>> GetAll(string filter);
    }
}