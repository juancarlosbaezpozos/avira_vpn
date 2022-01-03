using System;
using Avira.Acp.Messages;

namespace Avira.Acp.Endpoints
{
    public interface IRemoteMessageProcessor : IDisposable
    {
        void Initialize(string newRemoteHost, IAdapter newAdapter);

        void ProcessMessage(Message acpMessage, ResponseHandler responseHandler);

        void UnregisterRemoteResources();
    }
}