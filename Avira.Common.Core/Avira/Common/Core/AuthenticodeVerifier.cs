using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Avira.Common.Core.CertificateTools;

namespace Avira.Common.Core
{
    public class AuthenticodeVerifier : IAuthenticodeVerifier
    {
        private Avira.Common.Core.CertificateTools.AuthenticodeVerifier authenticodeIdentifier;

        public AuthenticodeVerifier()
            : this(Assembly.GetExecutingAssembly().Location)
        {
        }

        public AuthenticodeVerifier(string executingAssemblyLocation)
        {
            authenticodeIdentifier =
                new Avira.Common.Core.CertificateTools.AuthenticodeVerifier(executingAssemblyLocation);
        }

        public bool VerifyAviraSignature(string file)
        {
            return authenticodeIdentifier.VerifyAviraSignature(file).IsSuccessful;
        }

        public bool VerifyAviraSignature(Process process)
        {
            return authenticodeIdentifier.VerifyAviraSignature(process).IsSuccessful;
        }

        public bool VerifySignature(string file, List<string> validPublicKeys)
        {
            return authenticodeIdentifier.VerifySignature(file, validPublicKeys).IsSuccessful;
        }
    }
}