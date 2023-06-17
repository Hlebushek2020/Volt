using System.Threading.Tasks;
using DSharpPlus;
using Microsoft.Extensions.Logging;
using VoltBot.Logs;
using VoltBot.Logs.Providers;

namespace VoltBot.Modules
{
    public abstract class HandlerModule<THandlerArgs>
    {
        protected static readonly ILogger _defaultLogger = LoggerFactory.Current.CreateLogger<DefaultLoggerProvider>();

        public abstract Task Handler(DiscordClient sender, THandlerArgs e);
    }
}