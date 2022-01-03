using System.Collections.Generic;
using System.Linq;

namespace Avira.Acp.Caching.SmartCache
{
    public class SmartCacheFactory : ISmartCacheFactory
    {
        private readonly ICacheDataAccess cachDataAccess;

        private readonly List<ISmartCacheLogic> smartCacheLogicList = new List<ISmartCacheLogic>();

        public SmartCacheFactory(ICacheDataAccess cachDataAccess)
        {
            this.cachDataAccess = cachDataAccess;
        }

        public void Register<T>(IDataBaseMapper<T> dataBaseMapper, ResourceLocation resourceLocation, string dataType)
        {
            smartCacheLogicList.Add(new SmartCacheLogic<T>(cachDataAccess, dataBaseMapper, resourceLocation, dataType));
        }

        public ISmartCacheLogic GetByPath(string host, string path)
        {
            return smartCacheLogicList.FirstOrDefault((ISmartCacheLogic m) =>
                m.ResourceLocation.Host.Equals(host) && path.StartsWith(m.ResourceLocation.Path));
        }
    }
}