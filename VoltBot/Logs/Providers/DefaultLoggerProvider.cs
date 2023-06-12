using Microsoft.Extensions.Logging;
using VoltBot.Logs.Loggers;

namespace VoltBot.Logs.Providers
{
    internal class DefaultLoggerProvider : ILoggerProvider
    {
        private readonly LogLevel _minimumLevel;
        private FileLogger _logger;

        public DefaultLoggerProvider(LogLevel minimumLevel = LogLevel.Information) { _minimumLevel = minimumLevel; }

        public ILogger CreateLogger(string categoryName)
        {
            if (_logger == null)
            {
                _logger = new FileLogger(LogWriter.GetOrCreate(), _minimumLevel);
            }
            return _logger;
        }

        public void Dispose() => _logger?.Dispose();
    }
}