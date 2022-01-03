using System;

namespace Avira.VPN.Core
{
    public class AuthenticatorException : Exception
    {
        public AuthenticatorErrorCode ErrorCode { get; private set; }

        public AuthenticatorException(AuthenticatorErrorCode errorCode, string message)
            : base(message)
        {
            ErrorCode = errorCode;
        }
    }
}