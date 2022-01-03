using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Avira.Acp.Common;
using Avira.Acp.Messages;
using Avira.Acp.Messages.JsonApi;

namespace Avira.Acp.Caching.SmartCache
{
    public class DataBaseMapper<T> : IDataBaseMapper<T>
    {
        public virtual Expression<Func<SmartCacheEntry<T>, bool>> Get(string id)
        {
            return (SmartCacheEntry<T> r) => r.Resource.Id == id;
        }

        public virtual Expression<Func<SmartCacheEntry<T>, bool>> GetAll(string filter)
        {
            return (SmartCacheEntry<T> r) => string.IsNullOrEmpty(filter);
        }

        public SmartCacheEntry<T> MapResponse(Response<JsonObjectWrapper> response)
        {
            return CreateSmartCacheEntry(response.Payload.Data, response.Headers);
        }

        public IEnumerable<SmartCacheEntry<T>> MapResponseCollection(CollectionResponse<JsonObjectWrapper> response)
        {
            Dictionary<string, string> headers = ConvertHeaders(response.Headers);
            DateTime expirationdate = GetExpirationDate(response.Headers);
            return response.Payload.Data
                .Select((Resource<JsonObjectWrapper> r) => CreateSmartCacheEntry(r, headers, expirationdate)).ToList();
        }

        public SmartCacheEntry<T> MapNotification(Notification<JsonObjectWrapper> notification)
        {
            return CreateSmartCacheEntry(notification.Payload.Data, notification.Headers);
        }

        private SmartCacheEntry<T> CreateSmartCacheEntry(Resource<JsonObjectWrapper> data, IHeaderCollection headers)
        {
            return CreateSmartCacheEntry(data, ConvertHeaders(headers), GetExpirationDate(headers));
        }

        private SmartCacheEntry<T> CreateSmartCacheEntry(Resource<JsonObjectWrapper> data,
            Dictionary<string, string> headers, DateTime expirationDate)
        {
            return new SmartCacheEntry<T>(data, data.Id, headers, expirationDate);
        }

        private DateTime GetExpirationDate(IHeaderCollection headerCollection)
        {
            if (headerCollection.AviraCacheControl.MaxAge.HasValue)
            {
                return DateTime.Now + headerCollection.AviraCacheControl.MaxAge.Value;
            }

            return DateTime.Now + (headerCollection.CacheControl.MaxAge ?? TimeSpan.FromSeconds(0.0));
        }

        private Dictionary<string, string> ConvertHeaders(IHeaderCollection headerCollection)
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>
                { { "X-Cache", "HIT from launcher" } };
            string text = headerCollection.Get("Content-Type");
            if (text != null)
            {
                dictionary.Add("Content-Type", text);
            }

            string text2 = headerCollection.Get("Via");
            if (text2 != null)
            {
                dictionary.Add("Via", text2);
            }

            return dictionary;
        }
    }
}