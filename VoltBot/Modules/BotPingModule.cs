using System;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;

namespace VoltBot.Modules
{
    internal class BotPingModule : HandlerModule<MessageCreateEventArgs>
    {
        private readonly EventId _eventId = new EventId(0, "Ping");

        public override async Task Handler(DiscordClient sender, MessageCreateEventArgs e)
        {
            string messageContent = e.Message.Content.Trim();

            if ($"<@{sender.CurrentUser.Id}>".Equals(messageContent) ||
                messageContent.Equals(Settings.Settings.Current.BotPrefix, StringComparison.InvariantCultureIgnoreCase))
            {
                _defaultLogger.LogInformation(_eventId, $"{e.Guild.Name}, {e.Channel.Name}, {e.Message.Id}");

                DiscordMember discordMember = await e.Message.Channel.Guild.GetMemberAsync(e.Message.Author.Id);
                await e.Message.RespondAsync(
                    $"**Виляет хвостиком и смотрит на {discordMember.DisplayName} в ожидании команды**");
            }
        }
    }
}