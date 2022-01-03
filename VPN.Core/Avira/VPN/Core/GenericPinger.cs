using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Avira.VPN.Core
{
    public abstract class GenericPinger : IDisposable
    {
        private readonly int intervalInMiliseconds;

        private readonly string persistencyKeyName;

        private CancellationTokenSource pingerCancellationToken;

        private Task runningTask;

        private bool pingerTaskStarted;

        private object pingerTaskLock = new object();

        private bool disposed;

        public GenericPinger(int intervalInMiliseconds, string persistancyKeyName, bool startOnCreation = true)
        {
            this.intervalInMiliseconds = intervalInMiliseconds;
            persistencyKeyName = persistancyKeyName;
            if (startOnCreation)
            {
                StartPinger();
            }
        }

        public void StartPinger()
        {
            lock (pingerTaskLock)
            {
                if (pingerTaskStarted)
                {
                    return;
                }

                pingerCancellationToken = new CancellationTokenSource();
                CancellationToken token = pingerCancellationToken.Token;
                runningTask = Task.Factory.StartNew((Func<Task>)async delegate
                {
                    while (!pingerCancellationToken.IsCancellationRequested)
                    {
                        if (!IsPingAlreadySent())
                        {
                            SendPing();
                        }

                        await Task.Delay(intervalInMiliseconds, token);
                        token.ThrowIfCancellationRequested();
                    }
                }, token);
                pingerTaskStarted = true;
            }
        }

        public void StopPinger()
        {
            lock (pingerTaskLock)
            {
                if (!pingerTaskStarted)
                {
                    return;
                }

                pingerCancellationToken.Cancel();
                try
                {
                    runningTask.Wait();
                }
                catch (Exception)
                {
                }
                finally
                {
                    pingerCancellationToken.Dispose();
                }

                pingerTaskStarted = false;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    StopPinger();
                }

                disposed = true;
            }
        }

        public bool IsPingAlreadySent()
        {
            string value = DiContainer.Resolve<ISettings>().Get(persistencyKeyName);
            if (string.IsNullOrEmpty(value))
            {
                return false;
            }

            DateTime dateTime = JsonConvert.DeserializeObject<DateTime>(value)!.AddMilliseconds(intervalInMiliseconds);
            return DateTime.Now < dateTime;
        }

        public abstract void SendPing();
    }
}