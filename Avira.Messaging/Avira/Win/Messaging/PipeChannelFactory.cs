using System;
using System.Collections.Concurrent;

namespace Avira.Win.Messaging
{
    public class PipeChannelFactory : IMessengerFactory, IDisposable
    {
        private ConcurrentDictionary<string, IMessenger> messengers = new ConcurrentDictionary<string, IMessenger>();

        public IMessenger GetMessenger(string url)
        {
            Uri uri = new Uri(url);
            if (messengers.TryGetValue(uri.Host, out var value))
            {
                return value;
            }

            value = PipeCommunicatorClient.Connect(uri.Host);
            messengers[uri.Host] = value;
            return value;
        }

        public void Dispose()
        {
            foreach (IMessenger value in messengers.Values)
            {
                value.Dispose();
            }
        }
    }
}