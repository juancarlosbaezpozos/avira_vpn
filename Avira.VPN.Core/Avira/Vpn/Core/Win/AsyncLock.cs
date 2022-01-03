using System;
using System.Threading;
using System.Threading.Tasks;

namespace Avira.VPN.Core.Win
{
    public class AsyncLock
    {
        public sealed class Releaser : IDisposable
        {
            private readonly AsyncLock toRelease;

            internal Releaser(AsyncLock toRelease)
            {
                this.toRelease = toRelease;
            }

            public void Dispose()
            {
                if (toRelease != null)
                {
                    toRelease.semaphore.Release();
                }
            }
        }

        private readonly AsyncSemaphore semaphore;

        private readonly Task<Releaser> releaser;

        public AsyncLock()
        {
            semaphore = new AsyncSemaphore(1);
            releaser = Task.FromResult(new Releaser(this));
        }

        public Task<Releaser> LockAsync()
        {
            Task task = semaphore.WaitAsync();
            if (!task.IsCompleted)
            {
                return task.ContinueWith((Task _, object state) => new Releaser((AsyncLock)state), this,
                    CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }

            return releaser;
        }
    }
}