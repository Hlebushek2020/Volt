using DSharpPlus.CommandsNext;
using Microsoft.Extensions.Logging;
using VoltBot.Logs;
using VoltBot.Logs.Providers;
using VoltBot.Settings;

namespace VoltBot.Commands
{
    /// <summary>
    /// Represents a base class for all command modules
    /// </summary>
    public class VoltCommandModule : BaseCommandModule
    {
        protected static readonly ILogger _defaultLogger = LoggerFactory.Current.CreateLogger<DefaultLoggerProvider>();
        protected static readonly IReadOnlySettings _settings = Settings.Settings.Current;
    }
}