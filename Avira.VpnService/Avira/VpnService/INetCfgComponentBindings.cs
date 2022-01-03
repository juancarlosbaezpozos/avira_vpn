using System.Runtime.InteropServices;

namespace Avira.VpnService
{
    [Guid("C0E8AE9E-306E-11D1-AACF-00805FC1270E")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [ComVisible(true)]
    public interface INetCfgComponentBindings
    {
        [PreserveSig]
        int BindTo([In] [MarshalAs(UnmanagedType.IUnknown)] object pnccItem);

        [PreserveSig]
        int UnbindFrom([In] [MarshalAs(UnmanagedType.IUnknown)] object pnccItem);

        [PreserveSig]
        int SupportsBindingInterface([In] [MarshalAs(UnmanagedType.U4)] int flags,
            [In] [MarshalAs(UnmanagedType.LPWStr)] string interfaceName);

        [PreserveSig]
        int IsBoundTo([In] [MarshalAs(UnmanagedType.IUnknown)] object pnccItem);

        [PreserveSig]
        int IsBindableTo([In] [MarshalAs(UnmanagedType.IUnknown)] object pnccItem);

        [PreserveSig]
        int EnumBindingPaths([In] [MarshalAs(UnmanagedType.U4)] int flags,
            [MarshalAs(UnmanagedType.IUnknown)] out object ienum);

        [PreserveSig]
        int MoveBefore([In] [MarshalAs(UnmanagedType.IUnknown)] object pncbItemSrc,
            [In] [MarshalAs(UnmanagedType.IUnknown)] object pncbItemDest);

        [PreserveSig]
        int MoveAfter([In] [MarshalAs(UnmanagedType.IUnknown)] object pncbItemSrc,
            [In] [MarshalAs(UnmanagedType.IUnknown)] object pncbItemDest);
    }
}