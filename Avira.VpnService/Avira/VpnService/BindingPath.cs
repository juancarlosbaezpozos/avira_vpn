using System;

namespace Avira.VpnService
{
    public class BindingPath
    {
        private readonly INetCfgBindingPath path;

        private readonly INetCfg cfg;

        public BindingPath(INetCfg cfg, INetCfgBindingPath path)
        {
            this.path = path;
            this.cfg = cfg;
        }

        public NetCfgComponent GetOwner()
        {
            if (path.GetOwner(out var component) != 0)
            {
                throw new Exception("INetCfgBindingPath.GetOwner failed");
            }

            INetCfgComponent netCfgComponent = component as INetCfgComponent;
            if (netCfgComponent == null)
            {
                throw new Exception("INetCfgComponent failed");
            }

            return new NetCfgComponent(cfg, netCfgComponent);
        }

        public void Enable()
        {
            if (path.Enable(enable: true) != 0)
            {
                throw new Exception("INetCfgBindingPath::Enable failed");
            }
        }

        public void Disable()
        {
            if (path.Enable(enable: false) != 0)
            {
                throw new Exception("INetCfgBindingPath::Enable failed");
            }
        }
    }
}