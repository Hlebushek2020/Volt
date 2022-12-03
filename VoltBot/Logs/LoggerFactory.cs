using DSharpPlus;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using VoltBot.Logs.Providers;

namespace VoltBot.Logs
{
    internal class LoggerFactory : ILoggerFactory
    {
        private Dictionary<string, ILoggerProvider> _providers = new Dictionary<string, ILoggerProvider>();

        public void AddProvider(ILoggerProvider provider)
        {

        }

        public ILogger CreateLogger(string categoryName)
        {
            if (typeof(BaseDiscordClient).Name.Equals(categoryName))
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

        public T CreateLogger<T>() => (T)CreateLogger(typeof(T).Name);

        public void Dispose()
        {
            foreach (ILoggerProvider provider in _providers.Values)
            {
                provider.Dispose();
            }
        }
    }
}