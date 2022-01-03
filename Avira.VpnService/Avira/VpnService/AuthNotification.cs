using System;

namespace Avira.VpnService
{
    public class AuthNotification : OpenVpnNotification
    {
        public enum AuthTypes
        {
            Auth,
            PrivateKey,
            Failed
        }

        public AuthTypes Type { get; protected set; }

        public AuthNotification(string message)
        {
            if (message.StartsWith("Need"))
            {
                Type = ParseAuthType(message, out var reason);
                base.Reason = reason;
                return;
            }

            if (message.StartsWith("Verification Failed"))
            {
                Type = AuthTypes.Failed;
                base.Reason = message;
                return;
            }

            throw new Exception($"[error] Wrong format of password real-time notification : {message}");
        }

        private static AuthTypes ParseAuthType(string message, out string reason)
        {
            if (message.Contains("'Auth'"))
            {
                reason = "Auth Auth Request";
                return AuthTypes.Auth;
            }

            if (message.Contains("'Private Key'"))
            {
                reason = "Private Key Auth Request";
                return AuthTypes.PrivateKey;
            }

            throw new Exception($"[error] Wrong auth request type in password real-time notification : {message}");
        }

        public override string ToString()
        {
            return $"[{base.Timestamp.ToShortDateString()}] {GetType()} ({Type}) : {base.Reason}";
        }
    }
}