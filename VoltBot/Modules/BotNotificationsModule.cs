using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using VoltBot.Database;
using VoltBot.Database.Entities;
using VoltBot.Logs;
using VoltBot.Logs.Providers;
using LoggerFactory = VoltBot.Logs.LoggerFactory;

namespace VoltBot.Modules;

internal class BotNotificationsModule
{
    private static readonly EventId _eventId = new EventId(0, "Bot Notifications");
    private static readonly ILogger _defaultLogger = LoggerFactory.Current.CreateLogger<DefaultLoggerProvider>();

    private readonly DiscordClient _discordClient;

    public BotNotificationsModule(DiscordClient discordClient) { _discordClient = discordClient; }

    public Task SendReadyNotifications() =>
        SendNotifications("Бот снова в сети!", new VoltDbContext().GuildSettings.Where(gs => gs.IsReadyNotification));

    public Task SendShutdownNotifications(string reason) =>
        SendNotifications($"Отключение бота по следующей причине: {reason}",
            new VoltDbContext().GuildSettings.Where(gs => gs.IsShutdownNotification));

    private async Task SendNotifications(string message, IEnumerable<GuildSettings> guilds)
    {
        DiscordEmbed discordEmbed = new DiscordEmbedBuilder()
            .WithTitle(_discordClient.CurrentUser.Username)
            .WithDescription(message)
            .WithColor(Constants.SuccessColor)
            .Build();

        foreach (GuildSettings guildSettings in guilds)
        {
            try
            {
                DiscordChannel discordChannel =
                    await _discordClient.GetChannelAsync(guildSettings.NotificationChannelId.Value);
                await discordChannel.SendMessageAsync(discordEmbed);
            }
            catch (Exception ex)
            {
                _defaultLogger.LogWarning(_eventId, ex, string.Empty);
            }
        }
    }
}