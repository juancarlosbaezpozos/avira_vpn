using System;

namespace Avira.VpnService
{
    public class Credentials
    {
        public Func<string> UserId { get; set; }

        public Func<string> Password { get; set; }
    }
}