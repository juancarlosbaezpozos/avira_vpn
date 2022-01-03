using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Avira.VPN.Core.Win
{
    public class AsyncSemaphore
    {
        private static readonly Task Completed = Task.FromResult(result: true);

        private readonly Queue<TaskCompletionSource<bool>> waiters = new Queue<TaskCompletionSource<bool>>();

        private int currentCount;

        public AsyncSemaphore(int initialCount)
        {
            if (initialCount < 0)
            {
                throw new ArgumentOutOfRangeException("initialCount");
            }

            currentCount = initialCount;
        }

        public Task WaitAsync()
        {
            lock (waiters)
            {
                if (currentCount > 0)
                {
                    currentCount--;
                    return Completed;
                }

                TaskCompletionSource<bool> taskCompletionSource = new TaskCompletionSource<bool>();
                waiters.Enqueue(taskCompletionSource);
                return taskCompletionSource.Task;
            }
        }

        public void Release()
        {
            TaskCompletionSource<bool> taskCompletionSource = null;
            lock (waiters)
            {
                if (waiters.Count > 0)
                {
                    taskCompletionSource = waiters.Dequeue();
                }
                else
                {
                    currentCount++;
                }
            }

            taskCompletionSource?.SetResult(result: true);
        }
    }
}