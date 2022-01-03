using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Security.Principal;
using System.Text;
using Avira.VPN.Core.Win;
using Microsoft.Win32;
using Serilog;

namespace Avira.VPN.OeConnector
{
    public class LauncherCommunicator : ICommunicationChannel
    {
        private string pipeKeyName;

        public LauncherCommunicator()
            : this("ExternalNamedPipe")
        {
        }

        public LauncherCommunicator(string pipeKeyName)
        {
            this.pipeKeyName = pipeKeyName;
        }

        public void SendMessage(string message)
        {
            NamedPipeClientStream namedPipeClientStream = null;
            try
            {
                namedPipeClientStream = new NamedPipeClientStream(".", GetPipeName(), PipeDirection.Out,
                    PipeOptions.None, TokenImpersonationLevel.Identification);
                namedPipeClientStream.Connect(1000);
                using StreamWriter streamWriter =
                    new StreamWriter(namedPipeClientStream, Encoding.Default, 1024, leaveOpen: true);
                namedPipeClientStream = null;
                Log.Information("Send message to back-end: " + message);
                streamWriter.WriteLine(message);
                streamWriter.Flush();
            }
            catch (Exception exception)
            {
                Log.Warning(exception, "Can't send message to launcher.");
            }
            finally
            {
                namedPipeClientStream?.Dispose();
            }
        }

        internal static string ConvertTo32RegKeyPath(string keyPath)
        {
            string[] array = keyPath.Split('\\');
            if (array.Length >= 2 && string.Compare(array[1], "software", ignoreCase: true) == 0 &&
                (array.Length == 2 || string.Compare(array[2], "wow6432node", ignoreCase: true) != 0))
            {
                List<string> list = new List<string>(array);
                list.Insert(2, "Wow6432Node");
                return string.Join("\\", list);
            }

            return keyPath;
        }

        internal string GetRegValue32(string keyPath, string keyName, string defaultValue)
        {
            if (Environment.Is64BitProcess)
            {
                keyPath = ConvertTo32RegKeyPath(keyPath);
            }

            return (string)Registry.GetValue(keyPath, keyName, defaultValue);
        }

        internal string GetPipeName()
        {
            return GetPipeName("HKEY_LOCAL_MACHINE", "SOFTWARE\\X-AVCSD\\Launcher");
        }

        internal string GetPipeName(string registryHieve, string masterKey)
        {
            try
            {
                string regValue = GetRegValue32(Path.Combine(registryHieve, masterKey), "MasterKey", string.Empty);
                if (!string.IsNullOrEmpty(regValue))
                {
                    string text = GetRegValue32(Path.Combine(registryHieve, regValue), pipeKeyName, string.Empty)
                        .ToString();
                    return string.IsNullOrEmpty(text) ? "Avira.ExternalCommunicationTaskPipe" : text;
                }
            }
            catch (Exception exception)
            {
                Log.Warning(exception, "Failed to read pipe name from registry.");
            }

            return "Avira.ExternalCommunicationTaskPipe";
        }
    }
}