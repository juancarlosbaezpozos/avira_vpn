namespace Avira.VPN.Core.Win
{
    public interface IStorageSecurity
    {
        void AdjustSecurityForSecureDirectoryContainer(string path, bool allowReadForUsers = false);

        void AdjustSecurityForSecureFileContainer(string path, bool allowReadForUsers = false);
    }
}