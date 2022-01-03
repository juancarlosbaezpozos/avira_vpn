using System;

namespace Avira.Win.Messaging
{
    public interface IChannelConnectNotifier
    {
        event EventHandler<PipeConnectionArgs> PipeConnected;

        event EventHandler<PipeConnectionArgs> PipeDisconnected;
    }
}