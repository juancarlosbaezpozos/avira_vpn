namespace Avira.VpnService
{
    public class HoldNotification : OpenVpnNotification
    {
        public HoldNotification(string message)
        {
            base.Reason = message;
        }

        public override string ToString()
        {
            return $"[{base.Timestamp.ToLongDateString()}] {GetType()} : {base.Reason}";
        }
    }
}