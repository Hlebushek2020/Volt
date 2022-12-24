using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Reflection;
using System.Text;

namespace VoltBot.Logs
{
    public class LogWriter : IDisposable
    {
        public const string FileNameFormat = "yyyyMMdd";
        public const string LogDateTimeFormatter = "dd.MM.yyyy HH:mm:ss";

        private static readonly ConcurrentDictionary<string, LogWriter> _logWritters = new ConcurrentDictionary<string, LogWriter>();

        public bool IsDisposable { get; private set; } = false;

        private readonly StreamWriter _fileLog;
        private readonly string _postFix;

        private LogWriter() { }

        private LogWriter(string postFix)
        {
            string logsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "logs");
            Directory.CreateDirectory(logsPath);
            string fileName = Path.Combine(logsPath, $"{DateTime.Now.ToString(FileNameFormat)}{postFix}.txt");
            _fileLog = new StreamWriter(fileName, true, Encoding.UTF8) { AutoFlush = true };
            _postFix = postFix;
        }

        public void Dispose()
        {
            _fileLog?.Dispose();
            IsDisposable = true;
            _logWritters.TryRemove(_postFix, out _);
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            string log = $"[{DateTime.Now.ToString(LogDateTimeFormatter)}] {logLevel.ToString().ToUpper()}; {(eventId.Name != null ? eventId.Name : "none")}";

            string formatedMessage = formatter(state, exception);
            if (!string.IsNullOrEmpty(formatedMessage))
            {
                log += $"; {formatedMessage}";
            }

            if (exception != null)
            {
                log += $"; {exception.GetType().Name}; {exception.Message}";
            }

            lock (this)
            {
                _fileLog.WriteLine(log);
                if (exception != null && logLevel > LogLevel.Warning)
                    _fileLog.WriteLine(exception.StackTrace);
            }

            if (logLevel == LogLevel.Warning)
                SynchronizedConsole.WriteLine(log, ConsoleColor.Yellow);
            else if (logLevel >= LogLevel.Error)
                SynchronizedConsole.WriteLine(log, ConsoleColor.Red);
            else
                SynchronizedConsole.WriteLine(log);
        }

        public static LogWriter GetOrCreate(string postFix = "")
        {
            if (_logWritters.ContainsKey(postFix))
                return _logWritters[postFix];
            LogWriter logWriter = new LogWriter(postFix);
            _logWritters.TryAdd(postFix, logWriter);
            return logWriter;
        }
    }
}