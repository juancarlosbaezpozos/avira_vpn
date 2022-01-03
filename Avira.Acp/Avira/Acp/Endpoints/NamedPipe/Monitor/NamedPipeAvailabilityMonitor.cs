using System;
using System.Threading;

namespace Avira.Acp.Endpoints.NamedPipe.Monitor
{
    public class NamedPipeAvailabilityMonitor : INamedPipeAvailabilityMonitor
    {
        private readonly TimeSpan pollTimeout;

        private readonly string pipeName;

        private NamedPipeStatus lastKnownStatus;

        private bool started;

        public NamedPipeStatus CurrentStatus => GetCurrentStatus();

        public event EventHandler<NamedPipeAvailabilityChangedEventArgs> StatusChanged;

        public NamedPipeAvailabilityMonitor(string pipeName)
            : this(pipeName, TimeSpan.FromSeconds(5.0))
        {
        }

        public NamedPipeAvailabilityMonitor(string pipeName, TimeSpan pollTimeout)
        {
            this.pipeName = pipeName;
            this.pollTimeout = pollTimeout;
        }

        public void Start()
        {
            if (started)
            {
                return;
            }

            started = true;
            Thread thread = new Thread((ParameterizedThreadStart)delegate
            {
                while (started)
                {
                    NamedPipeStatus currentStatus = GetCurrentStatus();
                    if (lastKnownStatus != currentStatus)
                    {
                        lastKnownStatus = currentStatus;
                        this.StatusChanged?.Invoke(this, new NamedPipeAvailabilityChangedEventArgs(currentStatus));
                    }

                    Thread.Sleep(pollTimeout);
                }
            });
            thread.IsBackground = true;
            thread.Start();
        }

        public void Stop()
        {
            started = false;
            Reset();
        }

        public void Reset()
        {
            lastKnownStatus = NamedPipeStatus.Unknown;
        }

        private NamedPipeStatus GetCurrentStatus()
        {
            if (!DuplexNamedPipeChannel.NativeMethods.WaitNamedPipe("\\\\.\\pipe\\" + pipeName, 0))
            {
                return NamedPipeStatus.NotAvailable;
            }

            return NamedPipeStatus.Available;
        }
    }
}