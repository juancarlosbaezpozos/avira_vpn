namespace Avira.Win.Messaging
{
    public class ServiceInterfaceFactory
    {
        private IServiceLocator serviceLocator;

        private IMessengerFactory messengerFactory;

        public ServiceInterfaceFactory(IServiceLocator serviceLocator, IMessengerFactory messengFactory)
        {
            this.serviceLocator = serviceLocator;
            messengerFactory = messengFactory;
        }

        public IService CreateServiceInterface(string serviceName)
        {
            string serviceUrl = serviceLocator.GetServiceUrl(serviceName);
            return new ServiceInterface(messengerFactory.GetMessenger(serviceUrl), serviceName);
        }
    }
}