using System;
using Avira.Acp.Messages;

namespace Avira.Acp.Caching
{
    public class CacheEntry
    {
        public Response Response { get; private set; }

        public DateTime ResponseTimeStamp { get; private set; }

        public CacheEntry(Response response, DateTime responseTimeStamp)
        {
            Response = response;
            ResponseTimeStamp = responseTimeStamp;
        }
    }
}