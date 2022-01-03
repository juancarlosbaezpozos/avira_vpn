namespace Avira.VPN.Acp
{
    public class TrialPaymentUrl : PaymentUrl
    {
        public TrialPaymentUrl()
            : base("&filter[subsource]=trial")
        {
        }
    }
}