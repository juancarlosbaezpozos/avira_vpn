using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Shell32
{
    [ComImport]
    [CompilerGenerated]
    [Guid("E7A1AF80-4D96-11CF-960C-0080C7F4EE85")]
    [TypeIdentifier]
    public interface IShellFolderViewDual
    {
        [DispId(1610743808)]
        object Application
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            [DispId(1610743808)]
            [return: MarshalAs(UnmanagedType.IDispatch)]
            get;
        }
    }
}