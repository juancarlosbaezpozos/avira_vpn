using System.Security.Cryptography.X509Certificates;

namespace Avira.Common.Core.CertificateTools
{
    internal class AuthenticodeVerificationResult
    {
        public bool IsSuccessful => Error == AuthenticodeError.Success;

        internal X509Certificate2 Certificate { get; set; }

        public AuthenticodeError Error { get; set; }

        public uint TrustCheckResult { get; set; }

        public int LastWin32Error { get; set; }

        public AuthenticodeVerificationResult(AuthenticodeError error)
        {
            Error = error;
        }

        internal AuthenticodeVerificationResult(AuthenticodeError error, X509Certificate2 certificate)
            : this(error)
        {
            Certificate = certificate;
        }
    }
}