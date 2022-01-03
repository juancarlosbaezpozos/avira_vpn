namespace Avira.Common.Core.CertificateTools
{
    internal enum AuthenticodeError
    {
        Success,
        ErrorNotValidSignature,
        ErrorUnknownSignature,
        ErrorEmptySignature,
        ErrorNotTrustworthySignature,
        ErrorCannotReadExecutablePath,
        ErrorCannotLoadWinTrust
    }
}