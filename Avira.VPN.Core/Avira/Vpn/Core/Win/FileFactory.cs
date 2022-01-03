using System;

namespace Avira.VPN.Core.Win
{
    public class FileFactory : IFileFactory
    {
        public IFile CreateApplicationDataFile(string fileName)
        {
            throw new NotImplementedException();
        }

        public IFile CreateFile(string path)
        {
            return new FileWrapper(path);
        }
    }
}