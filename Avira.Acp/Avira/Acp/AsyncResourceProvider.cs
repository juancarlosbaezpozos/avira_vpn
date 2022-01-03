using System;
using System.Collections.Generic;
using System.Net;
using Avira.Acp.Logging;
using Avira.Acp.Messages;
using Avira.Acp.Messages.JsonApi;

namespace Avira.Acp
{
    public class AsyncResourceProvider<T> : BaseResourceProvider<T> where T : class
    {
        private readonly IResourceRepositoryAsync<T> repository;

        private readonly ILogger logger = LoggerFacade.GetCurrentClassLogger();

        public AsyncResourceProvider(IResourceRepositoryAsync<T> repository, ResourceLocation resourceLocation,
            IAcpMessageBroker messageBroker)
            : base(resourceLocation, messageBroker)
        {
            this.repository = repository;
            this.repository.Created += base.RepositoryOnCreated;
            this.repository.Deleted += base.RepositoryOnDeleted;
            this.repository.Updated += base.RepositoryOnUpdated;
        }

        public override void HandleMessage(Request request)
        {
            string id = GetId(request.Path);
            string filter = GetFilter(request.Path);
            switch (request.Verb)
            {
                case "GET":
                    TryExecute(request,
                        delegate
                        {
                            HandleGetRequestAsync(id, filter, request,
                                delegate(Exception ex) { HandleRequestException(request, ex); });
                        });
                    break;
                case "PUT":
                    TryExecute(request,
                        delegate
                        {
                            HandlePutRequestAsync(id, filter, request,
                                delegate(Exception ex) { HandleRequestException(request, ex); });
                        });
                    break;
                case "DELETE":
                    TryExecute(request, delegate { HandleDeleteRequest(id, filter, request); });
                    break;
                case "POST":
                    TryExecute(request,
                        delegate
                        {
                            HandlePostRequestAsync(request,
                                delegate(Exception ex) { HandleRequestException(request, ex); });
                        });
                    break;
                default:
                    MessageBroker.HandleResponse(Response.Create(request.Id, HttpStatusCode.MethodNotAllowed));
                    break;
            }
        }

        private void HandleRequestException(Request request, Exception exception)
        {
            ResourceOperationException ex = exception as ResourceOperationException;
            if (ex != null)
            {
                logger.Warn("Request failed {0}, Error: {1}",
                    AcpMessageFormatter.RemoveTokenInformation(AcpMessageSerializer.Instance.SerializeToJson(request)),
                    ex);
                SingleResourceDocument<T> data = new SingleResourceDocument<T>
                {
                    Errors = ex.Errors
                };
                MessageBroker.HandleResponse(Response.Create(request.Id, ex.HttpStatusCode, data));
            }
            else
            {
                logger.Warn("Request failed {0}, Exception: {1}",
                    AcpMessageFormatter.RemoveTokenInformation(AcpMessageSerializer.Instance.SerializeToJson(request)),
                    exception);
                MessageBroker.HandleResponse(Response.Create(request.Id, HttpStatusCode.InternalServerError));
            }
        }

        private void TryExecute(Request request, Action requestHandler)
        {
            try
            {
                requestHandler();
            }
            catch (Exception exception)
            {
                HandleRequestException(request, exception);
            }
        }

        private void HandlePutRequestAsync(string id, string filter, Request request, Action<Exception> errorCallback)
        {
            Resource<T> payloadData = request.GetPayloadData<T>(request);
            if (string.IsNullOrEmpty(payloadData?.Type) || string.IsNullOrEmpty(payloadData.Id))
            {
                MessageBroker.HandleResponse(Response.Create(request.Id, HttpStatusCode.BadRequest));
            }
            else if (string.IsNullOrEmpty(id))
            {
                repository.UpdateAll(filter, payloadData,
                    delegate(List<Resource<T>> response)
                    {
                        MessageBroker.HandleResponse(Response.CreateCollection(request.Id, HttpStatusCode.OK,
                            response));
                    }, errorCallback);
            }
            else
            {
                repository.Update(id, payloadData,
                    delegate(Resource<T> response)
                    {
                        MessageBroker.HandleResponse(Response.Create(request.Id, HttpStatusCode.OK, response));
                    }, errorCallback);
            }
        }

        private void HandlePostRequestAsync(Request request, Action<Exception> errorCallback)
        {
            Resource<T> payloadData = request.GetPayloadData<T>(request);
            if (string.IsNullOrEmpty(payloadData?.Type))
            {
                MessageBroker.HandleResponse(Response.Create(request.Id, HttpStatusCode.BadRequest));
                return;
            }

            repository.Create(payloadData,
                delegate(Resource<T> response)
                {
                    MessageBroker.HandleResponse(Response.Create(request.Id, HttpStatusCode.Created, response));
                }, errorCallback);
        }

        private void HandleGetRequestAsync(string id, string filter, Request request, Action<Exception> errorCallback)
        {
            if (string.IsNullOrEmpty(id))
            {
                repository.ReadAll(filter,
                    delegate(List<Resource<T>> response)
                    {
                        MessageBroker.HandleResponse(Response.CreateCollection(request.Id, HttpStatusCode.OK,
                            response));
                    }, errorCallback);
                return;
            }

            repository.Read(id, delegate(Resource<T> response)
            {
                Response message = ((response != null)
                    ? Response.Create(request.Id, HttpStatusCode.OK, response)
                    : Response.Create(request.Id, HttpStatusCode.NotFound));
                MessageBroker.HandleResponse(message);
            }, errorCallback);
        }

        private void HandleDeleteRequest(string id, string filter, Request request)
        {
            if (string.IsNullOrEmpty(id))
            {
                repository.DeleteAll(filter);
            }
            else
            {
                repository.Delete(id);
            }

            MessageBroker.HandleResponse(Response.Create(request.Id, HttpStatusCode.NoContent));
        }
    }
}