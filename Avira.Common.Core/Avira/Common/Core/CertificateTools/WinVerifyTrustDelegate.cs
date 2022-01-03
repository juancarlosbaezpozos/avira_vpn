using System;
using System.Runtime.InteropServices;

namespace Avira.Common.Core.CertificateTools
{
    [UnmanagedFunctionPointer(CallingConvention.StdCall)]
    internal delegate uint WinVerifyTrustDelegate(IntPtr hWnd, IntPtr pgActionID, IntPtr pWinTrustData);
}