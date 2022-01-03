using System;
using System.Threading;
using System.Threading.Tasks;

namespace Avira.VPN.Core
{
    public interface ISocket : IDisposable
    {
        bool IsConnected();

        Task Connect(string host, int port);

        Task Send(byte[] data, int timeout = -1);

        Task<byte[]> Receive(int timeout = -1, CancellationToken cancellationToken = default(CancellationToken));

        void Disconnect();
    }
}