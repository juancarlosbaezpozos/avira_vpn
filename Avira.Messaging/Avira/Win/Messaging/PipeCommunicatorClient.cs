namespace Avira.Win.Messaging
{
    public class PipeCommunicatorClient
    {
        public static IMessenger Connect(string pipeName)
        {
            return Connect(pipeName, 500);
        }

        public static IMessenger Connect(string pipeName, int timeOut)
        {
            return new PipeMessenger(pipeName, timeOut);
        }
    }
}