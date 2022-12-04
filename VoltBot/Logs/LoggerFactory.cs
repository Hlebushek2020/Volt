using DSharpPlus;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using VoltBot.Logs.Providers;

namespace VoltBot.Logs
{
    internal class LoggerFactory : ILoggerFactory
    {
        #region Instance
        private static LoggerFactory _current;

        public static LoggerFactory Current
        {
            get
            {
                if (_current == null)
                    _current = new LoggerFactory();
                return _current;
            }
        }
        #endregion

        private readonly Dictionary<string, ILoggerProvider> _providers = new Dictionary<string, ILoggerProvider>();

        public void AddProvider(ILoggerProvider provider) => _providers.Add(provider.GetType().Name, provider);

        public ILogger CreateLogger(string categoryName)
        {
            if (typeof(BaseDiscordClient).FullName.Equals(categoryName))
            {
                categoryName = typeof(DiscordClientLoggerProvider).Name;
            }
            if (_providers.ContainsKey(categoryName))
            {
                return _providers[categoryName].CreateLogger(categoryName);
            }
            string defaultProviderName = typeof(DefaultLoggerProvider).Name;
            if (!_providers.ContainsKey(defaultProviderName))
            {
                _providers.Add(defaultProviderName, new DefaultLoggerProvider());
            }
            return _providers[defaultProviderName].CreateLogger(defaultProviderName);
        }

        public ILogger CreateLogger<T>() => CreateLogger(typeof(T).Name);

        public void Dispose()
        {
            foreach (ILoggerProvider provider in _providers.Values)
            {
                provider.Dispose();
            }
        }
    }
}