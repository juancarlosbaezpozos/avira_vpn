using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Avira.Common.Core.CertificateTools
{
    internal class FilesLocker : IDisposable
    {
        private readonly Dictionary<string, FileLockHandle> lockedFiles = new Dictionary<string, FileLockHandle>();

        private readonly ReadOnlyCollection<string> filesPath;

        public FilesLocker(ReadOnlyCollection<string> filesPath)
        {
            this.filesPath = filesPath;
        }

        public virtual void LockAll()
        {
            foreach (string item in filesPath)
            {
                Lock(item);
            }
        }

        public void UnlockAll()
        {
            foreach (FileLockHandle value in lockedFiles.Values)
            {
                value.Release();
            }
        }

        public void Unlock(string filePath)
        {
            if (lockedFiles.ContainsKey(filePath))
            {
                lockedFiles[filePath].Release();
                lockedFiles.Remove(filePath);
            }
        }

        protected void Lock(string filePath)
        {
            lockedFiles.Add(filePath, FileLockHandle.SetReadAccess(filePath));
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                UnlockAll();
            }
        }
    }
}