using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using Avira.Acp.Common;
using Avira.Acp.Logging;
using Avira.Acp.Messages;
using Avira.Acp.Messages.JsonApi;

namespace Avira.Acp.Caching.SmartCache
{
    public class SmartCacheLogic<T> : ISmartCacheLogic
    {
        private const string ResourceLocationCollectionName = "__ResourceLocation";

        private readonly ICacheDataAccess cacheDataAccess;

        private readonly IDataBaseMapper<T> dataBaseMapper;

        private readonly ILogger logger;

        private readonly string dataType;

        public ResourceLocation ResourceLocation { get; }

        public SmartCacheLogic(ICacheDataAccess cacheDataAccess, IDataBaseMapper<T> dataBaseMapper,
            ResourceLocation resourceLocation, string dataType)
        {
            this.cacheDataAccess = cacheDataAccess;
            this.dataBaseMapper = dataBaseMapper;
            ResourceLocation = resourceLocation;
            this.dataType = dataType;
            logger = LoggerFacade.GetLogger("SmartCacheLogic`" + dataType);
        }

        public bool TryGetDataFromCache(Request request, out Response response)
        {
            response = null;
            if (request.Verb != "GET" && request.Verb != "DELETE")
            {
                return false;
            }

            if (request.Verb == "DELETE")
            {
                string deleteIdForRequest = GetDeleteIdForRequest(request);
                bool flag = DeleteEntryInCache(deleteIdForRequest);
                logger.Info(
                    $"Delete cache entry {deleteIdForRequest} for {request.Host}{request.Path}. Operation succeed: {flag}");
                return false;
            }

            if (TryGetSingleData(request, out response))
            {
                logger.Info($"HIT: Single Request [{request.Id}] to {request.Host}{request.Path}");
                return true;
            }

            if (TryGetCollectionData(request, out response))
            {
                logger.Info($"HIT: Collection Request [{request.Id}] to {request.Host}{request.Path}");
                return true;
            }

            logger.Info($"MISS: Request [{request.Id}] to {request.Host}{request.Path}");
            return false;
        }

        public void Cache(Response response, string verb, string host, string path)
        {
            if (response.StatusCode == HttpStatusCode.NotFound && verb == "PUT")
            {
                string idFromPath = GetIdFromPath(path);
                if (!string.IsNullOrEmpty(idFromPath))
                {
                    logger.Info($"Response to PUT request is 404 , removing resource {idFromPath} from cache");
                    DeleteEntryInCache(idFromPath);
                }

                return;
            }

            if (StatusCodeNotValid(response) || !IsHeaderCacheControlValid(response.Headers))
            {
                logger.Info(
                    $"Response [{response.Id}] can not be cached - status code or cache control header are invalid");
                return;
            }

            Response<JsonObjectWrapper> response2 = Response<JsonObjectWrapper>.ConvertFrom(response);
            if (response2?.Payload?.Data == null)
            {
                CollectionResponse<JsonObjectWrapper> collectionResponse =
                    CollectionResponse<JsonObjectWrapper>.ConvertFrom(response);
                if (collectionResponse?.Payload?.Data != null && collectionResponse.Payload.Data.Any())
                {
                    CacheCollection(collectionResponse, new SmartCacheResourceLocation(host, path), verb);
                }
            }
            else
            {
                CacheSingle(response2, verb);
            }
        }

        public void Cache(Notification notification)
        {
            if (IsHeaderCacheControlValid(notification.Headers))
            {
                if (notification.Verb.Equals("DELETE"))
                {
                    string resourceId = GetResourceId(notification);
                    bool flag = DeleteEntryInCache(resourceId);
                    logger.Info(
                        $"Delete cache entry {resourceId} for notification {notification.Sender}{notification.Path}. Operation succeed: {flag}");
                }
                else
                {
                    CacheEntry(notification.Verb,
                        dataBaseMapper.MapNotification(Notification<JsonObjectWrapper>.ConvertFrom(notification)));
                }
            }
        }

