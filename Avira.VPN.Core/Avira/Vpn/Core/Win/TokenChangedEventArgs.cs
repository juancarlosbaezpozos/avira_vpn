using System;

namespace Avira.VPN.Core.Win
{
    public class TokenChangedEventArgs : EventArgs
    {
        public string CurrentToken { get; }

        public TokenChangedEventArgs(string currentToken)
        {
            CurrentToken = currentToken;
        }
    }
}