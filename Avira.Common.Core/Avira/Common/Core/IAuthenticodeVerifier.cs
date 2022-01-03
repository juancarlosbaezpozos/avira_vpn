using System.Collections.Generic;
using System.Diagnostics;

namespace Avira.Common.Core
{
    public interface IAuthenticodeVerifier
    {
        bool VerifyAviraSignature(string file);

        bool VerifyAviraSignature(Process process);

        bool VerifySignature(string file, List<string> validPublicKeys);
    }
}