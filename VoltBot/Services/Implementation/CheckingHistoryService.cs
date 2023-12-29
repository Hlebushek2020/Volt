﻿using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        private const char AddTwoWords = '4';
        private const char TwoMessagesInRow = '5';

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

            if (guildSettings is { HistoryModuleIsEnabled: true } &&
                e.Channel.Id == guildSettings.HistoryChannelId)
            {
                _logger.LogInformation(
                    $"Guild {e.Guild.Name}. Channel: {e.Channel.Name}. Jump link: {e.Message.JumpLink}");

                DiscordMessage beforeMessage = e.Channel
                    .GetMessagesBeforeAsync(e.Message.Id, 1)
                    .ToBlockingEnumerable()
                    .First();
                string[] beforeParts = beforeMessage.Content.Replace("  ", " ").Split(' ');
                string[] currentParts = e.Message.Content.Replace("  ", " ").Split(' ');
                StringBuilder breakingRule = new StringBuilder();

                if (beforeParts.Length < currentParts.Length - guildSettings.HistoryWordCount)
                    breakingRule.Append(AddTwoWords);

                if (e.Message.Author.Id == beforeMessage.Author.Id)
                {
                    if (breakingRule.Length > 0)
                        breakingRule.Append(", ");
                    breakingRule.Append(TwoMessagesInRow);
                }

                if (breakingRule.Length > 0)
                {
                    DiscordChannel discordChannel =
                        await sender.GetChannelAsync(guildSettings.HistoryAdminNotificationChannelId.Value);

                    DiscordEmbedBuilder discordEmbed = new DiscordEmbedBuilder()
                        .WithTitle("Checking history")
                        .WithDescription(e.Message.JumpLink.ToString())
                        .AddField("Cases", breakingRule.ToString())
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