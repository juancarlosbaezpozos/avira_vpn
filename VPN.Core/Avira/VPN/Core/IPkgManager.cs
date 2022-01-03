using System;

namespace Avira.VPN.Core
{
    public interface IPkgManager
    {
        void Install(string path);

        bool IsSignatureValid(string path);

        Version GetPackageVersion(string filename);

        string GetPackageFileName(string path, string pattern);
    }
}