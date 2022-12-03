using Microsoft.Extensions.Logging;
using VoltBot.Logs.Loggers;

namespace VoltBot.Logs.Providers
{
    internal class DefaultLoggerProvider : ILoggerProvider
    {
        private FileLogger _logger;

        public ILogger CreateLogger(string categoryName)
        {
            if (_logger == null)
            {
                _logger = new FileLogger(LogWriter.GetDefault());
            }
            return _logger;
        }

        public void Dispose() => _logger?.Dispose();
    }
}