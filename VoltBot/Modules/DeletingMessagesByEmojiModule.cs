using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using VoltBot.Modules;

namespace VoltBot.Services
{
    internal class DeletingMessagesByEmojiModule : HandlerModule<MessageReactionAddEventArgs>
    {
        private readonly EventId _eventId = new EventId(0, "Deleting Messages By Emoji");

        public override async Task Handler(DiscordClient sender, MessageReactionAddEventArgs e)
        {
            DiscordEmoji emoji = DiscordEmoji.FromName(sender, Constants.DeleteMessageEmoji, false);

            if (e.Emoji.Equals(emoji) && !e.User.Id.Equals(sender.CurrentUser.Id))
            {
                _defaultLogger.LogInformation(_eventId,
                    $"{e.User.Username}#{e.User.Discriminator}{
                        (e.Guild != null ? $", {e.Guild.Name}, {e.Channel.Name}" : $"")}, {e.Message.Id}");
                if (e.Guild != null)
                {
                    DiscordMessage currentMessage = await e.Channel.GetMessageAsync(e.Message.Id);
                    DiscordMember discordMember = await e.Guild.GetMemberAsync(e.User.Id);
                    if (discordMember.Permissions.HasPermission(Permissions.Administrator) ||
                        currentMessage.ReferencedMessage?.Author.Id == e.User.Id)
                    {
                        List<DiscordMessage> discordMessages = new List<DiscordMessage>();
                        discordMessages.AddRange(await e.Channel.GetMessagesAfterAsync(e.Message.Id, 4));
                        discordMessages.Add(currentMessage);
                        for (int numMessage = discordMessages.Count - 1; numMessage >= 0; numMessage--)
                        {
                            DiscordMessage message = discordMessages[numMessage];
                            if (message.Author.Id.Equals(sender.CurrentUser.Id))
                            {
                                await message.DeleteAsync();
                                //e.Handled = true;
                            }
                            else
                            {
                                numMessage = -100;
                            }
                        }
                    }
                }
                else
                {
                    await e.Message.DeleteAsync();
                    //e.Handled = true;
                }
            }
        }
    }
}