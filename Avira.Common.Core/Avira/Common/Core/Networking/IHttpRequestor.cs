using System;
using System.Net;

namespace Avira.Common.Core.Networking
{
    public interface IHttpRequestor
    {
        string GetResponse(Uri requestUri);

        WebResponse GetWebResponse(Uri requestUri);

        IWebProxy GetProxy(Uri requestUri);
    }
}