#define TRACE
using System;
using System.Diagnostics;

namespace Avira.Common.Core
{
    public static class Log
    {
        public static TraceLevel TraceLevel { get; set; }

        static Log()
        {
            Trace.Listeners.Remove("Default");
        }

        public static void Error(string message)
        {
            Trace.TraceError(message);
        }

        public static void Error(Exception ex)
        {
            Error("", ex);
        }

        public static void Error(string message, Exception ex)
        {
            Trace.TraceError(BuildExceptionMessage(message, ex));
        }

        public static void Error(string message, params object[] parameters)
        {
            Trace.TraceError(message, parameters);
        }

        public static void Warning(string message)
        {
            Trace.TraceWarning(message);
        }

        public static void Warning(Exception ex)
        {
            Warning("", ex);
        }

        public static void Warning(string message, Exception ex)
        {
            Trace.TraceWarning(BuildExceptionMessage(message, ex));
        }

        public static void Warning(string message, params object[] parameters)
        {
            Trace.TraceWarning(message, parameters);
        }

        public static void Information(string message)
        {
            Trace.TraceInformation(message);
        }

        public static void Information(string message, params object[] parameters)
        {
            Trace.TraceInformation(message, parameters);
        }

        public static void Debug(string message)
        {
            Trace.WriteLine(message);
        }

        public static void Debug(string message, params object[] parameters)
        {
            if (TraceLevel >= TraceLevel.Verbose)
            {
                Trace.WriteLine(string.Format(message, parameters));
            }
        }

        private static string BuildExceptionMessage(string message, Exception ex)
        {
            string text = message + " " + ex.Message + " StackTrace: " + ex.StackTrace;
            if (ex.InnerException != null)
            {
                text = text + "InnerException: " + ex.InnerException.Message + "StackTrace: " +
                       ex.InnerException.StackTrace;
            }

            return text;
        }
    }
}