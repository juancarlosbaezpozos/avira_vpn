using System;
using System.Collections.Generic;

namespace Avira.VpnService
{
    public class Adapter : NetCfgComponent
    {
        private static Guid adapterGuid = new Guid(1295444338u, 58149, 4558, 191, 193, 8, 0, 43, 225, 3, 24);

        private static Guid netCfgClassGuid = new Guid("C0E8AE97-306E-11D1-AACF-00805FC1270E");

        public Adapter(INetCfg cfg, INetCfgComponent component)
            : base(cfg, component)
        {
        }

        public static IEnumerable<Adapter> GetAdapters(INetCfg cfg)
        {
            if (cfg.QueryNetCfgClass(ref adapterGuid, ref netCfgClassGuid, out var ppvObject) != 0)
            {
                throw new Exception("INetCfg.QueryNetCfgClass failed");
            }

            if (((ppvObject as INetCfgClass) ?? throw new Exception("INetCfgClass failed")).EnumComponents(
                    out ppvObject) != 0)
            {
                throw new Exception("INetCfgClass.EnumComponents failed");
            }

            IEnumNetCfgComponent adapters = ppvObject as IEnumNetCfgComponent;
            if (adapters == null)
            {
                throw new Exception("IEnumNetCfgComponent failed");
            }

            if (adapters.Reset() != 0)
            {
                throw new Exception("INetCfgClass.EnumComponents failed");
            }

            int pceltFetched;
            int num = adapters.Next(1, out ppvObject, out pceltFetched);
            while (num == 0)
            {
                if (ppvObject is INetCfgComponent netCfgComponent)
                {
                    yield return new Adapter(cfg, netCfgComponent);
                    num = adapters.Next(1, out ppvObject, out pceltFetched);
                    continue;
                }

                break;
            }
        }

        public void Enable(string protocol)
        {
            foreach (BindingPath path in GetPaths(EnumBindingPathFlags.EBP_ABOVE))
            {
                if (path.GetOwner().GetId() == protocol)
                {
                    path.Enable();
                }
            }
        }

        public void DisableAllProtocols()
        {
            foreach (BindingPath path in GetPaths(EnumBindingPathFlags.EBP_ABOVE))
            {
                path.Disable();
            }
        }
    }
}