using System.IO.Pipes;

namespace Avira.Acp.Endpoints.NamedPipe
{
    public interface INamedPipeAuthTokenExtractor
    {
        NamedPipeAuthenticationToken Extract(NamedPipeServerStream namedPipeServerStream);
    }
}