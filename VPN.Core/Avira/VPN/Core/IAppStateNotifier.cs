using System;

namespace Avira.VPN.Core
{
    public interface IAppStateNotifier
    {
        event EventHandler Suspending;

        event EventHandler Resuming;

        event EventHandler ResumingWithInternetAccess;
    }
}