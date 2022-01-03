using System.Collections.Generic;
using System.Net;
using Avira.Acp.Messages;
using Avira.Acp.Messages.JsonApi;

namespace Avira.Acp.Endpoints
{
    public class RemoteMessageProcessor : RemoteMessageProcessorBase
    {
        private class PendingRequest
        {
            public Request Request { get; }

            public ResponseHandler ResponseHandler { get; }

            public PendingRequest(Request request, ResponseHandler responseHandler)
            {
                Request = request;
                ResponseHandler = responseHandler;
            }
        }

        private readonly List<PendingRequest> pendingRequests = new List<PendingRequest>();

        private readonly object pendingRequestsLock = new object();

        private IAdapter adapter;

        private string remoteHost;

        private string subscriptionIdForSubscriptions;

        private string subscriptionIdForResources;

        private bool initialized;

        public RemoteMessageProcessor(IAcpMessageBroker messageBroker,
            IRemoteResourceRegistrator remoteResourceRegistrator, string localHost)
            : base(messageBroker, remoteResourceRegistrator, localHost)
        {
        }

        public override void Initialize(string newRemoteHost, IAdapter newAdapter)
        {
            remoteHost = newRemoteHost;
            adapter = newAdapter;
            RegisterResourceProxy();
            SubscribeToResourceAndSubscriptionNotifications();
            UpdateRemoteResources();
        }

        protected override void HandleResponse(Response response)
        {
            MessageBroker.HandleResponse(response);
        }

        protected override void HandleNotification(Notification notification)
        {
            MessageBroker.DispatchNotification(notification);
        }

        protected override void HandleRequest(Request request, ResponseHandler responseHandler)
        {
            if (request.Path == "/resources" && request.Host == MessageBroker.HostName && request.Verb == "GET")
            {
                MessageBroker.DispatchRequest(request,
                    delegate(CollectionResponse<ResourceLocation> handler)
                    {
                        InterceptGetResourcesResponse(handler, responseHandler);
                    });
            }
            else if (request.Path == "/subscriptions" && request.Host == MessageBroker.HostName &&
                     request.Verb == "GET")
            {
                MessageBroker.DispatchRequest(request,
                    delegate(CollectionResponse<ResourceLocation> handler)
                    {
                        InterceptGetSubscriptionsResponse(handler, responseHandler);
                    });
            }
            else if (request.Path == "/subscriptions" && request.Host == MessageBroker.HostName &&
                     request.Verb == "POST")
            {
                InterceptCreateSubscriptionRequest(request);
            }
            else if (initialized)
            {
                MessageBroker.DispatchRequest(request, responseHandler);
            }
            else
            {
                lock (pendingRequestsLock)
                {
                    pendingRequests.Add(new PendingRequest(request, responseHandler));
                }
            }
        }

        private void InterceptCreateSubscriptionRequest(Request request)
        {
            Resource<ResourceLocation> payloadData = Request<ResourceLocation>.ConvertFrom(request).PayloadData;
            string subscriptionId = RemoteResourceRegistrator.CreateSubscription(payloadData.Id, payloadData.Attributes,
                delegate(Notification notification)
                {
                    if (notification.Path == "/resources")
                    {
                        Notification<ResourceLocation> notification2 =
                            Notification<ResourceLocation>.ConvertFrom(notification);
                        if ((notification.Verb == "POST" &&
                             RemoteResourceRegistrator.ResourceIsRegistered(notification2.Payload.Data.Attributes)) ||
                            (notification.Verb == "DELETE" &&
                             RemoteResourceRegistrator.ResourceIsRegistered(notification2.Payload.Data.Id)))
                        {
                            return;
                        }
                    }

                    adapter.Send(notification);
                }, remoteHost);
            SendCreateSubscritpionResponse(request, subscriptionId, payloadData);
        }

        private void SendCreateSubscritpionResponse(Request request, string subscriptionId,
            Resource<ResourceLocation> resourceLocationPayload)
        {
            if (!string.IsNullOrEmpty(subscriptionId))
            {
                adapter.Send(Response.Create(request.Id, HttpStatusCode.Created, new Resource<ResourceLocation>
                {
                    Attributes = resourceLocationPayload.Attributes,
                    Id = subscriptionId,
                    Type = "subscriptions"
                }));
            }
            else
            {
                adapter.Send(Response.Create(request.Id, HttpStatusCode.Conflict));
            }
        }

        private void RegisterResourceProxy()
        {
            RemoteResourceRegistrator.RegisterResource(null, new ResourceLocation
            {
                Host = remoteHost,
                Path = "/resources"
            }, adapter.Send, remoteHost);
            RemoteResourceRegistrator.RegisterResource(null, new ResourceLocation
            {
                Host = remoteHost,
                Path = "/subscriptions"
            }, adapter.Send, remoteHost);
        }

        private void SubscribeToResourceAndSubscriptionNotifications()
        {
            subscriptionIdForResources = MessageBroker.CreateSubscription<ResourceLocation>(new ResourceLocation
            {
                Host = remoteHost,
                Path = "/resources"
            }, HandleRemoteResourceRegistrationChangeNotification);
            subscriptionIdForSubscriptions = MessageBroker.CreateSubscription<ResourceLocation>(new ResourceLocation
            {
                Host = remoteHost,
                Path = "/subscriptions"
            }, HandleRemoteSubscriptionsChangeNotification);
        }

