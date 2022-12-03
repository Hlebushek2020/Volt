using Microsoft.Extensions.Logging;
using System;

namespace VoltBot.Logs.Loggers
{
    internal class FileLogger : ILogger, IDisposable
    {
        public LogLevel MinimumLevel { get; }
        private LogWriter _logWriter { get; }

        public FileLogger(LogWriter logWriter, LogLevel logLevel = LogLevel.Information)
        {
            MinimumLevel = logLevel;
            _logWriter = logWriter;
        }

        public IDisposable BeginScope<TState>(TState state) { return null; }

        public bool IsEnabled(LogLevel logLevel) => logLevel >= MinimumLevel;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (IsEnabled(logLevel) && !_logWriter.IsDisposable)
            {
                _logWriter.Log(logLevel, eventId, state, exception, formatter);
            }
        }

        public void Dispose() => _logWriter?.Dispose();
    }
}