        public void Clear()
        {
            cacheDataAccess.DeleteAll("__ResourceLocation", ResourceLocation);
            cacheDataAccess.DeleteAll(dataType);
        }

        private string GetResourceId(Notification notification)
        {
            return Notification<T>.ConvertFrom(notification)?.Payload?.Data?.Id ??
                   GetDeleteIdForNotification(notification);
        }

        private bool TryGetCollectionData(Request request, out Response response)
        {
            response = null;
            List<SmartCacheEntry<T>> collectionData = GetCollectionData(request);
            if (collectionData == null)
            {
                return false;
            }

            if (collectionData.Any((SmartCacheEntry<T> entry) => entry.IsExpired()))
            {
                logger.Info($"EXPIRED: Collection for {request.Host}{request.Path}");
                collectionData.ForEach(delegate(SmartCacheEntry<T> entry) { DeleteEntryInCache(entry.Id); });
                InvalidateRequest(request);
                return false;
            }

            response = Response.CreateCollection(request.Id, HttpStatusCode.OK,
                collectionData.Select((SmartCacheEntry<T> s) => s.OriginalResource).ToList(),
                collectionData.First().Header);
            return true;
        }

        private void InvalidateRequest(Request request)
        {
            cacheDataAccess.Delete("__ResourceLocation", request.ResourceLocation.GetHashCode().ToString());
        }

        private bool TryGetSingleData(Request request, out Response response)
        {
            response = null;
            SmartCacheEntry<T> singleData = GetSingleData(request);
            if (singleData == null)
            {
                return false;
            }

            if (singleData.IsExpired())
            {
                bool flag = DeleteEntryInCache(singleData.Id);
                logger.Info(
                    $"EXPIRED: Single [{singleData.Id}] for {request.Host}{request.Path}. Operation succeed: {flag}");
                return false;
            }

            response = Response.Create(request.Id, HttpStatusCode.OK, singleData.OriginalResource, singleData.Header);
            return true;
        }

        private bool StatusCodeNotValid(Response response)
        {
            if (response.StatusCode != HttpStatusCode.OK)
            {
                return response.StatusCode != HttpStatusCode.Created;
            }

            return false;
        }

        private bool IsHeaderCacheControlValid(IHeaderCollection headerCollection)
        {
            if (!IsHeaderCacheValid(headerCollection, "Cache-Control", headerCollection.CacheControl.MaxAge))
            {
                return IsHeaderCacheValid(headerCollection, "X-Avira-Cache-Control",
                    headerCollection.AviraCacheControl.MaxAge);
            }

            return true;
        }

        private bool IsHeaderCacheValid(IHeaderCollection headerCollection, string headerName, TimeSpan? maxAge)
        {
            if (headerCollection.Contains(headerName) && maxAge.HasValue)
            {
                TimeSpan? timeSpan = maxAge;
                TimeSpan timeSpan2 = TimeSpan.FromSeconds(0.0);
                if (!timeSpan.HasValue)
                {
                    return true;
                }

                if (!timeSpan.HasValue)
                {
                    return false;
                }

                return timeSpan.GetValueOrDefault() != timeSpan2;
            }

            return false;
        }

        private List<SmartCacheEntry<T>> GetCollectionData(Request request)
        {
            string filterFromPath = GetFilterFromPath(request.Path);
            if (filterFromPath == null)
            {
                return null;
            }

            if (!RequestWasDoneBefore(request.ResourceLocation.GetHashCode().ToString()))
            {
                logger.Info(
                    $"MISS: Request [{request.Id}] to {request.Host}{request.Path} can not be served from smart cache - it was never done before");
                return null;
            }

            List<SmartCacheEntry<T>> all = cacheDataAccess.GetAll(dataType, dataBaseMapper.GetAll(filterFromPath));
            if (all.Any())
            {
                return all;
            }

            return null;
        }

        private bool RequestWasDoneBefore(string id)
        {
            return cacheDataAccess.Get("__ResourceLocation", id) != null;
        }

