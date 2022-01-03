using System.IO.Pipes;

namespace Avira.Win.Messaging
{
    internal struct AsyncState
    {
        public PipeStream Server;

        public byte[] Len;

        public AsyncState(PipeStream server, byte[] len)
        {
            this = default;
            Server = server;
            Len = len;
        }
    }
}