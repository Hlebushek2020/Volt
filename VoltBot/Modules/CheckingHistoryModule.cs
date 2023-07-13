using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using VoltBot.Database;
using VoltBot.Database.Entities;

namespace VoltBot.Modules
{
    internal class CheckingHistoryModule : HandlerModule<MessageCreateEventArgs>
    {
        private static readonly EventId _eventId = new EventId(0, "Checking history");

        public override async Task Handler(DiscordClient sender, MessageCreateEventArgs e)
        {
            using VoltDbContext dbContext = new VoltDbContext();

            GuildSettings guildSettings = await dbContext.GuildSettings.FindAsync(e.Guild.Id);

            if (guildSettings != null && guildSettings.HistoryModuleIsEnabled &&
                guildSettings.HistoryChannelId == e.Channel.Id)
            {
                DefaultLogger.LogInformation(
                    _eventId,
                    $"{e.Message.Author.Username}#{e.Message.Author.Discriminator} {e.Message.JumpLink}");
                IReadOnlyList<DiscordMessage> beforeMessages = await e.Channel.GetMessagesBeforeAsync(e.Message.Id, 1);
                DiscordMessage beforeMessage = beforeMessages.FirstOrDefault();
                string[] beforeParts = beforeMessage.Content.Replace("  ", " ").Split(' ');
                string[] currentParts = e.Message.Content.Replace("  ", " ").Split(' ');
                if (beforeParts.Length < currentParts.Length - guildSettings.HistoryWordCount)
                {
                    DiscordChannel discordChannel =
                        await sender.GetChannelAsync(guildSettings.HistoryAdminNotificationChannelId.Value);

                    DiscordEmbedBuilder discordEmbed = new DiscordEmbedBuilder()
                        .WithTitle(_eventId.Name)
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