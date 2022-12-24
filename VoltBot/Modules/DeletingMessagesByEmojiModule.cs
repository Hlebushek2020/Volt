using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using VoltBot.Logs;
using VoltBot.Logs.Providers;
using VoltBot.Modules;

namespace VoltBot.Services
{
    internal class DeletingMessagesByEmojiModule : IHandlerModule<MessageReactionAddEventArgs>
    {
        private readonly ILogger _defaultLogger = LoggerFactory.Current.CreateLogger<DefaultLoggerProvider>();
        private readonly EventId _eventId = new EventId(0, "Deleting Messages By Emoji");

        public async Task Handler(DiscordClient sender, MessageReactionAddEventArgs e)
        {
            DiscordEmoji emoji = DiscordEmoji.FromName(sender, ":negative_squared_cross_mark:", false);

            if (e.Emoji.Equals(emoji) && !e.User.Id.Equals(sender.CurrentUser.Id))
            {
                DiscordMessage discordMessage = await e.Channel.GetMessageAsync(e.Message.Id);
                if (discordMessage.Author.Id.Equals(sender.CurrentUser.Id))
                {
                    _defaultLogger.LogInformation(_eventId, $"{e.User.Username}#{e.User.Discriminator}{(e.Guild != null ? $", {e.Guild.Name}, {e.Channel.Name}" : $"")}, {e.Message.Id}");

                    if (e.Guild != null)
                    {
                        DiscordMember discordMember = await e.Guild.GetMemberAsync(e.User.Id);
                        if (discordMember.Permissions.HasPermission(Permissions.Administrator) ||
                            e.Message.ReferencedMessage?.Author.Id == e.User.Id && discordMessage.IsTTS)
                        {
                            await e.Message.DeleteAsync();
                            e.Handled = true;
                        }
                    }
                    else
                    {
                        await e.Message.DeleteAsync();
                        e.Handled = true;
                    }
                }
            }
        }
    }
}