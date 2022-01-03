using System;
using Newtonsoft.Json;

namespace Avira.VPN.Core.Win
{
    public class Status : EventArgs
    {
        public ConnectionState NewState { get; set; }

        [JsonProperty(PropertyName = "status")]
        public string NewStatus => NewState.ToString();

        [JsonProperty(PropertyName = "message")]
        public string Message { get; set; }

        [JsonProperty(PropertyName = "error")] public ErrorType Error { get; set; }

        public Status()
        {
            NewState = ConnectionState.Disconnected;
            Error = ErrorType.NoError;
        }

        public Status(ConnectionState state)
        {
            NewState = state;
            Error = ErrorType.NoError;
        }

        public Status(ConnectionState state, ErrorType errorType, string errorMessage)
        {
            NewState = state;
            Error = errorType;
            Message = errorMessage;
        }
    }
}