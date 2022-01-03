using System;
using System.Linq;
using System.Net;
using Avira.Acp.Messages.JsonApi;

namespace Avira.Acp
{
    [Serializable]
    public class ResourceDeleteException : ResourceOperationException
    {
        public ResourceDeleteException(HttpStatusCode statusCode, params Error[] errors)
            : base($"Deleting resource(s) failed ({statusCode}).", statusCode, errors.ToList())
        {
        }
    }
}