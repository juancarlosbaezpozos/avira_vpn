using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using Serilog;

namespace Avira.VPN.Core.Win
{
    public class AbandonedProcess
    {
        private readonly string processFullPath;

        public AbandonedProcess(string processFullPath)
        {
            this.processFullPath = processFullPath;
        }

        public bool IsRunning()
        {
            return ListAbandonedProcesses().Count != 0;
        }

        public void CleanRunningInstances()
        {
            ListAbandonedProcesses().ForEach(delegate(Process process)
            {
                try
                {
                    process.Kill();
                    process.WaitForExit(5000);
                    if (!process.HasExited)
                    {
                        throw new Exception("Process " + processFullPath +
                                            " couldn't be killed. Exit timeout was 5 seconds.");
                    }
                }
                catch (Exception exception)
                {
                    Log.Error(exception, "Failed to clean abandoned " + processFullPath + ".");
                }
            });
        }

        public void SoftCleanRunningInstances()
        {
            ListAbandonedProcesses().ForEach(delegate(Process process)
            {
                EventWaitHandle eventWaitHandle = new EventWaitHandle(initialState: false, EventResetMode.ManualReset,
                    "AviraVPNOpenVpnQuitEvent");
                eventWaitHandle.Set();
                try
                {
                    process.WaitForExit(5000);
                    process.Close();
                }
                catch (Exception exception)
                {
                    Log.Error(exception, "Failed to clean abandoned " + processFullPath + ".");
                }

                eventWaitHandle.Reset();
            });
        }

        private List<Process> ListAbandonedProcesses()
        {
            return Process.GetProcessesByName(Path.GetFileNameWithoutExtension(processFullPath)).Where(
                delegate(Process process)
                {
                    try
                    {
                        return string.Compare(process.MainModule.FileName, processFullPath,
                            StringComparison.OrdinalIgnoreCase) == 0 && !process.HasExited;
                    }
                    catch (Exception exception)
                    {
                        Log.Error(exception, "Failed to list abandoned process.");
                        return false;
                    }
                }).ToList();
        }
    }
}