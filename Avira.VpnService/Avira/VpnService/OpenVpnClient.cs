namespace Avira.VpnService
{
    public class OpenVpnClient : IOpenVpnClient
    {
        private readonly IManagementEndpoint endpoint;

        public OpenVpnClient(IManagementEndpoint endpoint)
        {
            this.endpoint = endpoint;
        }

        public void EnableStateNotification()
        {
            endpoint.Request("state on");
        }

        public void Release()
        {
            endpoint.Request("hold release");
        }

        public void Auth(string user, string password)
        {
            endpoint.Request("username \"Auth\" " + user);
            endpoint.Request("password \"Auth\" " + password);
        }

        public void EnableByteCountNotification(int period)
        {
            endpoint.Request($"bytecount {period}");
        }

        public void EnableLogging()
        {
            endpoint.Request("log on");
        }

        public void SetVerbosityLevel(ushort level)
        {
            if (level > 15)
            {
                level = 15;
            }

            endpoint.Request($"verb {level}");
        }
    }
}