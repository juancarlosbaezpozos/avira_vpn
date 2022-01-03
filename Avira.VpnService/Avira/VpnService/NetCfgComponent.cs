using System;
using System.Collections.Generic;

namespace Avira.VpnService
{
    public class NetCfgComponent
    {
        public enum EnumBindingPathFlags
        {
            EBP_ABOVE = 1,
            EBP_BELOW
        }

        private INetCfgComponent component;

        private INetCfg cfg;

        public NetCfgComponent(INetCfg cfg, INetCfgComponent component)
        {
            this.component = component;
            this.cfg = cfg;
        }

        public string GetId()
        {
            if (component.GetId(out var ppszwId) != 0)
            {
                throw new Exception("GetId failed");
            }

            return ppszwId;
        }

        public override string ToString()
        {
            return GetId();
        }

        public IEnumerable<BindingPath> GetPaths(EnumBindingPathFlags where)
        {
            if (((component as INetCfgComponentBindings) ?? throw new Exception("INetCfgComponentBindings failed"))
                .EnumBindingPaths((int)where, out var ienum) != 0)
            {
                throw new Exception("INetCfgComponentBindings.EnumBindingPaths failed");
            }

            IEnumNetCfgBindingPath paths = ienum as IEnumNetCfgBindingPath;
            if (paths == null)
            {
                throw new Exception("IEnumNetCfgBindingPath failed");
            }

            if (paths.Reset() != 0)
            {
                throw new Exception("INetCfgClass.Reset failed");
            }

            int pceltFetched;
            while (paths.Next(1, out ienum, out pceltFetched) == 0)
            {
                INetCfgBindingPath netCfgBindingPath = ienum as INetCfgBindingPath;
                if (netCfgBindingPath != null)
                {
                    yield return new BindingPath(cfg, netCfgBindingPath);
                    continue;
                }

                break;
            }
        }
    }
}