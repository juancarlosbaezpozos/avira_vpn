using System;
using System.Threading;
using System.Threading.Tasks;

public sealed class SemaphoreLock
{
    public sealed class DisposableLockAsync : IDisposable
    {
        private SemaphoreSlim internalSemaphore;

        internal DisposableLockAsync()
        {
        }

        internal async Task<DisposableLockAsync> Lock(SemaphoreSlim semaphore)
        {
            internalSemaphore = semaphore;
            await internalSemaphore.WaitAsync();
            return this;
        }

        public void Dispose()
        {
            internalSemaphore.Release();
        }
    }

    private readonly SemaphoreSlim semaphore = new(1, 1);

    public Task<DisposableLockAsync> AquireLockAsync()
    {
        return new DisposableLockAsync().Lock(semaphore);
    }
}