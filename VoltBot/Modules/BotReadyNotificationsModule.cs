using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;

namespace VoltBot.Modules;

internal class BotReadyNotificationsModule : HandlerModule<ReadyEventArgs>
{
    private static readonly EventId _eventId = new EventId(0, "Bot Ready Notifications");

    public override async Task Handler(DiscordClient sender, ReadyEventArgs e)
    {
        KeyValuePair<ulong, ulong>[] guildsNotificationData = BotReadyNotificationsContainer.Current.Get();
        if (guildsNotificationData.Length > 0)
        {
            DiscordEmbed discordEmbed = new DiscordEmbedBuilder()
                .WithTitle(sender.CurrentUser.Username)
                .WithDescription("Bot is running!")
                .WithColor(Constants.SuccessColor)
                .Build();

            foreach (var guildNotificationData in guildsNotificationData)
            {
                if (sender.Guilds.TryGetValue(guildNotificationData.Key, out DiscordGuild guild))
                {
                    try
                    {
                        DiscordChannel discordChannel = await sender.GetChannelAsync(guildNotificationData.Value);
                        await discordChannel.SendMessageAsync(discordEmbed);
                    }
                    catch (Exception exception)
                    {
                        _defaultLogger.LogWarning(_eventId, exception, string.Empty);
                    }
                }
            }
        }
    }
}