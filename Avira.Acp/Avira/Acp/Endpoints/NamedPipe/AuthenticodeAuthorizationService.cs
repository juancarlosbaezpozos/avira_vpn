using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Avira.Acp.CertificateTools;

namespace Avira.Acp.Endpoints.NamedPipe
{
    public class AuthenticodeAuthorizationService : INamedPipeAuthenticationService
    {
        internal readonly IAuthenticodeVerifier AuthenticodeVerifier;

        public AuthenticodeAuthorizationService()
        {
            AuthenticodeVerifier = new AuthenticodeVerifier();
        }

        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands",
            Justification = "This call site is trusted.")]
        public NamedPipeAuthenticationResult Authenticate(NamedPipeAuthenticationToken namedPipeAuthenticationToken)
        {
            Process processById = Process.GetProcessById(namedPipeAuthenticationToken.ProcessId);
            if (IsOwnProcess(processById) || IsValidAviraProcess(processById))
            {
                return NamedPipeAuthenticationResult.Succeeded;
            }

            return NamedPipeAuthenticationResult.Failed;
        }

        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands",
            Justification = "This call site is trusted.")]
        private static bool IsOwnProcess(Process process)
        {
            return Process.GetCurrentProcess().Id == process.Id;
        }

        private bool IsValidAviraProcess(Process process)
        {
            return AuthenticodeVerifier.VerifyAviraSignature(process).IsSuccessful;
        }
    }
}