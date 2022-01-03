using System.Net;

namespace Avira.Acp.Extensions
{
    public static class HttpStatusCodeExtensions
    {
        public static bool IsSuccess(this HttpStatusCode httpStatusCode)
        {
            if (httpStatusCode >= HttpStatusCode.OK)
            {
                return httpStatusCode <= (HttpStatusCode)299;
            }

            return false;
        }
    }
}