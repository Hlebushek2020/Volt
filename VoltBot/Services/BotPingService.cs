using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using System;
using System.Threading.Tasks;

namespace VoltBot.Services
{
    internal class BotPingService
    {
        public async Task Ping(DiscordClient sender, MessageCreateEventArgs e)
        {
            string messageContent = e.Message.Content.Trim();

            if ($"<@{sender.CurrentUser.Id}>".Equals(messageContent) ||
                messageContent.Equals(Settings.Settings.Current.BotPrefix, StringComparison.InvariantCultureIgnoreCase))
            {
                DiscordMember discordMember = await e.Message.Channel.Guild.GetMemberAsync(e.Message.Author.Id);
                await e.Message.RespondAsync($"**Виляет хвостиком и смотрит на {discordMember.DisplayName} в ожидании команды**");
            }
        }
    }
}