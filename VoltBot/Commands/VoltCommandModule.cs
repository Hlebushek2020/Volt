using DSharpPlus.CommandsNext;
using Microsoft.Extensions.Logging;
using VoltBot.Logs;
using VoltBot.Logs.Providers;
using VoltBot.Settings;
using LoggerFactory = VoltBot.Logs.LoggerFactory;

namespace VoltBot.Commands
{
    /// <summary>
    /// Represents a base class for all command modules
    /// </summary>
    public class VoltCommandModule : BaseCommandModule
    {
        protected static readonly ILogger DefaultLogger = LoggerFactory.Current.CreateLogger<DefaultLoggerProvider>();
        protected static readonly IReadOnlySettings Settings = VoltBot.Settings.Settings.Current;
    }
}