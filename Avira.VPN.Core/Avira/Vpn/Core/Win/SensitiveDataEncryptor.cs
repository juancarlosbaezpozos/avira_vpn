using System.Collections.Generic;

namespace Avira.VPN.Core.Win
{
    public class SensitiveDataEncryptor
    {
        private const string ListGuid = "7333909a-d049-46e9-bbaa-fa2cfb006686";

        internal static List<string> DecryptAppsList(string appsListData)
        {
            return new DataEncryption("7333909a-d049-46e9-bbaa-fa2cfb006686").Decrypt<List<string>>(appsListData);
        }
    }
}