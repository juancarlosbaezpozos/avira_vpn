using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace Avira.VpnService
{
    [SuppressMessage("StyleCop.CSharp.NamingRules", "SA1306:FieldNamesMustBeginWithLowerCaseLetter",
        Justification = "Reviewed.")]
    internal class NetCfg : IDisposable
    {
        internal static class NativeMethods
        {
            [DllImport("ole32.Dll")]
            public static extern int CoCreateInstance(ref Guid clsid, [MarshalAs(UnmanagedType.IUnknown)] object inner,
                uint context, ref Guid uuid, [MarshalAs(UnmanagedType.IUnknown)] out object returnedComObject);
        }

        private static readonly uint CLSCTX_INPROC_SERVER = 1u;

        private static Guid clsidCNetCfg = new Guid("5B035261-40F9-11D1-AAEC-00805FC1270E");

        private static Guid iidINetCfg = new Guid("C0E8AE93-306E-11D1-AACF-00805FC1270E");

        private INetCfgLock cfgLock;

        private INetCfg cfg;

        public static NetCfg CreateInstance()
        {
            NetCfg netCfg = new NetCfg();
            int num = NativeMethods.CoCreateInstance(ref clsidCNetCfg, null, CLSCTX_INPROC_SERVER, ref iidINetCfg,
                out var returnedComObject);
            if (num != 0)
            {
                throw new Exception($"CoCreateInstance failed, HRESULT: {num}.");
            }

            INetCfg netCfg2 = (netCfg.cfg = returnedComObject as INetCfg);
            netCfg.cfgLock = netCfg.GetLock();
            num = netCfg.cfgLock.AcquireWriteLock(5000u, "vpnservice", out var ppszwClientDescription);
            if (num != 0)
            {
                throw new Exception($"AcquireWriteLock failed, lock held by {ppszwClientDescription}. HRESULT: {num}.");
            }

            num = netCfg.cfg.Initialize(IntPtr.Zero);
            if (num != 0)
            {
                throw new Exception($"Initialize HRESULT: {num}.");
            }

            return netCfg;
        }

        public INetCfgLock GetLock()
        {
            return (INetCfgLock)cfg;
        }

        public INetCfg Get()
        {
            return cfg;
        }

        public void Dispose()
        {
            if (cfg != null)
            {
                cfg.Uninitialize();
            }

            if (cfgLock != null)
            {
                cfgLock.ReleaseWriteLock();
            }
        }
    }
}