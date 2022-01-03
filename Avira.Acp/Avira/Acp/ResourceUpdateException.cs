using System;
using System.Linq;
using System.Net;
using Avira.Acp.Messages.JsonApi;

namespace Avira.Acp
{
    [Serializable]
    public class ResourceUpdateException : ResourceOperationException
    {
        public ResourceUpdateException(HttpStatusCode statusCode, params Error[] errors)
            : base($"Updating resource(s) failed ({statusCode}).", statusCode, errors.ToList())
        {
        }
    }
}