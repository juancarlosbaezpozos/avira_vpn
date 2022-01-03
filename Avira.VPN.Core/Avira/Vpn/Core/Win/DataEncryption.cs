using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml.Serialization;

namespace Avira.VPN.Core.Win
{
    public class DataEncryption
    {
        private readonly string encryptionKey;

        private readonly byte[] salt = new byte[13]
        {
            73, 118, 97, 110, 32, 77, 101, 100, 118, 101,
            100, 101, 118
        };

        public DataEncryption(string encryptionKey)
        {
            this.encryptionKey = encryptionKey;
        }

        public string Encrypt(string clearText)
        {
            return Encryptor(clearText, EncryptMethod);
        }

        public string Decrypt(string cipherText)
        {
            return Encryptor(cipherText, DecryptMethod);
        }

        public string Encrypt<T>(T data)
        {
            XmlSerializer xmlSerializer = new XmlSerializer(data.GetType());
            StringBuilder stringBuilder = new StringBuilder();
            using (TextWriter textWriter = new StringWriter(stringBuilder))
            {
                xmlSerializer.Serialize(textWriter, data);
            }

            return Encrypt(stringBuilder.ToString());
        }

        public T Decrypt<T>(string cipherText) where T : class
        {
            string s = Decrypt(cipherText);
            XmlSerializer deserializer = new XmlSerializer(typeof(T));
            TextReader reader = new StringReader(s);
            try
            {
                return Catch.All(() => (T)deserializer.Deserialize(reader), null);
            }
            finally
            {
                if (reader != null)
                {
                    ((IDisposable)reader).Dispose();
                }
            }
        }

        private string Encryptor(string data, Func<string, MemoryStream, SymmetricAlgorithm, string> encryptorMethod)
        {
            using Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(encryptionKey, salt);
            using Aes aes = Aes.Create();
            aes.Key = rfc2898DeriveBytes.GetBytes(32);
            aes.IV = rfc2898DeriveBytes.GetBytes(16);
            using MemoryStream arg = new MemoryStream();
            return encryptorMethod(data, arg, aes);
        }

        private string EncryptMethod(string clearText, MemoryStream memoryStream, SymmetricAlgorithm encryptor)
        {
            using (CryptoStream cryptoStream =
                   new CryptoStream(memoryStream, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
            {
                byte[] bytes = Encoding.Unicode.GetBytes(clearText);
                cryptoStream.Write(bytes, 0, bytes.Length);
            }

            return Convert.ToBase64String(memoryStream.ToArray());
        }

        private string DecryptMethod(string cipherText, MemoryStream memoryStream, SymmetricAlgorithm encryptor)
        {
            using (CryptoStream cryptoStream =
                   new CryptoStream(memoryStream, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
            {
                byte[] array = Convert.FromBase64String(cipherText);
                cryptoStream.Write(array, 0, array.Length);
            }

            return Encoding.Unicode.GetString(memoryStream.ToArray());
        }
    }
}