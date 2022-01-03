using System.Runtime.InteropServices;

namespace Avira.VpnService
{
    [ComVisible(true)]
    [Guid("C0E8AE96-306E-11D1-AACF-00805FC1270E")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    public interface INetCfgBindingPath
    {
        [PreserveSig]
        int IsSamePathAs([In] [MarshalAs(UnmanagedType.IUnknown)] object path);

        [PreserveSig]
        int IsSubPathOf([In] [MarshalAs(UnmanagedType.IUnknown)] object path);

        [PreserveSig]
        int IsEnabled();

        [PreserveSig]
        int Enable([MarshalAs(UnmanagedType.Bool)] bool enable);

        [PreserveSig]
        int GetPathToken([MarshalAs(UnmanagedType.LPWStr)] out string ppszwPathToken);

        [PreserveSig]
        int GetOwner([MarshalAs(UnmanagedType.IUnknown)] out object component);

        [PreserveSig]
        int GetDepth([MarshalAs(UnmanagedType.U4)] out int interfaces);

        [PreserveSig]
        int EnumBindingInterfaces([MarshalAs(UnmanagedType.IUnknown)] out object ppenumInterface);
    }
}