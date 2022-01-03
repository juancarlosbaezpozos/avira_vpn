using System;
using System.Threading;

namespace Avira.VpnService
{
    public interface INotificationSource
    {
        EventWaitHandle SourceClosed { get; set; }

        event EventHandler<FatalNotification> FatalReceived;

        event EventHandler<StateNotification> StateReceived;

        event EventHandler<HoldNotification> HoldReceived;

        event EventHandler<AuthNotification> AuthReceived;

        event EventHandler<ByteCountNotification> ByteCountReceived;

        event EventHandler<LogNotification> LogReceived;

        event EventHandler ReadyReceived;
    }
}