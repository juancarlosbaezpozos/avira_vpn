using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Avira.Acp.CertificateTools
{
    internal class AssemblyLoadVerifier : IDisposable
    {
        private readonly ReadOnlyCollection<string> assembliesAbsolutePath;

        private readonly FilesLocker locker;

        private readonly string executingAssemblyDirectoryName =
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        private readonly string eventLogSourceName;

        private bool allAssembliesLockedAndVerified;

        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands",
            Justification = "This call site is trusted.")]
        public AssemblyLoadVerifier(AppDomain domain, ReadOnlyCollection<string> assembliesAbsolutePath,
            string eventLogSourceName)
        {
            this.assembliesAbsolutePath = assembliesAbsolutePath;
            locker = new FilesLocker(this.assembliesAbsolutePath);
            this.eventLogSourceName = eventLogSourceName;
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
                WriteEventLogEntry($"Unknown Assembly Loaded : {args.LoadedAssembly.Location}");
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
            if (authenticodeVerifier.VerifySignature(filePath, authenticodeVerifier.GetValidPublicKeys()).IsSuccessful)
            {
                return true;
            }

            WriteEventLogEntry($"Verifying Signature Failed : {filePath}");
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