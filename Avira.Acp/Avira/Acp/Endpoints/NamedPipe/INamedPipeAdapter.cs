namespace Avira.Acp.Endpoints.NamedPipe
{
    public interface INamedPipeAdapter
    {
        NamedPipeConnectionState ConnectionState { get; }

        void Connect(string remoteHost, string serverPipeName);

        void Reconnect();

        void ProcessMessages();
    }
}