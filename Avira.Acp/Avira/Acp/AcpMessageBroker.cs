using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using Avira.Acp.Logging;
using Avira.Acp.Messages;
using Avira.Acp.Messages.JsonApi;

namespace Avira.Acp
{
    public class AcpMessageBroker : IAcpMessageBroker
    {
        private readonly ILogger logger = LoggerFacade.GetCurrentClassLogger();

        private readonly IResourceHandlersMap resourceHandlersMap;

        private readonly IRegisteredSubscriptionHandlers registeredSubscriptionHandlers;

        private readonly IResponseMessageHandler responseMessageHandler;

        private readonly IProxy proxy;

        private readonly ResourceProvider<ResourceLocation> resourceLocationResourceProvider;

        private readonly ResourceProvider<ResourceLocation> subscriptionsResourceProvider;

        private IResourceNotFoundBehaviour resourceNotFoundBehaviour;

        public string HostName { get; }

        public IResourceNotFoundBehaviour ResourceNotFoundBehaviour
        {
            get { return resourceNotFoundBehaviour; }
            set
            {
                if (this.resourceNotFoundBehaviour != value)
                {
                    IResourceNotFoundBehaviour resourceNotFoundBehaviour = this.resourceNotFoundBehaviour;
                    this.resourceNotFoundBehaviour = value;
                    if (resourceNotFoundBehaviour != null && value != null)
                    {
                        resourceNotFoundBehaviour.GetUnprocessedRequests().ForEach(value.ProcessUnhandledRequest);
                    }
                }
            }
        }

        public AcpMessageBroker(string hostName)
            : this(hostName, new TransparentProxy())
        {
        }

        public AcpMessageBroker(string hostName, IProxy proxy)
            : this(hostName, new ResourceHandlersMap(), new RegisteredSubscriptionHandlers(),
                new ResponseMessageHandler(), proxy)
        {
        }

        internal AcpMessageBroker(string hostName, IResourceHandlersMap resourceHandlersMap,
            IRegisteredSubscriptionHandlers registeredSubscriptionHandlers,
            IResponseMessageHandler responseMessageHandler)
            : this(hostName, resourceHandlersMap, registeredSubscriptionHandlers, responseMessageHandler,
                new TransparentProxy())
        {
        }

        internal AcpMessageBroker(string hostName, IResourceHandlersMap resourceHandlersMap,
            IRegisteredSubscriptionHandlers registeredSubscriptionHandlers,
            IResponseMessageHandler responseMessageHandler, IProxy proxy)
        {
            this.resourceHandlersMap = resourceHandlersMap;
            this.registeredSubscriptionHandlers = registeredSubscriptionHandlers;
            this.responseMessageHandler = responseMessageHandler;
            this.proxy = proxy;
            resourceLocationResourceProvider = new ResourceProvider<ResourceLocation>(resourceHandlersMap,
                new ResourceLocation
                {
                    Host = hostName,
                    Path = "/resources"
                }, this);
            subscriptionsResourceProvider = new ResourceProvider<ResourceLocation>(registeredSubscriptionHandlers,
                new ResourceLocation
                {
                    Host = hostName,
                    Path = "/subscriptions"
                }, this);
            HostName = hostName;
            ResourceNotFoundBehaviour = new RespondNotFoundBehavior();
            RegisterInternalResources();
        }

        public void DispatchRequest<T>(Request message, ResponseHandler<T> responseHandler)
        {
            DispatchRequest(message,
                delegate(Response response) { responseHandler(Response<T>.ConvertFrom(response)); });
        }

        public void DispatchRequest<T>(Request message, CollectionResponseHandler<T> responseHandler)
        {
            DispatchRequest(message,
                delegate(Response response) { responseHandler(CollectionResponse<T>.ConvertFrom(response)); });
        }

        public void DispatchRequest(Request message, ResponseHandler responseHandler)
        {
            ThreadPool.QueueUserWorkItem(delegate { HandleRequest(message, responseHandler); });
        }

        public string RegisterResource<T>(ResourceLocation resourceLocation, RequestHandler<T> requestHandler)
        {
            return RegisterResource(resourceLocation,
                delegate(Request request) { requestHandler(Request<T>.ConvertFrom(request)); });
        }

        public string RegisterResource<T>(ResourceLocation resourceLocation, CollectionRequestHandler<T> handler)
        {
            return RegisterResource(resourceLocation,
                delegate(Request request) { handler(CollectionRequest<T>.ConvertFrom(request)); });
        }

        public string RegisterResource(ResourceLocation resourceLocation, RequestHandler requestHandler)
        {
            return RegisterResource(resourceLocation, requestHandler, string.Empty);
        }

        public string RegisterResource(ResourceLocation resourceLocation, RequestHandler requestHandler, string owner)
        {
            string result = resourceHandlersMap.Add(resourceLocation, requestHandler, owner);
            resourceNotFoundBehaviour.OnResourceRegistered(resourceLocation, requestHandler);
            return result;
        }

        public string RegisterResourceProviderSubstitute(ResourceLocation resourceLocation,
            IResourceProvider providerSubstitute)
        {
            return resourceHandlersMap.AddSubstitute(resourceLocation, providerSubstitute);
        }

