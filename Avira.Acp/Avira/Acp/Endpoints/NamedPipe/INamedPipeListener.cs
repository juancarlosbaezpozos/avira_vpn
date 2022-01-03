using System;

namespace Avira.Acp.Endpoints.NamedPipe
{
    public interface INamedPipeListener : IDisposable
    {
        bool IsAvailable { get; }

        event EventHandler PipeConnected;

        event EventHandler ThreadFinished;

        event EventHandler Restarted;

        void Start();

        void Close();
    }
}