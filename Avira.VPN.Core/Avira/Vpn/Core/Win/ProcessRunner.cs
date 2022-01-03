using System;
using System.Diagnostics;

namespace Avira.VPN.Core.Win
{
    public class ProcessRunner
    {
        private readonly IProcess process;

        private readonly IWhiteList whitelist;

        public string Arguments { get; set; }

        public string WorkingDirectory { get; set; }

        public string FileName { get; set; }

        public event DataReceivedEventHandler OutputDataReceived;

        public ProcessRunner(IProcess process, IWhiteList whitelist)
        {
            this.process = process;
            this.whitelist = whitelist;
        }

        public void Start()
        {
            if (!whitelist.IsWhiteListed(FileName))
            {
                throw new Exception("[error] can't start the process " + FileName + " because path is't whitelisted");
            }

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = FileName,
                Arguments = Arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = (WorkingDirectory ?? AppDomain.CurrentDomain.BaseDirectory),
                RedirectStandardOutput = true,
                RedirectStandardInput = true,
                RedirectStandardError = true
            };
            if (this.OutputDataReceived != null)
            {
                process.OutputDataReceived += delegate(object sender, DataReceivedEventArgs args)
                {
                    this.OutputDataReceived?.Invoke(this, args);
                };
            }

            process.StartInfo = startInfo;
            process.Start();
            if (this.OutputDataReceived != null)
            {
                process.BeginOutputReadLine();
            }
        }

        public int StartAndWaitForExit()
        {
            Start();
            process.WaitForExit();
            return process.ExitCode;
        }
    }
}