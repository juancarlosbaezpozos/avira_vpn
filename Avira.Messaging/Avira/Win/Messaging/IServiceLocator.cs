namespace Avira.Win.Messaging
{
    public interface IServiceLocator
    {
        string GetServiceUrl(string serviceName);
    }
}