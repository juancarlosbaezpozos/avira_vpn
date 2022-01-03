#define TRACE
using System;
using System.Diagnostics;
using System.Reflection;

namespace Avira.Acp.Logging
{
    internal class LoggerFacade : ILogger, IDisposable
    {
        private readonly TraceSource traceSource;

        public string Name { get; set; }

        protected LoggerFacade(string name)
        {
            Name = name;
            traceSource = new TraceSource("Avira.Acp");
        }

        public static ILogger GetLogger(string loggerName)
        {
            return new LoggerFacade(loggerName);
        }

        public static ILogger GetCurrentClassLogger()
        {
            int num = 1;
            Type declaringType;
            string name;
            do
            {
                MethodBase method = new StackFrame(num, fNeedFileInfo: false).GetMethod();
                declaringType = method.DeclaringType;
                if (declaringType == null)
                {
                    name = method.Name;
                    break;
                }

                num++;
                name = declaringType.Name;
            } while (declaringType.Module.Name.Equals("mscorlib.dll", StringComparison.OrdinalIgnoreCase));

            return GetLogger(name);
        }

        public void Debug(string message, params object[] args)
        {
            TraceEvent(TraceEventType.Verbose, message, args);
        }

        public void Info(string message, params object[] args)
        {
            TraceEvent(TraceEventType.Information, message, args);
        }

        public void Warn(string message, params object[] args)
        {
            TraceEvent(TraceEventType.Warning, message, args);
        }

        public void Error(string message, params object[] args)
        {
            TraceEvent(TraceEventType.Error, message, args);
        }

        public void Fatal(string message, params object[] args)
        {
            TraceEvent(TraceEventType.Critical, message, args);
        }

        public void Dispose()
        {
            traceSource.Close();
        }

        private void TraceEvent(TraceEventType eventType, string message, params object[] args)
        {
            string arg;
            if (args.Length != 0)
            {
                try
                {
                    arg = string.Format(message, args);
                }
                catch (FormatException)
                {
                    arg = message;
                }
            }
            else
            {
                arg = message;
            }

            traceSource.TraceEvent(eventType, 0, $"[{Name}] {arg}");
            traceSource.Flush();
        }
    }
}