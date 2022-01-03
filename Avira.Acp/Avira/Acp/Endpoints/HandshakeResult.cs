using Avira.Acp.Messages;

namespace Avira.Acp.Endpoints
{
    public class HandshakeResult
    {
        public Response Response { get; private set; }

        public string NamedPipeName { get; private set; }

        public HandshakeResult(string namedPipeName, Response response)
        {
            NamedPipeName = namedPipeName;
            Response = response;
        }
    }
}