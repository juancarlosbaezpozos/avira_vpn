using System;
using System.IO;

namespace Avira.Acp.CertificateTools
{
    internal class FileLockHandle : IDisposable
    {
        private bool disposed;

        private FileStream stream;

        private FileLockHandle(string fileName, FileAccess fileAccess, FileShare fileShare, bool create)
        {
            FileMode mode = (create ? FileMode.OpenOrCreate : FileMode.Open);
            stream = new FileStream(fileName, mode, fileAccess, fileShare);
        }

        public static FileLockHandle CreateFileAndSetReadWriteAccess(string fileName)
        {
            return new FileLockHandle(fileName, FileAccess.ReadWrite, FileShare.ReadWrite, create: true);
        }

        public static FileLockHandle CreateFileAndSetWithReadAccess(string fileName)
        {
            return new FileLockHandle(fileName, FileAccess.Read, FileShare.Read, create: true);
        }

        public static FileLockHandle SetReadWriteAccess(string fileName)
        {
            return new FileLockHandle(fileName, FileAccess.ReadWrite, FileShare.ReadWrite, create: false);
        }

        public static FileLockHandle SetReadAccess(string fileName)
        {
            return new FileLockHandle(fileName, FileAccess.Read, FileShare.Read, create: false);
        }

        public void Release()
        {
            Dispose();
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (stream != null)
                {
                    stream.Close();
                    stream = null;
                }

                disposed = true;
            }
        }
    }
}