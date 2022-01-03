using System;
using System.IO;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;
using Serilog.Formatting.Display;

namespace Avira.VPN.Core
{
    public class DebugLogger : ILogEventSink
    {
        private const string outputTemplate = "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}";

        private readonly ITextFormatter textFormatter;

        public DebugLogger()
        {
            textFormatter =
                new MessageTemplateTextFormatter(
                    "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level}] {Message}{NewLine}{Exception}", null);
        }

        public void Emit(LogEvent logEvent)
        {
            if (logEvent == null)
            {
                throw new ArgumentNullException("logEvent");
            }

            StringWriter output = new StringWriter();
            textFormatter.Format(logEvent, output);
        }
    }
}