using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using VoltBot.Logs;
using VoltBot.Logs.Providers;

namespace VoltBot.Modules.Notifications;

internal class BotNotificationsModule
{
    private static readonly EventId _eventId = new EventId(0, "Bot Notifications");
    private static readonly ILogger _defaultLogger = LoggerFactory.Current.CreateLogger<DefaultLoggerProvider>();

    private readonly DiscordClient _discordClient;

    public BotNotificationsModule(DiscordClient discordClient) { _discordClient = discordClient; }

    public Task SendReadyNotifications() =>
        SendNotifications($"Отключение бота по следующей причине", notification => notification.IsReady);

    public Task SendShutdownNotifications(string reason) =>
        SendNotifications($"Отключение бота по следующей причине: {reason}", notification => notification.IsShutdown);

    private async Task SendNotifications(string message, Func<GuildNotification, bool> typeFunc)
    {
        DiscordEmbed discordEmbed = new DiscordEmbedBuilder()
            .WithTitle(_discordClient.CurrentUser.Username)
            .WithDescription(message)
            .WithColor(Constants.SuccessColor)
            .Build();

        foreach (GuildNotification guildNotification in BotNotificationsController.Current.GetAll())
        {
            if (typeFunc(guildNotification) && _discordClient.Guilds.TryGetValue(guildNotification.GuildId, out _))
            {
                try
                {
                    DiscordChannel discordChannel = await _discordClient.GetChannelAsync(guildNotification.ChannelId);
                    await discordChannel.SendMessageAsync(discordEmbed);
                }
                catch (Exception ex)
                {
                    _defaultLogger.LogWarning(_eventId, ex, string.Empty);
                }
            }
        }
    }
}