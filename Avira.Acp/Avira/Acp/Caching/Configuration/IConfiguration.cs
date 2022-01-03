namespace Avira.Acp.Caching.Configuration
{
    public interface IConfiguration
    {
        ResourceConfiguration GetResourceConfiguration(ResourceLocation resourceLocation);
    }
}