using Avira.Acp.Messages;

namespace Avira.Acp
{
    internal class RegisteredResponseEntry
    {
        private readonly ResponseHandler responseHandler;

        public Request Request { get; }

        public string ExternalId { get; private set; }

        public RegisteredResponseEntry(string externalId, Request request, ResponseHandler responseHandler)
        {
            ExternalId = externalId;
            Request = request;
            this.responseHandler = responseHandler;
        }

        public void Handle(Response response)
        {
            responseHandler(response);
        }
    }
}