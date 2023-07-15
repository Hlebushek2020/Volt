using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using VoltBot.Database;
using VoltBot.Database.Entities;

namespace VoltBot.Services.Implementation
{
    internal class CheckingHistoryService : ICheckingHistoryService
    {
        private readonly ILogger<CheckingHistoryService> _logger;

        public CheckingHistoryService(DiscordClient discordClient, ILogger<CheckingHistoryService> logger)
        {
            _logger = logger;

            discordClient.MessageCreated += Handler;

            _logger.LogInformation($"{nameof(CheckingHistoryService)} loaded.");
        }

        public async Task Handler(DiscordClient sender, MessageCreateEventArgs e)
        {
            using VoltDbContext dbContext = new VoltDbContext();

            GuildSettings guildSettings = await dbContext.GuildSettings.FindAsync(e.Guild.Id);

            if (guildSettings != null && guildSettings.HistoryModuleIsEnabled &&
                guildSettings.HistoryChannelId == e.Channel.Id)
            {
                _logger.LogInformation(
                    $"Guild {e.Guild.Name}. Channel: {e.Channel.Name}. Jump link: {e.Message.JumpLink}");

                IReadOnlyList<DiscordMessage> beforeMessages = await e.Channel.GetMessagesBeforeAsync(e.Message.Id, 1);
                DiscordMessage beforeMessage = beforeMessages.FirstOrDefault();
                string[] beforeParts = beforeMessage.Content.Replace("  ", " ").Split(' ');
                string[] currentParts = e.Message.Content.Replace("  ", " ").Split(' ');
                if (beforeParts.Length < currentParts.Length - guildSettings.HistoryWordCount)
                {
                    DiscordChannel discordChannel =
                        await sender.GetChannelAsync(guildSettings.HistoryAdminNotificationChannelId.Value);

                    DiscordEmbedBuilder discordEmbed = new DiscordEmbedBuilder()
                        .WithTitle("Checking history")
                        .WithDescription(e.Message.JumpLink.ToString())
                        .WithColor(Constants.WarningColor);

                    RoleMention roleMention = new RoleMention(guildSettings.HistoryAdminPingRole.Value);

                    // For ping to work, the role must be specified in the Сontent and Mention
                    DiscordMessageBuilder discordMessage = new DiscordMessageBuilder()
                        .WithContent($"<@&{guildSettings.HistoryAdminPingRole.Value}>")
                        .AddMention(roleMention)
                        .AddEmbed(discordEmbed.Build());

                    await discordChannel.SendMessageAsync(discordMessage);
                }
            }
        }
    }
}