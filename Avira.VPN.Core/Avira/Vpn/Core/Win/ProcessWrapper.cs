using System;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Avira.VPN.Core.Win
{
    public class ProcessWrapper : IProcess, IDisposable
    {
        private bool disposed;

        private Process process;

        public int ExitCode => process.ExitCode;

        public StreamReader StandardOutput => process.StandardOutput;

        public StreamReader StandardError => process.StandardError;

        public bool HasExited => process.HasExited;

        public ProcessStartInfo StartInfo
        {
            get { return process.StartInfo; }
            set { process.StartInfo = value; }
        }

        public bool EnableRaisingEvents
        {
            get { return process.EnableRaisingEvents; }
            set { process.EnableRaisingEvents = value; }
        }

        public DateTime ExitTime => process.ExitTime;

        public DateTime StartTime => process.StartTime;

        public int Id => process.Id;

        public string MainWindowTitle => process.MainWindowTitle;

        public IntPtr MainWindowHandle => process.MainWindowHandle;

        public IntPtr Handle => process.Handle;

        public event DataReceivedEventHandler ErrorDataReceived;

        public event EventHandler Exited;

        public event DataReceivedEventHandler OutputDataReceived;

        public ProcessWrapper()
            : this(new Process())
        {
        }

        public ProcessWrapper(Process process)
        {
            this.process = process;
            this.process.ErrorDataReceived += delegate(object sender, DataReceivedEventArgs args)
            {
                this.ErrorDataReceived?.Invoke(this, args);
            };
            this.process.Exited += delegate(object sender, EventArgs args) { this.Exited?.Invoke(this, args); };
            this.process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs args)
            {
                this.OutputDataReceived?.Invoke(this, args);
            };
        }

        public void Refresh()
        {
            process.Refresh();
        }

        public void Kill()
        {
            process.Kill();
        }

        public bool Start()
        {
            return process.Start();
        }

        public bool WaitForExit(int milliseconds)
        {
            return process.WaitForExit(milliseconds);
        }

        public void WaitForExit()
        {
            process.WaitForExit();
        }

        public void Close()
        {
            process.Close();
        }

        public void BeginOutputReadLine()
        {
            process.BeginOutputReadLine();
        }

        public Process[] GetProcessesByName(string name)
        {
            return Process.GetProcessesByName(name);
        }

        public bool ProcessExists(int processId)
        {
            try
            {
                Process.GetProcessById(processId);
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        public bool ProcessExists(string processName)
        {
            try
            {
                return Process.GetProcessesByName(processName).Any();
            }
            catch (Exception)
            {
                return false;
            }
        }

        public bool ProcessOpenWhichContainsSubstringInMainWindowTitle(string processName, string substring)
        {
            return Process.GetProcessesByName(processName).Any((Process p) => p.MainWindowTitle.Contains(substring));
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing && process != null)
                {
                    process.Dispose();
                    process = null;
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}