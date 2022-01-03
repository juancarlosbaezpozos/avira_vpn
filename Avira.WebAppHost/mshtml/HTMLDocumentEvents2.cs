using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace mshtml
{
    [ComImport]
    [CompilerGenerated]
    [InterfaceType(2)]
    [Guid("3050F613-98B5-11CF-BB82-00AA00BDCE0B")]
    [TypeIdentifier]
    public interface HTMLDocumentEvents2
    {
        void _VtblGap1_3();

        [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall)]
        [DispId(-602)]
        void onkeydown([In] [MarshalAs(UnmanagedType.Interface)] IHTMLEventObj pEvtObj);

        [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall)]
        [DispId(-604)]
        void onkeyup([In] [MarshalAs(UnmanagedType.Interface)] IHTMLEventObj pEvtObj);

        void _VtblGap2_26();

        [MethodImpl(MethodImplOptions.PreserveSig | MethodImplOptions.InternalCall)]
        [DispId(1033)]
        bool onmousewheel([In] [MarshalAs(UnmanagedType.Interface)] IHTMLEventObj pEvtObj);
    }
}