using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Avira.Acp.Caching.SmartCache
{
    public interface ICacheDataAccess : IDisposable
    {
        bool Create<T>(string dataType, SmartCacheEntry<T> data);

        int CreateList<T>(string dataType, IEnumerable<SmartCacheEntry<T>> data);

        bool Delete(string dataType, string id);

        SmartCacheEntry<T> Get<T>(string dataType, Expression<Func<SmartCacheEntry<T>, bool>> predicate);

        List<SmartCacheEntry<T>> GetAll<T>(string dataType, Expression<Func<SmartCacheEntry<T>, bool>> predicate);

        bool Update<T>(string dataType, SmartCacheEntry<T> data);

        void DeleteAll(string collectionName);

        bool Exist<T>(string dataType, string id);

        bool Create(string dataType, SmartCacheResourceLocation data);

        SmartCacheResourceLocation Get(string dataType, string id);

        void DeleteAll(string dataType, ResourceLocation resourceLocation);
    }
}