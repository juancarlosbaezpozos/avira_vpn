using System;

namespace Avira.Win.Messaging
{
    public class MessageReceivedEvent : EventArgs
    {
        public string Message { get; set; }
    }
}