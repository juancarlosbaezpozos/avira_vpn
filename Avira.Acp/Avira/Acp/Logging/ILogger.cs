namespace Avira.Acp.Logging
{
    internal interface ILogger
    {
        string Name { get; }

        void Debug(string message, params object[] args);

        void Info(string message, params object[] args);

        void Warn(string message, params object[] args);

        void Error(string message, params object[] args);

        void Fatal(string message, params object[] args);
    }
}