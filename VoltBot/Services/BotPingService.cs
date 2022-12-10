using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace VoltBot.Services
{
    internal class BotPingService
    {
        private readonly ILogger _defaultLogger = null;// LoggerFactory.Current.CreateLogger<DefaultLoggerProvider>();

        public async Task Ping(DiscordClient sender, MessageCreateEventArgs e)
        {
            EventId eventId = new EventId(0, "Ping");

            string messageContent = e.Message.Content.Trim();

            if ($"<@{sender.CurrentUser.Id}>".Equals(messageContent) ||
                messageContent.Equals(Settings.Settings.Current.BotPrefix, StringComparison.InvariantCultureIgnoreCase))
            {
                _defaultLogger.LogInformation(eventId, $"{e.Guild.Name}, {e.Channel.Name}, {e.Message.Id}");

                DiscordMember discordMember = await e.Message.Channel.Guild.GetMemberAsync(e.Message.Author.Id);
                await e.Message.RespondAsync($"**Виляет хвостиком и смотрит на {discordMember.DisplayName} в ожидании команды**");
            }
        }
    }
}