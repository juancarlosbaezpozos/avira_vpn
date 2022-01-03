using System.Runtime.InteropServices;

namespace Avira.VpnService
{
    [Guid("C0E8AE98-306E-11D1-AACF-00805FC1270E")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(true)]
    public interface INetCfgSysPrep
    {
        [PreserveSig]
        int HrSetupSetFirstDword([In] [MarshalAs(UnmanagedType.LPWStr)] string pwszSection,
            [In] [MarshalAs(UnmanagedType.LPWStr)] string pwszKey, [In] [MarshalAs(UnmanagedType.U4)] int value);

        [PreserveSig]
        int HrSetupSetFirstString([In] [MarshalAs(UnmanagedType.LPWStr)] string pwszSection,
            [In] [MarshalAs(UnmanagedType.LPWStr)] string pwszKey,
            [In] [MarshalAs(UnmanagedType.LPWStr)] string pwszValue);

        [PreserveSig]
        int HrSetupSetFirstStringAsBool([In] [MarshalAs(UnmanagedType.LPWStr)] string pwszSection,
            [In] [MarshalAs(UnmanagedType.LPWStr)] string pwszKey, [MarshalAs(UnmanagedType.Bool)] bool value);

        [PreserveSig]
        int HrSetupSetFirstMultiSzField([In] [MarshalAs(UnmanagedType.LPWStr)] string pwszSection,
            [In] [MarshalAs(UnmanagedType.LPWStr)] string pwszKey,
            [In] [MarshalAs(UnmanagedType.LPWStr)] string pmszValue);
    }
}