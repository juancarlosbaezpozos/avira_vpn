using System;
using System.Runtime.InteropServices;

namespace Avira.VPN.Core.Win
{
    public class ShellComHelpers
    {
        public enum WindowTypes
        {
            Explorer = 0,
            Browser = 1,
            Desktop = 8
        }

        public enum FindWindowOptions
        {
            Needdispatch = 1,
            Includepending = 2,
            Cookiepassed = 4
        }

        public enum SVGIO : uint
        {
            SVGIO_BACKGROUND = 0u,
            SVGIO_SELECTION = 1u,
            SVGIO_ALLVIEW = 2u,
            SVGIO_CHECKED = 3u,
            SVGIO_TYPE_MASK = 0xFu,
            SVGIO_FLAG_VIEWORDER = 0x80000000u
        }

        public struct FolderSettings
        {
            public uint ViewMode;

            public uint Flags;
        }

        public struct Rect
        {
            public int Top;

            public int Left;

            public int Width;

            public int Height;
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214E2-0000-0000-C000-000000000046")]
        public interface IShellBrowser
        {
            void GetWindow(out IntPtr phwnd);

            void ContextSensitiveHelp(bool fEnterMode);

            void InsertMenusSB(IntPtr intPtrShared, IntPtr lpMenuWidths);

            void SetMenuSB(IntPtr intPtrShared, IntPtr holemenuRes, IntPtr intPtrActiveObject);

            void RemoveMenusSB(IntPtr intPtrShared);

            void SetStatusTextSB(IntPtr pszStatusText);

            void EnableModelessSB(bool fEnable);

            void TranslateAcceleratorSB(IntPtr pmsg, ushort wId);

            void BrowseObject(IntPtr pidl, uint wFlags);

            void GetViewStateStream(uint grfMode, IntPtr ppStrm);

            void GetControlWindow(uint id, out IntPtr lpIntPtr);

            void SendControlMsg(uint id, uint uMsg, uint wParam, uint lParam, IntPtr pret);

            void QueryActiveShellView(ref IShellView ppshv);

            void OnViewWindowActive(IShellView ppshv);

            void SetToolbarItems(IntPtr lpButtons, uint nButtons, uint uFlags);
        }

        [ComImport]
        [Guid("000214E3-0000-0000-C000-000000000046")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        public interface IShellView
        {
            void GetWindow(out IntPtr windowHandle);

            void ContextSensitiveHelp(bool fEnterMode);

            [PreserveSig]
            long TranslateAcceleratorA(IntPtr message);

            void EnableModeless(bool enable);

            void UIActivate([MarshalAs(UnmanagedType.U4)] uint activtionState);

            void Refresh();

            void CreateViewWindow([In] [MarshalAs(UnmanagedType.Interface)] IShellView previousShellView,
                [In] ref FolderSettings folderSetting, [In] IShellBrowser shellBrowser, [In] ref Rect bounds,
                [In] [Out] ref IntPtr handleOfCreatedWindow);

            void DestroyViewWindow();

            void GetCurrentInfo(ref FolderSettings pfs);

            void AddPropertySheetPages([In] [MarshalAs(UnmanagedType.U4)] uint reserved,
                [In] ref IntPtr functionPointer, [In] IntPtr lparam);

            void SaveViewState();

            void SelectItem(IntPtr pidlItem, [MarshalAs(UnmanagedType.U4)] uint flags);

            void GetItemObject(uint uItem, ref Guid riid, [MarshalAs(UnmanagedType.IUnknown)] out object ppv);
        }

        private static class NativeMethods
        {
            [DllImport("shlwapi.dll")]
            internal static extern int IUnknown_QueryService(IntPtr pUnk, ref Guid guidService, ref Guid riid,
                [MarshalAs(UnmanagedType.IUnknown)] out object ppv);
        }

        private static Guid sidSTopLevelBrowser = new Guid(1284947520u, 37212, 4559, 153, 211, 0, 170, 0, 74, 232, 55);

        public static Guid iidDispatch = new Guid("00020400-0000-0000-C000-000000000046");

        public static Guid iidSHellBrowser = new Guid("000214E2-0000-0000-C000-000000000046");

        public static Guid clsidShellWindows = new Guid("9BA05972-F6A8-11CF-A442-00A0C90A8F39");

        public static IShellBrowser GetShellBrowser(object firstDesktop)
        {
            NativeMethods.IUnknown_QueryService(Marshal.GetIDispatchForObject(firstDesktop), ref sidSTopLevelBrowser,
                ref iidSHellBrowser, out var ppv);
            return (ppv as IShellBrowser) ?? throw new Exception("No shell browser");
        }
    }
}