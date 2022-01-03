namespace Avira.VpnService
{
    public class FatalNotification : OpenVpnNotification
    {
        public FatalNotification(string message)
        {
            base.Reason = message;
        }

        public override string ToString()
        {
            return $"[{base.Timestamp.ToLongDateString()}] {GetType()} : {base.Reason}";
        }
    }
}