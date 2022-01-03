using System;
using System.Runtime.Serialization;

namespace Avira.Win.Messaging
{
    [Serializable]
    public class MessengerClosedException : Exception
    {
        public MessengerClosedException()
        {
        }

        public MessengerClosedException(string message)
            : base(message)
        {
        }

        public MessengerClosedException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected MessengerClosedException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}