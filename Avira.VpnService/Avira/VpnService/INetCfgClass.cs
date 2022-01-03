using System.Runtime.InteropServices;

namespace Avira.VpnService
{
    [Guid("C0E8AE97-306E-11D1-AACF-00805FC1270E")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(true)]
    public interface INetCfgClass
    {
        [PreserveSig]
        int FindComponent([In] [MarshalAs(UnmanagedType.LPWStr)] string pszwInfId,
            [MarshalAs(UnmanagedType.IUnknown)] out object ppnccItem);

        [PreserveSig]
        int EnumComponents([MarshalAs(UnmanagedType.IUnknown)] out object ppenumComponent);
    }
}