using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Avira.VPN.Core
{
    public class Cryptography
    {
        public static string ComputeSha1(string data)
        {
            SHA1CryptoServiceProvider sHA1CryptoServiceProvider = new SHA1CryptoServiceProvider();
            sHA1CryptoServiceProvider.Initialize();
            byte[] bytes = Encoding.UTF8.GetBytes(data);
            sHA1CryptoServiceProvider.HashCore(bytes, 0, bytes.Length);
            byte[] source = sHA1CryptoServiceProvider.HashFinal();
            return string.Join("", source.Select((byte b) => b.ToString("x2")).ToArray());
        }
    }
}