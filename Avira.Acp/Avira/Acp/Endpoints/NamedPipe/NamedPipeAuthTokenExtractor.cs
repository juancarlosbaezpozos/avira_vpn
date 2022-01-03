using System;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipes;
using System.Runtime.InteropServices;

namespace Avira.Acp.Endpoints.NamedPipe
{
    public class NamedPipeAuthTokenExtractor : INamedPipeAuthTokenExtractor
    {
        private static class NativeMethods
        {
            [DllImport("kernel32.dll", SetLastError = true)]
            public static extern bool GetNamedPipeClientProcessId(IntPtr pipeHandle, out uint clientProcessId);
        }

        [SuppressMessage("Microsoft.Security", "CA2122:DoNotIndirectlyExposeMethodsWithLinkDemands",
            Justification = "This call site is trusted.")]
        public NamedPipeAuthenticationToken Extract(NamedPipeServerStream namedPipeServerStream)
        {
            if (NativeMethods.GetNamedPipeClientProcessId(namedPipeServerStream.SafePipeHandle.DangerousGetHandle(),
                    out var clientProcessId))
            {
                return new NamedPipeAuthenticationToken
                {
                    ProcessId = (int)clientProcessId
                };
            }

            return null;
        }
    }
}