        private void MarkRequestAsMade(SmartCacheResourceLocation smartCacheResourceLocation)
        {
            cacheDataAccess.Create("__ResourceLocation", smartCacheResourceLocation);
            logger.Info(
                $"Request {smartCacheResourceLocation.ResourceLocation.Host}{smartCacheResourceLocation.ResourceLocation.Path} marked as already made");
        }

        private SmartCacheEntry<T> GetSingleData(Request request)
        {
            string idFromPath = GetIdFromPath(request.Path);
            if (string.IsNullOrEmpty(idFromPath))
            {
                return null;
            }

            return cacheDataAccess.Get(dataType, dataBaseMapper.Get(idFromPath));
        }

        private void CacheSingle(Response<JsonObjectWrapper> response, string verb)
        {
            SmartCacheEntry<T> dataToStore = dataBaseMapper.MapResponse(response);
            CacheEntry(verb, dataToStore);
        }

        private void CacheEntry(string verb, SmartCacheEntry<T> dataToStore)
        {
            bool flag = false;
            switch (verb)
            {
                case "GET":
                case "POST":
                    cacheDataAccess.Create(dataType, dataToStore);
                    flag = true;
                    break;
                case "PUT":
                    try
                    {
                        if (!cacheDataAccess.Exist<T>(dataType, dataToStore.Id))
                        {
                            cacheDataAccess.Create(dataType, dataToStore);
                            flag = true;
                        }
                        else
                        {
                            flag = cacheDataAccess.Update(dataType, dataToStore);
                        }
                    }
                    finally
                    {
                        if (!flag)
                        {
                            DeleteEntryInCache(dataToStore.Id);
                        }
                    }

                    break;
            }

            if (flag)
            {
                logger.Info($"CACHED: Single {verb} for {dataToStore.Id}");
            }
            else
            {
                logger.Warn($"ERROR CACHING: Single {verb} for {dataToStore.Id}");
            }
        }

        private bool DeleteEntryInCache(string id)
        {
            return cacheDataAccess.Delete(dataType, id);
        }

        private void CacheCollection(CollectionResponse<JsonObjectWrapper> response,
            SmartCacheResourceLocation smartCacheResourceLocation, string verb)
        {
            if (!(verb != "GET"))
            {
                List<SmartCacheEntry<T>> list = dataBaseMapper.MapResponseCollection(response).ToList();
                int num = cacheDataAccess.CreateList(dataType, list);
                logger.Info(
                    $"CACHED: Collection from {smartCacheResourceLocation.ResourceLocation.Host}{smartCacheResourceLocation.ResourceLocation.Path}, created {num} out of {list.Count} records");
                MarkRequestAsMade(smartCacheResourceLocation);
            }
        }

        private string GetDeleteIdForRequest(Request request)
        {
            return GetIdFromPath(request.Path);
        }

        private string GetDeleteIdForNotification(Notification notification)
        {
            return GetIdFromPath(notification.Path);
        }

        private string GetIdFromPath(string originPath)
        {
            if (originPath.Length <= ResourceLocation.Path.Length || originPath.Contains("?filter"))
            {
                return string.Empty;
            }

            if (originPath.Contains("?"))
            {
                int num = originPath.LastIndexOf("/", StringComparison.Ordinal) + 1;
                int num2 = originPath.LastIndexOf("?", StringComparison.Ordinal);
                return originPath.Substring(num, num2 - num);
            }

            return GetSubpath(originPath, '/');
        }

        private string GetFilterFromPath(string originPath)
        {
            if (ResourceLocation.Path.Equals(originPath))
            {
                return string.Empty;
            }

            if (!originPath.Contains("?") || originPath.Length < ResourceLocation.Path.Length)
            {
                return null;
            }

            return GetSubpath(originPath, '?');
        }

        private string GetSubpath(string path, char trim)
        {
            string text = path.Substring(path.LastIndexOf(trim)).Trim(trim);
            if (text.Length != 0)
            {
                return text;
            }

            return null;
        }
    }
}