using System;
using System.Collections.Generic;
using System.Linq;
using Avira.Acp.Logging;
using Avira.Acp.Messages.JsonApi;

namespace Avira.Acp.Endpoints
{
    public class EndpointRepository : BaseResourceRepository<IEndpoint>, IEndpointRepository
    {
        private const string ResourceType = "endpoints";

        private readonly Dictionary<string, IEndpoint> endpoints = new Dictionary<string, IEndpoint>();

        private readonly ILogger logger = LoggerFacade.GetCurrentClassLogger();

        public string Add(IEndpoint endpoint)
        {
            logger.Info($"Adding endpoint. Host: {endpoint.Host}.");
            string deletedEndpointId;
            string text;
            lock (endpoints)
            {
                Remove(endpoint, out deletedEndpointId);
                text = UniqueIdProvider.Get();
                endpoints.Add(text, endpoint);
            }

            if (deletedEndpointId != null)
            {
                OnDeleted(new DeletedEventArgs(deletedEndpointId, "endpoints"));
            }

            OnCreated(new CreatedEventArgs<IEndpoint>(endpoint, text, "endpoints"));
            return text;
        }

        public void Remove(IEndpoint endpoint)
        {
            string deletedEndpointId;
            lock (endpoints)
            {
                Remove(endpoint, out deletedEndpointId);
            }

            if (deletedEndpointId != null)
            {
                OnDeleted(new DeletedEventArgs(deletedEndpointId, "endpoints"));
            }
        }

        private void Remove(IEndpoint endpoint, out string deletedEndpointId)
        {
            deletedEndpointId = null;
            if (!TryGetEndpointByHost(endpoint.Host, out var endpointKeyValuePair))
            {
                return;
            }

            if (endpointKeyValuePair.Value != endpoint)
            {
                try
                {
                    logger.Info($"Remove endpoint. Host: {endpointKeyValuePair.Value.Host}.");
                    endpointKeyValuePair.Value.Close();
                }
                catch (Exception ex)
                {
                    logger.Warn($"Failed to close enpoint {endpointKeyValuePair.Value.Host}. {ex.Message}");
                }
            }

            endpoints.Remove(endpointKeyValuePair.Key);
            deletedEndpointId = endpointKeyValuePair.Key;
        }

        public override Resource<IEndpoint> Read(string id)
        {
            IEndpoint value;
            lock (endpoints)
            {
                if (!endpoints.TryGetValue(id, out value))
                {
                    return null;
                }
            }

            return new Resource<IEndpoint>
            {
                Attributes = value,
                Id = id,
                Type = "endpoints"
            };
        }

        public override List<Resource<IEndpoint>> ReadAll(string filter)
        {
            lock (endpoints)
            {
                return endpoints.Select((KeyValuePair<string, IEndpoint> acpEndpointKeyValue) => new Resource<IEndpoint>
                {
                    Attributes = acpEndpointKeyValue.Value,
                    Id = acpEndpointKeyValue.Key,
                    Type = "endpoints"
                }).ToList();
            }
        }

        private bool TryGetEndpointByHost(string host, out KeyValuePair<string, IEndpoint> endpointKeyValuePair)
        {
            lock (endpoints)
            {
                endpointKeyValuePair =
                    endpoints.SingleOrDefault((KeyValuePair<string, IEndpoint> e) => e.Value.Host == host);
            }

            return !endpointKeyValuePair.Equals(default(KeyValuePair<string, IEndpoint>));
        }
    }
}