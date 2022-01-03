using System;
using System.Runtime.InteropServices;

namespace Avira.Common.Core.CertificateTools
{
    internal class WinVerifyTrustNativeMethods
    {
        internal enum AllocMethod
        {
            HGlobal,
            CoTaskMem
        }

        internal enum UnionChoice
        {
            File = 1,
            Catalog,
            Blob,
            Signer,
            Cert
        }

        internal enum UiChoice
        {
            All = 1,
            NoUI,
            NoBad,
            NoGood
        }

        internal enum RevocationCheckFlags
        {
            None,
            WholeChain
        }

        internal enum StateAction
        {
            Ignore,
            Verify,
            Close,
            AutoCache,
            AutoCacheFlush
        }

        [Flags]
        internal enum TrustProviderFlags
        {
            UseIE4Trust = 0x1,
            NoIE4Chain = 0x2,
            NoPolicyUsage = 0x4,
            RevocationCheckNone = 0x10,
            RevocationCheckEndCert = 0x20,
            RevocationCheckChain = 0x40,
            RecovationCheckChainExcludeRoot = 0x80,
            Safer = 0x100,
            HashOnly = 0x200,
            UseDefaultOSVerCheck = 0x400,
            LifetimeSigning = 0x800,
            WtdCacheOnlyUrlRetrieval = 0x1000
        }

        internal enum UIContext
        {
            Execute,
            Install
        }

        internal struct WINTRUST_DATA : IDisposable
        {
            public uint cbStruct;

            public IntPtr pPolicyCallbackData;

            public IntPtr pSIPCallbackData;

            public UiChoice dwUIChoice;

            public RevocationCheckFlags fdwRevocationChecks;

            public UnionChoice dwUnionChoice;

            public IntPtr pInfoStruct;

            public StateAction dwStateAction;

            public IntPtr hWVTStateData;

            private IntPtr pwszURLReference;

            public TrustProviderFlags dwProvFlags;

            public UIContext dwUIContext;

            public WINTRUST_DATA(WINTRUST_FILE_INFO fileInfo, bool preventOnlineCheck)
            {
                cbStruct = (uint)Marshal.SizeOf(typeof(WINTRUST_DATA));
                pInfoStruct = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WINTRUST_FILE_INFO)));
                Marshal.StructureToPtr(fileInfo, pInfoStruct, fDeleteOld: false);
                dwUnionChoice = UnionChoice.File;
                pPolicyCallbackData = IntPtr.Zero;
                pSIPCallbackData = IntPtr.Zero;
                dwUIChoice = UiChoice.NoUI;
                fdwRevocationChecks = RevocationCheckFlags.None;
                dwStateAction = StateAction.Ignore;
                hWVTStateData = IntPtr.Zero;
                pwszURLReference = IntPtr.Zero;
                dwProvFlags = TrustProviderFlags.RevocationCheckNone;
                if (preventOnlineCheck)
                {
                    dwProvFlags |= TrustProviderFlags.WtdCacheOnlyUrlRetrieval;
                }

                dwUIContext = UIContext.Execute;
            }

            public void Dispose()
            {
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool disposing)
            {
                if (dwUnionChoice == UnionChoice.File)
                {
                    WINTRUST_FILE_INFO wINTRUST_FILE_INFO = new WINTRUST_FILE_INFO();
                    Marshal.PtrToStructure(pInfoStruct, wINTRUST_FILE_INFO);
                    wINTRUST_FILE_INFO.Dispose();
                    Marshal.DestroyStructure(pInfoStruct, typeof(WINTRUST_FILE_INFO));
                }

                Marshal.FreeHGlobal(pInfoStruct);
            }
        }

        internal sealed class UnmanagedPointer : IDisposable
        {
            private IntPtr data;

            private AllocMethod allocMethod;

            internal UnmanagedPointer(IntPtr ptr, AllocMethod method)
            {
                allocMethod = method;
                data = ptr;
            }

            ~UnmanagedPointer()
            {
                Dispose(disposing: false);
            }

            private void Dispose(bool disposing)
            {
                if (data != IntPtr.Zero)
                {
                    if (allocMethod == AllocMethod.HGlobal)
                    {
                        Marshal.FreeHGlobal(data);
                    }
                    else if (allocMethod == AllocMethod.CoTaskMem)
                    {
                        Marshal.FreeCoTaskMem(data);
                    }

                    data = IntPtr.Zero;
                }
            }

            public void Dispose()
            {
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }

            public static implicit operator IntPtr(UnmanagedPointer ptr)
            {
                return ptr.data;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        internal sealed class WINTRUST_FILE_INFO : IDisposable
        {
            public uint cbStruct;

            [MarshalAs(UnmanagedType.LPTStr)] public string pcwszFilePath;

            public IntPtr hFile;

            public IntPtr pgKnownSubject;

            public WINTRUST_FILE_INFO()
            {
                pgKnownSubject = IntPtr.Zero;
            }

            public WINTRUST_FILE_INFO(string fileName, Guid subject)
            {
                cbStruct = (uint)Marshal.SizeOf(typeof(WINTRUST_FILE_INFO));
                pcwszFilePath = fileName;
                if (subject != Guid.Empty)
                {
                    pgKnownSubject = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Guid)));
                    Marshal.StructureToPtr(subject, pgKnownSubject, fDeleteOld: true);
                }
                else
                {
                    pgKnownSubject = IntPtr.Zero;
                }

                hFile = IntPtr.Zero;
            }

            ~WINTRUST_FILE_INFO()
            {
                Dispose();
            }

            public void Dispose()
            {
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool disposing)
            {
                if (pgKnownSubject != IntPtr.Zero)
                {
                    Marshal.DestroyStructure(pgKnownSubject, typeof(Guid));
                    Marshal.FreeHGlobal(pgKnownSubject);
                }
            }
        }

        internal static uint WinVerifyTrust(string fileName, WinVerifyTrustDelegate winVerifyTrustDelegate,
            out int lastWin32Error, bool preventOnlineCheck)
        {
            Guid structure = new Guid("{00AAC56B-CD44-11d0-8CC2-00C04FC295EE}");
            using WINTRUST_FILE_INFO fileInfo = new WINTRUST_FILE_INFO(fileName, Guid.Empty);
            using WINTRUST_DATA structure2 = new WINTRUST_DATA(fileInfo, preventOnlineCheck);
            using UnmanagedPointer unmanagedPointer =
                new UnmanagedPointer(Marshal.AllocHGlobal(Marshal.SizeOf(typeof(Guid))), AllocMethod.HGlobal);
            using UnmanagedPointer unmanagedPointer2 =
                new UnmanagedPointer(Marshal.AllocHGlobal(Marshal.SizeOf(typeof(WINTRUST_DATA))), AllocMethod.HGlobal);
            IntPtr intPtr = unmanagedPointer;
            IntPtr intPtr2 = unmanagedPointer2;
            Marshal.StructureToPtr(structure, intPtr, fDeleteOld: true);
            Marshal.StructureToPtr(structure2, intPtr2, fDeleteOld: true);
            uint num = winVerifyTrustDelegate(IntPtr.Zero, intPtr, intPtr2);
            lastWin32Error = ((num != 0) ? Marshal.GetLastWin32Error() : 0);
            return num;
        }
    }
}