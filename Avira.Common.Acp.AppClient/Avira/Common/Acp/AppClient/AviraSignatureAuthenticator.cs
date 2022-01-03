using System.Diagnostics;
using Avira.Acp.Endpoints.NamedPipe;
using Avira.Common.Core;

namespace Avira.Common.Acp.AppClient
{
    public class AviraSignatureAuthenticator : INamedPipeAuthenticationService
    {
        public NamedPipeAuthenticationResult Authenticate(NamedPipeAuthenticationToken namedPipeAuthenticationToken)
        {
            using Process process = Process.GetProcessById(namedPipeAuthenticationToken.ProcessId);
            string fileName = process.MainModule.FileName;
            return new AuthenticodeVerifier().VerifyAviraSignature(fileName)
                ? NamedPipeAuthenticationResult.Succeeded
                : NamedPipeAuthenticationResult.Failed;
        }
    }
}