namespace Avira.Win.Messaging
{
    public interface IMessengerFactory
    {
        IMessenger GetMessenger(string url);
    }
}