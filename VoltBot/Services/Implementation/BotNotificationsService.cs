using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using VoltBot.Database;
using VoltBot.Database.Entities;

namespace VoltBot.Services.Implementation;

internal class BotNotificationsService : IBotNotificationsService
{
    private readonly DiscordClient _discordClient;
    private readonly ILogger<BotNotificationsService> _logger;

    public BotNotificationsService(DiscordClient discordClient, ILogger<BotNotificationsService> logger)
    {
        _discordClient = discordClient;
        _logger = logger;

        _logger.LogInformation($"{nameof(BotNotificationsService)} loaded.");
    }

    public Task SendReadyNotifications()
    {
        _logger.LogInformation("Sending notifications about the start of the bot.");
        return SendNotifications("Бот снова в сети!", gs => gs.IsReadyNotification);
    }

    public Task SendShutdownNotifications(string reason)
    {
        _logger.LogInformation("Sending notifications about the shutdown of the bot.");
        return SendNotifications($"Отключение бота по следующей причине: {reason}", gs => gs.IsShutdownNotification);
    }

    private async Task SendNotifications(string message, Expression<Func<GuildSettings, bool>> predicate)
    {
        VoltDbContext voltDbContext = new VoltDbContext();
        IReadOnlyList<GuildSettings> guildSettingsList =
            await voltDbContext.GuildSettings.Where(predicate).ToListAsync();

        DiscordEmbed discordEmbed = new DiscordEmbedBuilder()
            .WithTitle(_discordClient.CurrentUser.Username)
            .WithDescription(message)
            .WithColor(Constants.SuccessColor)
            .Build();

        foreach (GuildSettings guildSettings in guildSettingsList)
        {
            try
            {
                DiscordChannel discordChannel =
                    await _discordClient.GetChannelAsync(guildSettings.NotificationChannelId.Value);
                await discordChannel.SendMessageAsync(discordEmbed);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    $"Notification not sent. Guild: {guildSettings.GuildId}. Channel: {
                        guildSettings.NotificationChannelId}. Message: {ex.Message}");
            }
        }
    }
}