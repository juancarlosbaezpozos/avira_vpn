using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace Avira.VPN.Core
{
    public static class TaskExtension
    {
        public static void CatchAll(this Task task)
        {
            task.ContinueWith(delegate
            {
                if (task.IsFaulted)
                {
                    Log.Warning(task.Exception, "Task failed.");
                }
            });
        }

        public static Task Catch<TException>(this Task task, Action<TException> exceptionHandler,
            TaskScheduler scheduler = null) where TException : Exception
        {
            if (exceptionHandler == null)
            {
                throw new ArgumentNullException(nameof(exceptionHandler), "exceptionHandler paramter cannot be null");
            }

            return task.ContinueWith(delegate(Task t)
            {
                if (!t.IsCanceled && t.IsFaulted && t.Exception != null)
                {
                    Exception ex = t.Exception.Flatten().InnerExceptions.FirstOrDefault() ?? t.Exception;
                    if (ex is TException exception)
                    {
                        exceptionHandler(exception);
                    }
                }
            }, scheduler ?? TaskScheduler.Default);
        }

        public static void Finally(this Task task, Action finalAction, TaskScheduler scheduler = null)
        {
            if (finalAction == null)
            {
                throw new ArgumentNullException(nameof(finalAction), "finalAction parameter cannot be null");
            }

            task.ContinueWith(delegate { finalAction(); }, scheduler ?? TaskScheduler.Default);
        }

        public static async Task Timeout(this Task task, int delayMs)
        {
            CancellationTokenSource delayCancellationToken = new CancellationTokenSource();
            if (await Task.WhenAny(task, Task.Delay(delayMs, delayCancellationToken.Token)) == task)
            {
                delayCancellationToken.Cancel();
                delayCancellationToken.Dispose();
                return;
            }

            delayCancellationToken.Dispose();
            throw new TimeoutException();
        }
    }
}