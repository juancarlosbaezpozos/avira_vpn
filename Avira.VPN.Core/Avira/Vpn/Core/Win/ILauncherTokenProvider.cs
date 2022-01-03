using System;

namespace Avira.VPN.Core.Win
{
    public interface ILauncherTokenProvider
    {
        string CurrentToken { get; }

        event EventHandler<TokenChangedEventArgs> TokenChanged;
    }
}