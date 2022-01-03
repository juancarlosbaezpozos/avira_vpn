using System.Runtime.InteropServices;

namespace Avira.VpnService
{
    [Guid("C0E8AE94-306E-11D1-AACF-00805FC1270E")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(true)]
    public interface INetCfgBindingInterface
    {
        [PreserveSig]
        int GetName([MarshalAs(UnmanagedType.LPWStr)] out string ppszwInterfaceName);

        [PreserveSig]
        int GetUpperComponent([MarshalAs(UnmanagedType.IUnknown)] out object ppnccItem);

        [PreserveSig]
        int GetLowerComponent([MarshalAs(UnmanagedType.IUnknown)] out object ppnccItem);
    }
}