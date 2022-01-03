using System;
using System.Linq;
using System.Net;
using Avira.Acp.Messages.JsonApi;

namespace Avira.Acp
{
    [Serializable]
    public class ResourceCreateException : ResourceOperationException
    {
        public ResourceCreateException(HttpStatusCode statusCode, params Error[] errors)
            : base($"Creating resource failed ({statusCode}).", statusCode, errors.ToList())
        {
        }
    }
}