using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VoltBot.Logs;
using VoltBot.Logs.Providers;

namespace VoltBot.Services
{
    internal class MessageResendService
    {
        private readonly ILogger _defaultLogger = LoggerFactory.Current.CreateLogger<DefaultLoggerProvider>();
        private readonly Regex _messagePattern = new Regex(@"(?<!\\)https?:\/\/(?:ptb\.|canary\.)?discord\.com\/channels\/(\d+)\/(\d+)\/(\d+)", RegexOptions.Compiled);

        private Tuple<ulong, ulong, ulong> GetMessageLocation(string messageText)
        {
            Match match = _messagePattern.Match(messageText);

            if (match == null || match.Groups.Count != 4)
                return null;

            if (ulong.TryParse(match.Groups[1].Value, out ulong guildId) &&
                ulong.TryParse(match.Groups[2].Value, out ulong channelId) &&
                ulong.TryParse(match.Groups[3].Value, out ulong messageId))
            {
                return Tuple.Create(guildId, channelId, messageId);
            }

            return null;
        }

        public async Task Resend(DiscordClient sender, MessageCreateEventArgs e)
        {
            EventId eventId = new EventId(0, "Resend");

            Tuple<ulong, ulong, ulong> resendMessageLocation = GetMessageLocation(e.Message.Content);

            if (resendMessageLocation != null)
            {
                _defaultLogger.LogInformation(eventId, $"{e.Guild.Name}, {e.Channel.Name}, {e.Message.Id}");

                DiscordMessage resendMessage = await e.Channel.GetMessageAsync(resendMessageLocation.Item3);

                DiscordEmbedBuilder discordEmbed = new DiscordEmbedBuilder()
                    .WithColor(EmbedConstants.SuccessColor)
                    .WithFooter($"Guild: {e.Guild.Name}, Channel: {e.Channel.Name}, Time: {e.Message.CreationTimestamp}")
                    .WithDescription(resendMessage.Content);

                if (resendMessage.Author != null)
                {
                    discordEmbed.WithAuthor(
                        name: resendMessage.Author.Username,
                        iconUrl: resendMessage.Author.AvatarUrl);
                }

                DiscordMessageBuilder newMessage = new DiscordMessageBuilder();

                newMessage.AddEmbed(discordEmbed);

                if (resendMessage.Embeds?.Count > 0)
                {
                    newMessage.AddEmbeds(resendMessage.Embeds);
                }

                if (resendMessage.Attachments?.Count > 0)
                {
                    newMessage.AddEmbeds(resendMessage.Attachments
                        .Select(x =>
                        {
                            DiscordEmbedBuilder attacmentEmbed = new DiscordEmbedBuilder().WithColor(EmbedConstants.SuccessColor);
                            if (x.MediaType.StartsWith("image", StringComparison.InvariantCultureIgnoreCase))
                            {
                                attacmentEmbed.WithImageUrl(x.Url);
                            }
                            else
                            {
                                attacmentEmbed.WithUrl(x.Url);
                            }
                            return attacmentEmbed.Build();
                        }));
                }

                await e.Channel.SendMessageAsync(newMessage);
            }
        }
    }
}