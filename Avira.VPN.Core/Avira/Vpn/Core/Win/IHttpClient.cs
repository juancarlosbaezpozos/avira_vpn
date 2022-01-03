using System.ComponentModel;

namespace Avira.VPN.Core.Win
{
    public interface IHttpClient
    {
        string AuthenticationToken { get; }

        int Timeout { get; set; }

        string Get(string uri);

        string GetWithRetries(string uri, int numberOfRetries = 5);

        string Post(string json);

        string Post(string uri, string json);

        string Post(string uri, byte[] data, string contentType);

        void DownloadFileAsync(string uri, string destination,
            AsyncCompletedEventHandler downloadCompletedEventHandler);
    }
}