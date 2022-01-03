using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.Serialization;
using System.Security.Permissions;
using Avira.Acp.Messages.JsonApi;
using ServiceStack.Text;

namespace Avira.Acp
{
    [Serializable]
    public abstract class ResourceOperationException : Exception
    {
        public List<Error> Errors { get; private set; }

        public HttpStatusCode HttpStatusCode { get; private set; }

        protected ResourceOperationException(string message, HttpStatusCode httpStatusCode, List<Error> messageErrors)
            : base(message)
        {
            Errors = messageErrors;
            HttpStatusCode = httpStatusCode;
        }

        public override string ToString()
        {
            string arg = JsonSerializer.SerializeToString(Errors);
            return $"{base.ToString()}\nErrors: {arg}";
        }

        [SecurityPermission(SecurityAction.Demand, SerializationFormatter = true)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("Errors", Errors);
        }
    }
}