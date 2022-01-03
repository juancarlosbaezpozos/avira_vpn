using System.Collections.Generic;
using System.Diagnostics;

namespace Avira.Common.Core.CertificateTools
{
    internal interface IAuthenticodeVerifier
    {
        AuthenticodeVerificationResult VerifyAviraSignature(string file);

        AuthenticodeVerificationResult VerifyAviraSignature(Process process);

        AuthenticodeVerificationResult VerifySignature(string file, IEnumerable<string> validPublicKeys);

        AuthenticodeVerificationResult VerifySignature(string file, IEnumerable<string> validPublicKeys,
            string validAviraCertificateCommonName);

        IEnumerable<string> GetValidPublicKeys();

        string GetAviraCertificateCommonName();

        bool IsExecutingAssemblySigned();
    }
}