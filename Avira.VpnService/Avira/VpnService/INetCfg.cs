using System;
using System.Runtime.InteropServices;

namespace Avira.VpnService
{
    [Guid("C0E8AE93-306E-11D1-AACF-00805FC1270E")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(true)]
    public interface INetCfg
    {
        [PreserveSig]
        int Initialize(IntPtr reserved);

        [PreserveSig]
        int Uninitialize();

        [PreserveSig]
        int Apply();

        [PreserveSig]
        int Cancel();

        [PreserveSig]
        int EnumComponents(IntPtr pguidClass, [MarshalAs(UnmanagedType.IUnknown)] out object ppenumComponent);

        [PreserveSig]
        int FindComponent([In] [MarshalAs(UnmanagedType.LPWStr)] string pszwInfId,
            [MarshalAs(UnmanagedType.IUnknown)] out object component);

        [PreserveSig]
        int QueryNetCfgClass([In] ref Guid pguidClass, ref Guid riid,
            [MarshalAs(UnmanagedType.IUnknown)] out object ppvObject);
    }
}