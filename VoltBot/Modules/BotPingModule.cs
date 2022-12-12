using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;
using VoltBot.Logs;
using VoltBot.Logs.Providers;
using VoltBot.Modules;

namespace VoltBot.Services
{
    internal class BotPingModule : IHandlerModule<MessageCreateEventArgs>
    {
        private readonly ILogger _defaultLogger = LoggerFactory.Current.CreateLogger<DefaultLoggerProvider>();
        private readonly EventId _eventId = new EventId(0, "Ping");

        public async Task Handler(DiscordClient sender, MessageCreateEventArgs e)
        {

            string messageContent = e.Message.Content.Trim();

            if ($"<@{sender.CurrentUser.Id}>".Equals(messageContent) ||
                messageContent.Equals(Settings.Settings.Current.BotPrefix, StringComparison.InvariantCultureIgnoreCase))
            {
                _defaultLogger.LogInformation(_eventId, $"{e.Guild.Name}, {e.Channel.Name}, {e.Message.Id}");

                DiscordMember discordMember = await e.Message.Channel.Guild.GetMemberAsync(e.Message.Author.Id);
                await e.Message.RespondAsync($"**Виляет хвостиком и смотрит на {discordMember.DisplayName} в ожидании команды**");
            }
        }
    }
}