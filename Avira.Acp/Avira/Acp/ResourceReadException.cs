using System;
using System.Linq;
using System.Net;
using Avira.Acp.Messages.JsonApi;

namespace Avira.Acp
{
    [Serializable]
    public class ResourceReadException : ResourceOperationException
    {
        public ResourceReadException(HttpStatusCode statusCode, params Error[] errors)
            : base($"Reading resource(s) failed ({statusCode}).", statusCode, errors.ToList())
        {
        }
    }
}