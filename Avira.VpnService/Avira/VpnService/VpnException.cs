using System;
using Avira.VPN.Core.Win;

namespace Avira.VpnService
{
    [Serializable]
    internal class VpnException : Exception
    {
        public ErrorType ErrorType { get; }

        public VpnException(string message, ErrorType errorType)
            : base(message)
        {
            ErrorType = errorType;
        }
    }
}