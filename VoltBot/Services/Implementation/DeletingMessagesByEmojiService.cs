using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;

namespace VoltBot.Services.Implementation
{
    internal class DeletingMessagesByEmojiService : IDeletingMessagesByEmojiService
    {
        private readonly ILogger<DeletingMessagesByEmojiService> _logger;

        public DeletingMessagesByEmojiService(
            DiscordClient discordClient,
            ILogger<DeletingMessagesByEmojiService> logger)
        {
            _logger = logger;

            discordClient.MessageReactionAdded += Handler;

            _logger.LogInformation($"{nameof(DeletingMessagesByEmojiService)} loaded.");
        }

        public async Task Handler(DiscordClient sender, MessageReactionAddEventArgs e)
        {
            DiscordEmoji emoji = DiscordEmoji.FromName(sender, Constants.DeleteMessageEmoji, false);

            if (e.Emoji.Equals(emoji) && !e.User.Id.Equals(sender.CurrentUser.Id))
            {
                _logger.LogInformation(
                    e.Guild != null
                        ? $"Guild: {e.Guild.Name} ({e.Guild.Id}). Channel: {e.Channel.Name} ({e.Channel.Id})."
                        : $"Username: {e.User.Username}");
                if (e.Guild != null)
                {
                    DiscordMessage currentMessage = await e.Channel.GetMessageAsync(e.Message.Id);
                    DiscordMember discordMember = await e.Guild.GetMemberAsync(e.User.Id);
                    if (discordMember.Permissions.HasPermission(Permissions.Administrator) ||
                        currentMessage.ReferencedMessage?.Author.Id == e.User.Id)
                    {
                        List<DiscordMessage> discordMessages = new List<DiscordMessage>();
                        discordMessages.AddRange(
                            e.Channel
                                .GetMessagesAfterAsync(e.Message.Id, 4)
                                .ToBlockingEnumerable());
                        discordMessages.Add(currentMessage);
                        for (int numMessage = discordMessages.Count - 1; numMessage >= 0; numMessage--)
                        {
                            DiscordMessage message = discordMessages[numMessage];
                            if (message.Author.Id.Equals(sender.CurrentUser.Id))
                            {
                                await message.DeleteAsync();
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
                }
            }
        }
    }
}