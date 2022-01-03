using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Linq;
using System.Runtime.InteropServices;
using Avira.Common.Core;
using Avira.Win.Messaging;
using Serilog;

namespace Avira.Vpn.Core.Win
{
    public class PipeAuthorizationChecker : IAuthorizationChecker
    {
        internal static class NativeMethods
        {
            [DllImport("kernel32.dll", SetLastError = true)]
            internal static extern bool GetNamedPipeClientProcessId(IntPtr handle, out int id);
        }

        public AuthenticodeVerifier AuthenticodeVerifier { get; set; }

        public bool EnableAuthenticodeCheck { get; set; }

        internal List<string> PathList { get; }

        public PipeAuthorizationChecker()
        {
            PathList = new List<string>();
            AddDefaultPathRules();
            EnableAuthenticodeCheck = true;
            AuthenticodeVerifier = new AuthenticodeVerifier();
        }

        public bool Check(NamedPipeServerStream srv)
        {
            Process process = null;
            try
            {
                process = Process.GetProcessById(GetClientProcessId(srv.SafePipeHandle.DangerousGetHandle()));
                if (process != null)
                {
                    string fileName = process.MainModule.FileName;
                    return VerifyPath(fileName) && VerifyAviraSignature(fileName);
                }
            }
            catch (Exception exception)
            {
                Serilog.Log.Error(exception, "Authorization check failed for pipe client.");
            }
            finally
            {
                ((IDisposable)process)?.Dispose();
            }

            return false;
        }

        private void AddDefaultPathRules()
        {
            DirectoryInfo parent =
                Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar));
            AddPath(parent.FullName + Path.DirectorySeparatorChar);
        }

        private void AddPath(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                PathList.Add(path.ToLowerInvariant());
            }
        }

        private bool VerifyPath(string clientPath)
        {
            string path = clientPath.ToLowerInvariant();
            bool num = PathList.Any((string pathPrefix) => path.StartsWith(pathPrefix));
            if (!num)
            {
                Serilog.Log.Warning("Failed to verify path for " + clientPath);
            }

            return num;
        }

        private bool VerifyAviraSignature(string clientPath)
        {
            if (!EnableAuthenticodeCheck)
            {
                return true;
            }

            bool num = AuthenticodeVerifier?.VerifyAviraSignature(clientPath) ?? true;
            if (!num)
            {
                Serilog.Log.Warning("Failed to verify authenticode signature for " + clientPath);
            }

            return num;
        }

        public void AllowPath(string directory)
        {
            PathList.Add(directory);
        }

        public int GetClientProcessId(IntPtr handle)
        {
            if (!NativeMethods.GetNamedPipeClientProcessId(handle, out var id))
            {
                throw new Win32Exception("GetNamedPipeClientProcessId failed.");
            }

            return id;
        }
    }
}