namespace Avira.Acp.Caching.SmartCache
{
    public interface ISmartCacheFactory
    {
        ISmartCacheLogic GetByPath(string host, string path);

        void Register<T>(IDataBaseMapper<T> dataBaseMapper, ResourceLocation resourceLocation, string dataType);
    }
}