using System;
using System.Collections.Generic;
using System.Threading;
using Avira.Acp.Logging;

namespace Avira.Acp
{
    internal class SequentialDispatcher<T>
    {
        private readonly Queue<T> queue = new Queue<T>();

        private readonly Action<T> handler;

        private ILogger logger;

        private ILogger Logger
        {
            get
            {
                if (logger == null)
                {
                    logger = LoggerFacade.GetCurrentClassLogger();
                }

                return logger;
            }
        }

        public SequentialDispatcher(Action<T> handler)
        {
            this.handler = handler;
        }

        public void DispatchAsync(T item)
        {
            Enqueue(item);
            DispatchAsyncFromQueue();
        }

        private void DispatchAsyncFromQueue()
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                try
                {
                    lock (handler)
                    {
                        T val;
                        lock (queue)
                        {
                            val = queue.Dequeue();
                        }

                        if (val != null)
                        {
                            handler(val);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn("Dispatching failed. {0}", ex);
                }
            });
        }

        private void Enqueue(T item)
        {
            lock (queue)
            {
                queue.Enqueue(item);
            }
        }
    }
}