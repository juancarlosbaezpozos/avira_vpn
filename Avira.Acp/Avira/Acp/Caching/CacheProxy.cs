using System.Collections.Generic;
using System.Linq;
using System.Net;
using Avira.Acp.Caching.Configuration;
using Avira.Acp.Extensions;
using Avira.Acp.Logging;
using Avira.Acp.Messages;

namespace Avira.Acp.Caching
{
    public class CacheProxy : IProxy
    {
        private const string XCacheHeaderName = "X-Cache";

        private readonly IConfiguration configuration;

        private readonly List<Request> pendingRequests = new List<Request>();

        private readonly ICache cache;

        private readonly string hostName;

        private readonly ILogger logger = LoggerFacade.GetCurrentClassLogger();

        public CacheProxy(IConfiguration configuration, ICache cache, string hostName)
        {
            this.hostName = hostName;
            this.configuration = configuration;
            this.cache = cache;
        }

        public void InterceptRequest(Request request, RequestHandler requestHandler, ResponseHandler responseHandler)
        {
            if (RequestShouldBypassCache(request))
            {
                requestHandler(request);
            }
            else
            {
                if (HandleRequestFromCache(request, responseHandler))
                {
                    return;
                }

                if (request.Headers.CacheControl.OnlyIfCached)
                {
                    responseHandler(Response.Create(request.Id, HttpStatusCode.GatewayTimeout));
                    return;
                }

                bool flag;
                lock (pendingRequests)
                {
                    flag = !RequestToSameLocationIsPending(request);
                    pendingRequests.Add(request);
                }

                if (flag)
                {
                    requestHandler(request);
                }
            }
        }

        public void InterceptResponse(Response response, Request request, ResponseHandler responseHandler)
        {
            InterceptResponse(response, responseHandler);
        }

        public void InterceptResponse(Response response, ResponseHandler responseHandler)
        {
            if (!TryTakePendingRequests(response, out var requests))
            {
                responseHandler(response);
                return;
            }

            foreach (Request item in requests)
            {
                Response response2 = Response.Clone(response);
                if (item.Id == response.Id)
                {
                    if (ResponseShouldBeCached(response))
                    {
                        cache.Add(item.ResourceLocation, response);
                    }

                    AddCacheMissHeader(response2);
                }
                else
                {
                    LogCacheHit(item, response2);
                    AddCacheHitHeader(response2);
                }

                response2.Id = item.Id;
                responseHandler(response2);
            }
        }

        private static bool ResponseShouldBeCached(Response response)
        {
            if (response.StatusCode.IsSuccess())
            {
                return !response.Headers.CacheControl.NoCache;
            }

            return false;
        }

        private void LogCacheHit(Request request, Response clonedResponse)
        {
            string arg =
                AcpMessageFormatter.RemoveTokenInformation(AcpMessageSerializer.Instance.SerializeToJson(request));
            string arg2 =
                AcpMessageFormatter.RemoveTokenInformation(
                    AcpMessageSerializer.Instance.SerializeToJson(clonedResponse));
            logger.Info($"ACP cache request hit: {arg}");
            logger.Info($"ACP cache response:    {arg2}");
        }

        public void InterceptNotification(Notification notification, NotificationHandler notificationHandler)
        {
            cache.Clear(new ResourceLocation(notification.Sender, notification.Path));
            notificationHandler(notification);
        }

        private bool RequestToSameLocationIsPending(Request request)
        {
            return pendingRequests.Any((Request r) => RequestsPointToSameLocation(r, request));
        }

        private bool RequestsPointToSameLocation(Request firstRequest, Request secondRequest)
        {
            return firstRequest.ResourceLocation == secondRequest.ResourceLocation;
        }

        private bool RequestShouldBypassCache(Request request)
        {
            if (request.Verb != "GET")
            {
                return true;
            }

            ResourceConfiguration resourceConfiguration =
                configuration.GetResourceConfiguration(request.ResourceLocation);
            if (resourceConfiguration != null &&
                resourceConfiguration.CacheLevel != ResourceCacheLevel.BypassLocalCache)
            {
                return request.Headers.CacheControl.NoCache;
            }

            return true;
        }

        private bool HandleRequestFromCache(Request request, ResponseHandler responseHandler)
        {
            if (!cache.Get(request, out var response))
            {
                return false;
            }

            if (response != null)
            {
                LogCacheHit(request, response);
                AddCacheHitHeader(response);
            }

            responseHandler(response);
            return true;
        }

        private void AddCacheHitHeader(Response response)
        {
            response.Headers.Append("X-Cache", $"HIT from {hostName}");
        }

        private void AddCacheMissHeader(Response response)
        {
            response.Headers.Append("X-Cache", $"MISS from {hostName}");
        }

        private bool TryTakePendingRequests(Response response, out List<Request> requests)
        {
            lock (pendingRequests)
            {
                Request originalRequest = pendingRequests.FirstOrDefault((Request r) => r.Id == response.Id);
                if (originalRequest == null)
                {
                    requests = new List<Request>();
                    return false;
                }

                requests = pendingRequests.Where((Request pendingRequest) => pendingRequest.Id == response.Id ||
                                                                             RequestsPointToSameLocation(pendingRequest,
                                                                                 originalRequest)).ToList();
                foreach (Request request in requests)
                {
                    pendingRequests.Remove(request);
                }
            }

            return true;
        }
    }
}