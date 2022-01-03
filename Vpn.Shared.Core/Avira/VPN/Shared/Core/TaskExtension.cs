using System;
using System.Threading;
using System.Threading.Tasks;

namespace Avira.VPN.Shared.Core
{
    public static class TaskExtension
    {
        public static async Task<TResult> TimeoutAfter<TResult>(this Task<TResult> task, TimeSpan timeout)
        {
            using CancellationTokenSource timeoutCancellationTokenSource = new();
            if (await Task.WhenAny(task, Task.Delay(timeout, timeoutCancellationTokenSource.Token)) == task)
            {
                timeoutCancellationTokenSource.Cancel();
                return await task;
            }

            throw new TimeoutException($"The operation has timed out after {timeout}.");
        }
    }
}