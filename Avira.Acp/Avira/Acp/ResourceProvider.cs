using System;
using System.Net;
using Avira.Acp.Logging;
using Avira.Acp.Messages;
using Avira.Acp.Messages.JsonApi;

namespace Avira.Acp
{
    public class ResourceProvider<T> : BaseResourceProvider<T> where T : class
    {
        private readonly IResourceRepository<T> repository;

        private readonly ILogger logger = LoggerFacade.GetCurrentClassLogger();

        public ResourceProvider(IResourceRepository<T> repository, ResourceLocation resourceLocation,
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
            Response message = request.Verb switch
            {
                "GET" => TryExecute(request, () => HandleGetRequest(id, filter, request)),
                "PUT" => TryExecute(request, () => HandlePutRequest(id, filter, request)),
                "DELETE" => TryExecute(request, () => HandleDeleteRequest(id, filter, request)),
                "POST" => TryExecute(request, () => HandlePostRequest(request)),
                _ => Response.Create(request.Id, HttpStatusCode.MethodNotAllowed),
            };
            MessageBroker.HandleResponse(message);
        }

        private Response TryExecute(Request request, Func<Response> requestHandler)
        {
            try
            {
                return requestHandler();
            }
            catch (ResourceOperationException ex)
            {
                logger.Warn("Request failed {0}, Error: {1}",
                    AcpMessageFormatter.RemoveTokenInformation(AcpMessageSerializer.Instance.SerializeToJson(request)),
                    ex);
                SingleResourceDocument<T> data = new SingleResourceDocument<T>
                {
                    Errors = ex.Errors
                };
                return Response.Create(request.Id, ex.HttpStatusCode, data);
            }
            catch (Exception ex2)
            {
                logger.Warn("Request failed {0}, Exception: {1}",
                    AcpMessageFormatter.RemoveTokenInformation(AcpMessageSerializer.Instance.SerializeToJson(request)),
                    ex2);
                return Response.Create(request.Id, HttpStatusCode.InternalServerError);
            }
        }

        private Response HandlePutRequest(string id, string filter, Request request)
        {
            Resource<T> payloadData = request.GetPayloadData<T>(request);
            if (string.IsNullOrEmpty(payloadData?.Type) || string.IsNullOrEmpty(payloadData.Id))
            {
                return Response.Create(request.Id, HttpStatusCode.BadRequest);
            }

            if (string.IsNullOrEmpty(id))
            {
                return Response.CreateCollection(request.Id, HttpStatusCode.OK,
                    repository.UpdateAll(filter, payloadData));
            }

            Resource<T> data = repository.Update(id, payloadData);
            return Response.Create(request.Id, HttpStatusCode.OK, data);
        }

        private Response HandlePostRequest(Request request)
        {
            Resource<T> payloadData = request.GetPayloadData<T>(request);
            if (string.IsNullOrEmpty(payloadData?.Type))
            {
                return Response.Create(request.Id, HttpStatusCode.BadRequest);
            }

            Resource<T> data = repository.Create(payloadData);
            return Response.Create(request.Id, HttpStatusCode.Created, data);
        }

        private Response HandleGetRequest(string id, string filter, Request request)
        {
            if (string.IsNullOrEmpty(id))
            {
                return Response.CreateCollection(request.Id, HttpStatusCode.OK, repository.ReadAll(filter));
            }

            Resource<T> resource = repository.Read(id);
            if (resource == null)
            {
                return Response.Create(request.Id, HttpStatusCode.NotFound);
            }

            return Response.Create(request.Id, HttpStatusCode.OK, resource);
        }

        private Response HandleDeleteRequest(string id, string filter, Request request)
        {
            if (string.IsNullOrEmpty(id))
            {
                repository.DeleteAll(filter);
            }
            else
            {
                repository.Delete(id);
            }

            return Response.Create(request.Id, HttpStatusCode.NoContent);
        }
    }
}