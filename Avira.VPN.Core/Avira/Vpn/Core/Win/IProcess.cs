using System;
using System.Diagnostics;
using System.IO;

namespace Avira.VPN.Core.Win
{
    public interface IProcess
    {
        int ExitCode { get; }

        StreamReader StandardOutput { get; }

        StreamReader StandardError { get; }

        bool HasExited { get; }

        ProcessStartInfo StartInfo { get; set; }

        bool EnableRaisingEvents { get; set; }

        DateTime ExitTime { get; }

        DateTime StartTime { get; }

        string MainWindowTitle { get; }

        IntPtr MainWindowHandle { get; }

        IntPtr Handle { get; }

        int Id { get; }

        event DataReceivedEventHandler ErrorDataReceived;

        event EventHandler Exited;

        event DataReceivedEventHandler OutputDataReceived;

        void Refresh();

        void Kill();

        bool Start();

        bool WaitForExit(int milliseconds);

        void WaitForExit();

        void Close();

        void BeginOutputReadLine();

        Process[] GetProcessesByName(string name);

        bool ProcessExists(int processId);

        bool ProcessExists(string processName);

        bool ProcessOpenWhichContainsSubstringInMainWindowTitle(string processName, string substring);
    }
}