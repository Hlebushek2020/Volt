namespace VoltBot.Settings
{
    internal interface IReadOnlySettings
    {
        public string BotToken { get; }
        public string BotPrefix { get; }
        public string BotDescription { get; }
        public string VkSecret { get; }
    }
}
