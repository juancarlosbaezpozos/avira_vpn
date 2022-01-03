namespace Avira.Acp.Endpoints.NamedPipe
{
    public interface INamedPipeListenerFactory
    {
        INamedPipeListener CreateListener(bool closeNamedpipe);
    }
}