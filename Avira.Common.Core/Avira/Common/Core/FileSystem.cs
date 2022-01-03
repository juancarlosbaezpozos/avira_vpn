using System;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace Avira.Common.Core
{
    public static class FileSystem
    {
        public static string MakeFullPath(string path)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
        }

        public static Version GetVersion(string fileName)
        {
            return new Version(FileVersionInfo.GetVersionInfo(MakeFullPath(fileName)).ProductVersion);
        }

        public static string GetSha1Hash(string filePath)
        {
            try
            {
                using FileStream inputStream = File.OpenRead(filePath);
                return BitConverter.ToString(new SHA1Managed().ComputeHash(inputStream));
            }
            catch (Exception arg)
            {
                Log.Error($"Failed to calculate sha1 hash for {filePath}. {arg}");
                return null;
            }
        }
    }
}