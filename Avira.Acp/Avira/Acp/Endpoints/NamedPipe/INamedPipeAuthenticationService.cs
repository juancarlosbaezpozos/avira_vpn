namespace Avira.Acp.Endpoints.NamedPipe
{
    public interface INamedPipeAuthenticationService
    {
        NamedPipeAuthenticationResult Authenticate(NamedPipeAuthenticationToken namedPipeAuthenticationToken);
    }
}