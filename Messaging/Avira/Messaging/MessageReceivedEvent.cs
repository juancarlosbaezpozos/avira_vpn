using System;

namespace Avira.Messaging
{
    public class MessageReceivedEvent : EventArgs
    {
        public string Message { get; set; }
    }
}