        public bool UnregisterResourceProviderSubstitute(ResourceLocation resourceLocation)
        {
            return resourceHandlersMap.RemoveSubstitute(resourceLocation);
        }

        public bool UnregisterResource(string resourceId)
        {
            return UnregisterResource(resourceId, string.Empty);
        }

        public bool UnregisterResource(string resourceId, string owner)
        {
            return resourceHandlersMap.Remove(resourceId, owner);
        }

        public string CreateSubscription<T>(ResourceLocation resourceLocation,
            NotificationHandler<T> notificationHandler)
        {
            return CreateSubscription(resourceLocation,
                delegate(Notification notificationMessage)
                {
                    notificationHandler(Notification<T>.ConvertFrom(notificationMessage));
                });
        }

        public string CreateSubscription(ResourceLocation resourceLocation, NotificationHandler notificationHandler)
        {
            return CreateSubscription(resourceLocation, notificationHandler, HostName);
        }

        public string CreateSubscription(ResourceLocation resourceLocation, NotificationHandler notificationHandler,
            string owner)
        {
            return registeredSubscriptionHandlers.Add(resourceLocation, notificationHandler, owner);
        }

        public bool RemoveSubscription(string subscriptionId)
        {
            return RemoveSubscription(subscriptionId, string.Empty);
        }

        public bool RemoveSubscription(string subscriptionId, string owner)
        {
            return registeredSubscriptionHandlers.Remove(subscriptionId, owner);
        }

        public void HandleResponse(Response message)
        {
            try
            {
                Request request = responseMessageHandler.GetRequest(message);
                proxy.InterceptResponse(message, request, HandleResponseInThreadPool);
            }
            catch (Exception ex)
            {
                logger.Warn("Handling response message failed (message id: {0}). {1}", message.Id, ex);
            }
        }

        private void HandleResponseInThreadPool(Response response)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    responseMessageHandler.HandleResponse(response);
                }
                catch (Exception ex)
                {
                    logger.Warn("Handling response failed (message id: {0}). {1}", response.Id, ex);
                }
            });
        }

        public void DispatchNotification(Notification notification)
        {
            proxy.InterceptNotification(notification, DispatchNotificationInternal);
        }

        public void DispatchNotification(Notification notification, string excludedOwner)
        {
            proxy.InterceptNotification(notification,
                delegate(Notification n) { DispatchNotificationInternal(n, excludedOwner); });
        }

        private void DispatchNotificationInternal(Notification notification, string excludedOwner)
        {
            foreach (NotificationHandler item in registeredSubscriptionHandlers.Get(
                         new ResourceLocation(notification.Sender, notification.Path), excludedOwner))
            {
                item(notification);
            }
        }

        private void DispatchNotificationInternal(Notification notification)
        {
            foreach (NotificationHandler item in registeredSubscriptionHandlers.Get(
                         new ResourceLocation(notification.Sender, notification.Path)))
            {
                item(notification);
            }
        }

        public bool HasSubscribers(ResourceLocation resourceLocation)
        {
            return registeredSubscriptionHandlers.Get(resourceLocation).Any();
        }

        public bool IsResourceRegistered(ResourceLocation resourceLocation)
        {
            return resourceHandlersMap.IsResourceRegistered(resourceLocation);
        }

        public ICollection<ResourceLocation> GetRegisteredResourceLocations()
        {
            return resourceHandlersMap.GetAllResourceLocations();
        }

        private void RegisterInternalResources()
        {
            RegisterResource(resourceLocationResourceProvider.ResourceLocation,
                resourceLocationResourceProvider.HandleMessage);
            RegisterResource(subscriptionsResourceProvider.ResourceLocation,
                subscriptionsResourceProvider.HandleMessage);
        }

        private void HandleRequest(Request request, ResponseHandler responseHandler)
        {
            if (request.Id != null)
            {
                string host = (request.TargetsLocalHost() ? HostName : request.Host);
                ResourceLocation resourceLocation = new ResourceLocation
                {
                    Host = host,
                    Path = request.Path
                };
                RequestHandler requestHandler = resourceHandlersMap.Get(resourceLocation);
                if (requestHandler == null)
                {
                    resourceNotFoundBehaviour.ProcessUnhandledRequest(new RequestCallInfo(resourceLocation, request,
                        responseHandler));
                }
                else
                {
                    HandleRequest(request, responseHandler, requestHandler);
                }
            }
        }

        private void HandleRequest(Request request, ResponseHandler responseHandler, RequestHandler requestHandler)
        {
            try
            {
                Request request2 = responseMessageHandler.RegisterRequestMessage(request, responseHandler);
                proxy.InterceptRequest(request2, requestHandler, responseMessageHandler.HandleResponse);
            }
            catch (Exception ex)
            {
                logger.Warn("Dispatching request failed. {0}", ex);
                try
                {
                    responseHandler(Response.Create(request.Id, HttpStatusCode.InternalServerError));
                }
                catch (Exception ex2)
                {
                    logger.Warn("Dispatching error response failed. {0}", ex2);
                }
            }
        }
    }
}