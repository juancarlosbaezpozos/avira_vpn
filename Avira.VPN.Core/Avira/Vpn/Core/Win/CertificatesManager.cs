using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;

namespace Avira.VPN.Core.Win
{
    public class CertificatesManager : IDisposable
    {
        [SuppressMessage("ReSharper", "InconsistentNaming",
            Justification = "Const flags should have the same name as in Win32Api.")]
        internal static class NativeMethods
        {
            internal const int X509_ASN_ENCODING = 1;

            internal const int CERT_STORE_ADD_REPLACE_EXISTING = 3;

            internal const string TrustedPublisher = "TrustedPublisher";

            internal const uint CERT_STORE_PROV_SYSTEM = 10u;

            internal const uint CERT_SYSTEM_STORE_LOCAL_MACHINE_ID = 2u;

            internal const uint CERT_SYSTEM_STORE_LOCATION_SHIFT = 16u;

            internal const uint CERT_STORE_ADD_ALWAYS = 4u;

            internal const uint CERT_SYSTEM_STORE_LOCAL_MACHINE = 131072u;

            [DllImport("CRYPT32.DLL", CharSet = CharSet.Auto, SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool CertAddEncodedCertificateToStore(IntPtr certStore, int certEncodingType,
                byte[] certEncoded, int certEncodedLength, int addDisposition, IntPtr certContext);

            [DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern IntPtr CertOpenSystemStore(IntPtr hCryptProv, string storename);

            [DllImport("crypt32.dll", SetLastError = true)]
            public static extern bool CertCloseStore(IntPtr hCertStore, uint dwFlags);

            [DllImport("crypt32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern IntPtr CertOpenStore(IntPtr storeProvider, uint dwMsgAndCertEncodingType,
                IntPtr hCryptProv, uint dwFlags, string cchNameString);

            [DllImport("crypt32.dll", SetLastError = true)]
            public static extern bool CertDeleteCertificateFromStore(IntPtr pCertContext);

            [DllImport("CRYPT32", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern IntPtr CertEnumCertificatesInStore(IntPtr storeProvider, IntPtr prevCertContext);
        }

        private readonly X509Certificate certificate;

        private readonly byte[] aviraRootCertificateHash = new byte[20]
        {
            146, 34, 17, 245, 171, 69, 33, 148, 29, 38,
            145, 90, 235, 130, 238, 114, 143, 147, 16, 130
        };

        private IntPtr store;

        private bool disposed;

        public CertificatesManager(string certificate)
        {
            this.certificate = new X509Certificate(certificate);
            if (!IsAviraCertificate())
            {
                throw new Exception("Invalid certificate!");
            }
        }

        ~CertificatesManager()
        {
            Dispose(disposing: false);
        }

        public void AddToTrustedPublisher()
        {
            OpenTrustedPublisherStore();
            byte[] rawCertData = certificate.GetRawCertData();
            if (!NativeMethods.CertAddEncodedCertificateToStore(store, 1, rawCertData, rawCertData.Length, 3,
                    IntPtr.Zero))
            {
                throw new Exception("CertAddEncodedCertificateToStore failed with error: " +
                                    Marshal.GetLastWin32Error().ToString("X"));
            }
        }

        public void DeleteFromTrustedPublisher()
        {
            OpenTrustedPublisherStore();
            if (!NativeMethods.CertDeleteCertificateFromStore(FindCertificateInStore()))
            {
                throw new FileNotFoundException("CertFindCertificateInStore failed with error: " +
                                                Marshal.GetLastWin32Error().ToString("X"));
            }
        }

        private IntPtr FindCertificateInStore()
        {
            IntPtr intPtr = NativeMethods.CertEnumCertificatesInStore(store, IntPtr.Zero);
            while (intPtr != IntPtr.Zero)
            {
                if (new X509Certificate(intPtr).GetCertHash().SequenceEqual(aviraRootCertificateHash))
                {
                    return intPtr;
                }

                intPtr = NativeMethods.CertEnumCertificatesInStore(store, intPtr);
            }

            return IntPtr.Zero;
        }

        private bool IsAviraCertificate()
        {
            return certificate.GetCertHash().SequenceEqual(aviraRootCertificateHash);
        }

        private void OpenTrustedPublisherStore()
        {
            if (!(store != IntPtr.Zero))
            {
                store = NativeMethods.CertOpenStore((IntPtr)10L, 1u, IntPtr.Zero, 131076u, "TrustedPublisher");
                if (store == IntPtr.Zero)
                {
                    throw new Exception("CertOpenStore failed with error: " +
                                        Marshal.GetLastWin32Error().ToString("X"));
                }
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    NativeMethods.CertCloseStore(store, 0u);
                }

                disposed = true;
            }
        }
    }
}