using System;
using Avira.VPN.Notifier;

namespace Avira.VPN.NotifierClient
{
    public interface INotifierClient : IDisposable
    {
        Action<string, string, string> CustomActionHandler { get; set; }

        void Notify(Notification notification);
    }
}