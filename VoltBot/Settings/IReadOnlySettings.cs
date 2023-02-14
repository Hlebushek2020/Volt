namespace VoltBot.Settings
{
    public interface IReadOnlySettings
    {
        string BotToken { get; }
        string BotPrefix { get; }
        string BotDescription { get; }
        string VkSecret { get; }
        bool BugReport { get; }
        ulong BugReportChannel { get; }
        ulong BugReportServer { get; }
    }
}