using System;
using Avira.Acp.Logging;
using Avira.Acp.Messages;

namespace Avira.Acp.Caching.SmartCache
{
    public class SmartCacheProxy : IProxy
    {
        private readonly ILogger logger = LoggerFacade.GetCurrentClassLogger();

        private readonly IProxy cacheProxy;

        private readonly ISmartCacheFactory smartCacheFactory;

        public SmartCacheProxy(IProxy cacheProxy, ISmartCacheFactory smartCacheFactory)
        {
            this.cacheProxy = cacheProxy;
            this.smartCacheFactory = smartCacheFactory;
        }

        public void InterceptRequest(Request request, RequestHandler requestHandler, ResponseHandler responseHandler)
        {
            Response response = null;
            bool flag = false;
            ISmartCacheLogic byPath = smartCacheFactory.GetByPath(request.Host, request.Path);
            try
            {
                if (byPath != null)
                {
                    flag = byPath.TryGetDataFromCache(request, out response);
                }
            }
            catch (Exception ex)
            {
                logger.Warn("Getting response from SmartCache failed. {0}", ex);
            }

            if (flag)
            {
                responseHandler(response);
            }
            else
            {
                cacheProxy.InterceptRequest(request, requestHandler,
                    SmartCacheResponseHandler(request, responseHandler));
            }
        }

        public void InterceptResponse(Response response, Request request, ResponseHandler responseHandler)
        {
            cacheProxy.InterceptResponse(response, request, SmartCacheResponseHandler(request, responseHandler));
        }

        private ResponseHandler SmartCacheResponseHandler(Request request, ResponseHandler responseHandler)
        {
            return delegate(Response response)
            {
                if (request != null)
                {
                    try
                    {
                        smartCacheFactory.GetByPath(request.Host, request.Path)?.Cache(Response.Clone(response),
                            request.Verb, request.Host, request.Path);
                    }
                    catch (Exception ex)
                    {
                        logger.Warn("Handling response from cache failed (message id: {0}). {1}", response.Id, ex);
                    }
                }

                responseHandler(response);
            };
        }

        public void InterceptNotification(Notification notification, NotificationHandler notificationHandler)
        {
            ISmartCacheLogic byPath = smartCacheFactory.GetByPath(notification.Sender, notification.Path);
            try
            {
                byPath?.Cache(Notification.Clone(notification));
            }
            catch (Exception ex)
            {
                logger.Warn("Handling notification with smart cache failed. (notification: {0}) {1}",
                    notification.ToString(), ex);
            }

            cacheProxy.InterceptNotification(notification, notificationHandler);
        }
    }
}