using Microsoft.Extensions.Logging;
using VoltBotTelegram.Enums;

namespace VoltBotTelegram;

/// <summary>
/// Provides read-only bot configuration
/// </summary>
public interface ISettings
{
    string BotToken { get; }
    string BotPrefix { get; }
    string BotDescription { get; }
    LogLevel BotLogLevel { get; }
    string VkSecret { get; }
    bool BugReport { get; }
    ulong BugReportChannel { get; }
    ulong BugReportServer { get; }
    string PingTheHost { get; }
    IReadOnlyDictionary<HistoryRules, string> TextOfHistoryRules { get; }
}