        private void InterceptGetSubscriptionsResponse(CollectionResponse<ResourceLocation> response,
            ResponseHandler responseHandler)
        {
            RemoteResourceRegistrator.RemoveRemoteSubscriptionLocations(response);
            responseHandler(response);
        }

        private void InterceptGetResourcesResponse(CollectionResponse<ResourceLocation> response,
            ResponseHandler responseHandler)
        {
            RemoteResourceRegistrator.RemoveRemoteResourceLocations(response);
            responseHandler(response);
        }

        private void UpdateRemoteResources()
        {
            SubscribeToRemoteResources();
            RequestRemoteResources();
        }

        private void SubscribeToRemoteResources()
        {
            Request<ResourceLocation> message = Request.Create("POST", remoteHost, "/subscriptions",
                new Resource<ResourceLocation>
                {
                    Type = "subscriptions",
                    Attributes = new ResourceLocation
                    {
                        Host = remoteHost,
                        Path = "/resources"
                    }
                });
            MessageBroker.DispatchRequest(message, delegate { });
        }

        private void RequestRemoteResources()
        {
            Request message = Request.Create("GET", remoteHost, "/resources");
            MessageBroker.DispatchRequest<ResourceLocation>(message, HandleRemoteResourcesResponse);
        }

        private void UpdateRemoteSubscriptions()
        {
            SubscribeToRemoteSubscriptions();
            RequestRemoteSubscriptions();
        }

        private void RequestRemoteSubscriptions()
        {
            Request message = Request.Create("GET", remoteHost, "/subscriptions");
            MessageBroker.DispatchRequest<ResourceLocation>(message, HandleRemoteSubscriptionResponse);
        }

        private void SubscribeToRemoteSubscriptions()
        {
            Request<ResourceLocation> message = Request.Create("POST", remoteHost, "/subscriptions",
                new Resource<ResourceLocation>
                {
                    Type = "subscriptions",
                    Attributes = new ResourceLocation
                    {
                        Host = remoteHost,
                        Path = "/subscriptions"
                    }
                });
            MessageBroker.DispatchRequest(message, delegate { });
        }

        public override void UnregisterRemoteResources()
        {
            UnregisterRemoteResources(remoteHost);
            MessageBroker.RemoveSubscription(subscriptionIdForResources, remoteHost);
            MessageBroker.RemoveSubscription(subscriptionIdForSubscriptions, remoteHost);
        }

        private void HandleRemoteResourcesResponse(CollectionResponse<ResourceLocation> resourceRegistrationResponse)
        {
            if (resourceRegistrationResponse.Payload?.Data != null)
            {
                foreach (Resource<ResourceLocation> datum in resourceRegistrationResponse.Payload.Data)
                {
                    RemoteResourceRegistrator.RegisterResource(datum.Id, datum.Attributes, adapter.Send, remoteHost);
                }
            }

            UpdateRemoteSubscriptions();
        }

        private void HandleRemoteSubscriptionResponse(CollectionResponse<ResourceLocation> response)
        {
            if (response.Payload?.Data == null)
            {
                initialized = true;
                ProcessPendingRequests();
                return;
            }

            foreach (Resource<ResourceLocation> datum in response.Payload.Data)
            {
                RemoteResourceRegistrator.CreateSubscription(datum.Id, datum.Attributes, adapter.Send, remoteHost);
            }

            initialized = true;
            ProcessPendingRequests();
        }

        private void HandleRemoteSubscriptionsChangeNotification(Notification<ResourceLocation> notification)
        {
            string verb = notification.Verb;
            if (!(verb == "POST"))
            {
                if (verb == "DELETE")
                {
                    RemoteResourceRegistrator.RemoveSubscription(notification.Payload.Data.Id, notification.Sender);
                }
            }
            else
            {
                RemoteResourceRegistrator.CreateSubscription(notification.Payload.Data.Id,
                    notification.Payload.Data.Attributes, adapter.Send, notification.Sender);
            }
        }

        private void HandleRemoteResourceRegistrationChangeNotification(Notification<ResourceLocation> notification)
        {
            string verb = notification.Verb;
            if (!(verb == "POST"))
            {
                if (verb == "DELETE")
                {
                    RemoteResourceRegistrator.UnregisterResource(notification.Payload.Data.Id, notification.Sender);
                }
            }
            else
            {
                RemoteResourceRegistrator.RegisterResource(notification.Payload.Data.Id,
                    notification.Payload.Data.Attributes, adapter.Send, notification.Sender);
            }
        }

        private void ProcessPendingRequests()
        {
            lock (pendingRequestsLock)
            {
                foreach (PendingRequest pendingRequest in pendingRequests)
                {
                    MessageBroker.DispatchRequest(pendingRequest.Request, pendingRequest.ResponseHandler);
                }

                pendingRequests.Clear();
            }
        }
    }
}