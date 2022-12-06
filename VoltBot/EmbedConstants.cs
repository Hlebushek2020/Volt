using DSharpPlus.Entities;

namespace VoltBot
{
    internal sealed class EmbedConstants
    {
        public static DiscordColor ErrorColor { get; } = DiscordColor.Red;
        public static DiscordColor SuccessColor { get; } = DiscordColor.Rose;
        public static DiscordColor StatusColor { get; } = DiscordColor.DarkGray;
        public static DiscordColor WarningColor { get; } = DiscordColor.Yellow;
    }
}