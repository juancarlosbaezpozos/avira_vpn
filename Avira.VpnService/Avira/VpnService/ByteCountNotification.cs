namespace Avira.VpnService
{
    public class ByteCountNotification : OpenVpnNotification
    {
        public ulong Ingoing { get; private set; }

        public ulong Outgoing { get; private set; }

        public ByteCountNotification(string message)
        {
            string[] array = message.Split(',');
            Ingoing = ulong.Parse(array[0]);
            Outgoing = ulong.Parse(array[1]);
        }
    }
}