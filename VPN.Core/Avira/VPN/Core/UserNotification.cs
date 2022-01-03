using System;

namespace Avira.VPN.Core
{
    public class UserNotification
    {
        public Action OnClick;

        public string Title { get; set; }

        public string Text { get; set; }
    }
}