using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Avira.Common.Core.CertificateTools
{
    internal class AssemblyLoadVerifier : IDisposable
    {
        private readonly ReadOnlyCollection<string> assembliesAbsolutePath;

        private readonly FilesLocker locker;

        private readonly string executingAssemblyDirectoryName =
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        private readonly string eventLogSourceName;

        private readonly string[] additionalValidPublicKeys;

        private bool allAssembliesLockedAndVerified;

        public AssemblyLoadVerifier(AppDomain domain, ReadOnlyCollection<string> assembliesAbsolutePath,
            string eventLogSourceName)
            : this(domain, assembliesAbsolutePath, eventLogSourceName, new string[0])
        {
        }

        public AssemblyLoadVerifier(AppDomain domain, ReadOnlyCollection<string> assembliesAbsolutePath,
            string eventLogSourceName, string[] additionalValidPublicKeys)
        {
            this.assembliesAbsolutePath = assembliesAbsolutePath;
            locker = new FilesLocker(this.assembliesAbsolutePath);
            this.eventLogSourceName = eventLogSourceName;
            this.additionalValidPublicKeys = additionalValidPublicKeys;
            domain.UnhandledException += OnUnhandledException;
            domain.AssemblyLoad += OnAssemblyLoad;
        }

        private void WriteEventLogEntry(string message)
        {
            try
            {
                EventLog.WriteEntry(eventLogSourceName, message, EventLogEntryType.Error);
            }
            catch
            {
            }
        }

        public bool LockAndVerifyAssemblies()
        {
            locker.LockAll();
            allAssembliesLockedAndVerified = AreSignaturesValid(assembliesAbsolutePath);
            return allAssembliesLockedAndVerified;
        }

        public bool IsVerifiedFile(string path)
        {
            if (allAssembliesLockedAndVerified)
            {
                return assembliesAbsolutePath.Contains(path, StringComparer.InvariantCultureIgnoreCase);
            }

            return false;
        }

        public bool AreSignaturesValid()
        {
            return AreSignaturesValid(assembliesAbsolutePath);
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            locker.UnlockAll();
        }

        private void OnAssemblyLoad(object sender, AssemblyLoadEventArgs args)
        {
            if (executingAssemblyDirectoryName == Path.GetDirectoryName(args.LoadedAssembly.Location) &&
                !assembliesAbsolutePath.Contains(args.LoadedAssembly.Location))
            {
                WriteEventLogEntry("Unknown Assembly Loaded : " + args.LoadedAssembly.Location);
                Environment.Exit(-1);
            }

            string fileName = Path.GetFileName(args.LoadedAssembly.Location);
            locker.Unlock(fileName);
        }

        private bool AreSignaturesValid(IEnumerable<string> assembliesToCheck)
        {
            AuthenticodeVerifier authenticodeVerifier = new AuthenticodeVerifier();
            return assembliesToCheck.All((string assemblyFileName) =>
                IsSignatureValid(authenticodeVerifier, assemblyFileName));
        }

        private bool IsSignatureValid(IAuthenticodeVerifier authenticodeVerifier, string filePath)
        {
            List<string> list = authenticodeVerifier.GetValidPublicKeys().ToList();
            list.AddRange(additionalValidPublicKeys);
            if (authenticodeVerifier.VerifySignature(filePath, list).IsSuccessful)
            {
                return true;
            }

            WriteEventLogEntry("Verifying Signature Failed : " + filePath);
            return false;
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing && locker != null)
            {
                locker.Dispose();
            }
        }
    }
}