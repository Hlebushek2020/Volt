using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;

namespace VoltBot.Services.Implementation
{
    internal class BotPingService : IBotPingService
    {
        private readonly ISettings _settings;
        private readonly ILogger<BotPingService> _logger;

        public BotPingService(DiscordClient discordClient, ISettings settings, ILogger<BotPingService> logger)
        {
            _settings = settings;
            _logger = logger;

            discordClient.MessageCreated += Handler;

            _logger.LogInformation($"{nameof(BotPingService)} loaded.");
        }

        public async Task Handler(DiscordClient sender, MessageCreateEventArgs e)
        {
            string messageContent = e.Message.Content.Trim();

            if ($"<@{sender.CurrentUser.Id}>".Equals(messageContent) ||
                messageContent.Equals(_settings.BotPrefix, StringComparison.InvariantCultureIgnoreCase))
            {
                _logger.LogInformation(
                    $"Guild {e.Guild.Name}. Channel: {e.Channel.Name}. Jump link: {e.Message.JumpLink}");

                DiscordMember discordMember = await e.Message.Channel.Guild.GetMemberAsync(e.Message.Author.Id);
                await e.Message.RespondAsync(
                    $"**Виляет хвостиком и смотрит на {discordMember.DisplayName} в ожидании команды**");
            }
        }
    }
}