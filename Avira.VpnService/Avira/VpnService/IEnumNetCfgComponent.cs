using System.Runtime.InteropServices;

namespace Avira.VpnService
{
    [Guid("C0E8AE92-306E-11D1-AACF-00805FC1270E")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(true)]
    public interface IEnumNetCfgComponent
    {
        [PreserveSig]
        int Next([In] [MarshalAs(UnmanagedType.U4)] int celt, [MarshalAs(UnmanagedType.IUnknown)] out object rgelt,
            [MarshalAs(UnmanagedType.U4)] out int pceltFetched);

        [PreserveSig]
        int Skip([In] [MarshalAs(UnmanagedType.U4)] int celt);

        [PreserveSig]
        int Reset();

        [PreserveSig]
        int Clone([MarshalAs(UnmanagedType.IUnknown)] out object ppenum);
    }
}