using System;

namespace Avira.VPN.Acp
{
    public class VpnQuickAction
    {
        public Action<string> Action;

        public string Id { get; set; }

        public string Text { get; set; }

        public string Tag { get; set; }

        public bool Enabled { get; set; } = true;


        public string Command { get; set; }
    }
}