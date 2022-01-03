using System;
using System.Runtime.InteropServices;

namespace Avira.VpnService
{
    [Guid("C0E8AE99-306E-11D1-AACF-00805FC1270E")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(true)]
    public interface INetCfgComponent
    {
        [PreserveSig]
        int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string ppszwDisplayName);

        [PreserveSig]
        int SetDisplayName([In] [MarshalAs(UnmanagedType.LPWStr)] string pszwDisplayName);

        [PreserveSig]
        int GetHelpText([MarshalAs(UnmanagedType.LPWStr)] out string pszwHelpText);

        [PreserveSig]
        int GetId([MarshalAs(UnmanagedType.LPWStr)] out string ppszwId);

        [PreserveSig]
        int GetCharacteristics([MarshalAs(UnmanagedType.U4)] out int pdwCharacteristics);

        [PreserveSig]
        int GetInstanceGuid([Out] Guid guid);

        [PreserveSig]
        int GetPnpDevNodeId([MarshalAs(UnmanagedType.LPWStr)] out string ppszwDevNodeId);

        [PreserveSig]
        int GetClassGuid([Out] Guid guid);

        [PreserveSig]
        int GetBindName([MarshalAs(UnmanagedType.LPWStr)] out string ppszwBindName);

        [PreserveSig]
        int GetDeviceStatus([MarshalAs(UnmanagedType.U4)] out int pulStatus);

        [PreserveSig]
        int OpenParamKey([Out] [MarshalAs(UnmanagedType.U4)] IntPtr phkey);

        [PreserveSig]
        int RaisePropertyUi([In] IntPtr hwndParent, [In] [MarshalAs(UnmanagedType.U4)] int flags,
            [In] [MarshalAs(UnmanagedType.IUnknown)] object punkContext);
    }
}