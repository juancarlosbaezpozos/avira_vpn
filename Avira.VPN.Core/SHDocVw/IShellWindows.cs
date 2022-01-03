using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SHDocVw
{
    [ComImport]
    [CompilerGenerated]
    [Guid("85CB6900-4D95-11CF-960C-0080C7F4EE85")]
    [DefaultMember("Item")]
    [TypeIdentifier]
    public interface IShellWindows : IEnumerable
    {
        [DispId(1610743808)]
        int Count
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            [DispId(1610743808)]
            get;
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        [DispId(0)]
        [return: MarshalAs(UnmanagedType.IDispatch)]
        object Item([Optional] [In] [MarshalAs(UnmanagedType.Struct)] object index);

        void _VtblGap1_6();

        [MethodImpl(MethodImplOptions.InternalCall)]
        [DispId(1610743816)]
        [return: MarshalAs(UnmanagedType.IDispatch)]
        object FindWindowSW([In] [MarshalAs(UnmanagedType.Struct)] ref object pvarloc,
            [In] [MarshalAs(UnmanagedType.Struct)] ref object pvarlocRoot, [In] int swClass, out int pHWND,
            [In] int swfwOptions);
    }
}