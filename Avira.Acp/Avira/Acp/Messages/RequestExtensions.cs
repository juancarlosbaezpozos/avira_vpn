using System;

namespace Avira.Acp.Messages
{
    internal static class RequestExtensions
    {
        public static bool TargetsLocalHost(this Request request)
        {
            return request.Host.Equals("localhost", StringComparison.InvariantCultureIgnoreCase);
        }
    }
}