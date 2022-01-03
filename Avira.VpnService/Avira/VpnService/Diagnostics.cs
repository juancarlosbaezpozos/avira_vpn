using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avira.VPN.Core;
using Avira.VPN.Core.Win;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Serilog;

namespace Avira.VpnService
{
    public sealed class Diagnostics : IDiagnostics
    {
        private string deviceId;

        private IHttpClient httpClient;

        private string diagnosticFile;

        public Diagnostics(IHttpClient httpClient, string deviceId)
        {
            this.httpClient = httpClient;
            this.deviceId = deviceId;
        }

        public Task<bool> CollectData(JObject userSelection)
        {
            diagnosticFile = deviceId + "--" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string text =
                Convert.ToBase64String(
                    Encoding.Unicode.GetBytes(userSelection?.ToString() ?? "No user selection or null object."));
            string arguments = diagnosticFile + " " + text;
            return ExecuteCollector(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ProductSettings.DiagnosticExe),
                arguments);
        }

        private static Task<bool> ExecuteCollector(string exePath, string arguments)
        {
            Process process = new Process();
            try
            {
                TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();
                ProcessStartInfo startInfo = new ProcessStartInfo(exePath, arguments)
                {
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                process.StartInfo = startInfo;
                process.EnableRaisingEvents = true;
                process.Exited += delegate
                {
                    tcs.SetResult(process.ExitCode == 0);
                    process.Dispose();
                };
                process.Start();
                return tcs.Task;
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Error collecting diagnostic data.");
                return Task.FromResult(result: false);
            }
        }

        public DiagnosticData SendData()
        {
            string text = Directory.EnumerateFiles(ProductSettings.DiagnosticHistoryLocation, diagnosticFile + "*")
                .FirstOrDefault();
            if (string.IsNullOrEmpty(text))
            {
                Log.Error("No diagnostic data file found with pattern: " + diagnosticFile + " in folder: " +
                          ProductSettings.DiagnosticHistoryLocation);
                return null;
            }

            try
            {
                byte[] data = File.ReadAllBytes(text);
                string uri = "diagnostics?device_id=" + deviceId;
                DiagnosticData diagnosticData = JsonConvert.DeserializeObject<DiagnosticData>(
                    httpClient.Post(uri, data, "application/zip"), new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore
                    });
                if (diagnosticData != null)
                {
                    diagnosticData.Date = DateTime.Now;
                }

                return diagnosticData;
            }
            catch (Exception exception)
            {
                Log.Error(exception, "Error posting diagnostic data");
                return null;
            }
        }

        internal void LogReferenceNr(string diagId)
        {
            using StreamWriter streamWriter =
                new StreamWriter(Path.Combine(ProductSettings.DiagnosticHistoryLocation, "ReferenceNumbers.txt"),
                    append: true);
            streamWriter.WriteLineAsync(string.Concat(DateTime.Now, "\t -> \t", diagnosticFile, "\t -> \t", diagId));
        }
    }
}