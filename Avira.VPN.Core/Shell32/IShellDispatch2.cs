using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Shell32
{
    [ComImport]
    [CompilerGenerated]
    [Guid("A4C6892C-3BA9-11D2-9DEA-00C04FB16162")]
    [TypeIdentifier]
    public interface IShellDispatch2 : IShellDispatch
    {
        void _VtblGap1_24();

        [MethodImpl(MethodImplOptions.InternalCall)]
        [DispId(1610809345)]
        void ShellExecute([In] [MarshalAs(UnmanagedType.BStr)] string File,
            [Optional] [In] [MarshalAs(UnmanagedType.Struct)] object vArgs,
            [Optional] [In] [MarshalAs(UnmanagedType.Struct)] object vDir,
            [Optional] [In] [MarshalAs(UnmanagedType.Struct)] object vOperation,
            [Optional] [In] [MarshalAs(UnmanagedType.Struct)] object vShow);
    }
}