namespace Avira.Acp.CertificateTools
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