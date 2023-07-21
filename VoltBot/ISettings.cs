﻿using Microsoft.Extensions.Logging;

namespace VoltBot
{
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
    }
}