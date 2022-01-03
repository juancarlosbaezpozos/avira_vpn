using System;

namespace Avira.Acp.Endpoints.NamedPipe
{
    public interface IDuplexNamedPipeChannel : IChannel
    {
        string ReceiverPipeName { get; }

        event EventHandler Restarted;

        void Connect(string pipeName);

        void Close();

        string Receive();

        bool IsOpen();
    }
}