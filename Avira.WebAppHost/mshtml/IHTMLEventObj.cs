using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace mshtml
{
    [ComImport]
    [CompilerGenerated]
    [Guid("3050F32D-98B5-11CF-BB82-00AA00BDCE0B")]
    [TypeIdentifier]
    public interface IHTMLEventObj
    {
        [DispId(1003)]
        bool ctrlKey
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            [DispId(1003)]
            get;
        }

        [DispId(1007)]
        object returnValue
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            [DispId(1007)]
            [return: MarshalAs(UnmanagedType.Struct)]
            get;
            [MethodImpl(MethodImplOptions.InternalCall)]
            [DispId(1007)]
            [param: In]
            [param: MarshalAs(UnmanagedType.Struct)]
            set;
        }

        [DispId(1008)]
        bool cancelBubble
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            [DispId(1008)]
            get;
            [MethodImpl(MethodImplOptions.InternalCall)]
            [DispId(1008)]
            [param: In]
            set;
        }

        [DispId(1011)]
        int keyCode
        {
            [MethodImpl(MethodImplOptions.InternalCall)]
            [DispId(1011)]
            get;
            [MethodImpl(MethodImplOptions.InternalCall)]
            [DispId(1011)]
            [param: In]
            set;
        }

        void _VtblGap1_2();

        void _VtblGap2_1();

        void _VtblGap3_2();
    }
}