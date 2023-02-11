using DSharpPlus.Entities;

namespace VoltBot
{
    internal static class Constants
    {
        public const string DeleteMessageEmoji = ":negative_squared_cross_mark:";

        public static DiscordColor ErrorColor { get; } = DiscordColor.Red;
        public static DiscordColor SuccessColor { get; } = DiscordColor.Rose;
        public static DiscordColor StatusColor { get; } = DiscordColor.DarkGray;
        public static DiscordColor WarningColor { get; } = DiscordColor.Yellow;
    }
}