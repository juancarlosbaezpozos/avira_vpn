using System;

namespace Avira.Win.Messaging
{
    public class PipeConnectionArgs : EventArgs
    {
        public IMessenger Messenger { get; set; }

        public PipeConnectionArgs(IMessenger messenger)
        {
            Messenger = messenger;
        }
    }
}