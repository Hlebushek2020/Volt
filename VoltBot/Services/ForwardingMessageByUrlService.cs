using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using VoltBot.Logs;
using VoltBot.Logs.Providers;

namespace VoltBot.Services
{
    internal class ForwardingMessageByUrlService
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

        public async Task ForwardingMessageByUrl(DiscordClient sender, MessageCreateEventArgs e)
        {
            EventId eventId = new EventId(0, "Forwarding Message By Url");

            Tuple<ulong, ulong, ulong> resendMessageLocation = GetMessageLocation(e.Message.Content);

            if (resendMessageLocation != null)
            {
                _defaultLogger.LogInformation(eventId, $"{e.Guild.Name}, {e.Channel.Name}, {e.Message.Id}");

                DiscordChannel discordChannel = await sender.GetChannelAsync(resendMessageLocation.Item2);
                DiscordMessage resendMessage = await discordChannel.GetMessageAsync(resendMessageLocation.Item3);

                DiscordEmbedBuilder discordEmbed = new DiscordEmbedBuilder()
                    .WithColor(EmbedConstants.SuccessColor)
                    .WithFooter($"Guild: {resendMessage.Channel.Guild.Name}, Channel: {resendMessage.Channel.Name}, Time: {resendMessage.CreationTimestamp}")
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
                    foreach (DiscordAttachment discordAttachment in resendMessage.Attachments)
                    {
                        if (discordAttachment.MediaType.StartsWith("image", StringComparison.InvariantCultureIgnoreCase))
                        {
                            DiscordEmbedBuilder attacmentEmbed = new DiscordEmbedBuilder()
                                .WithColor(EmbedConstants.SuccessColor)
                                .WithImageUrl(discordAttachment.Url);
                            newMessage.AddEmbed(attacmentEmbed.Build());
                        }
                        else
                        {
                            if (discordAttachment.FileName != null)
                            {
                                try
                                {
                                    HttpClient client = new HttpClient();
                                    Stream fileStream = await client.GetStreamAsync(discordAttachment.Url);
                                    newMessage.AddFile(discordAttachment.FileName, fileStream);
                                }
                                catch (Exception ex)
                                {
                                    _defaultLogger.LogWarning(eventId, ex, "");
                                }
                            }
                        }
                    }
                }

                await e.Channel.SendMessageAsync(newMessage);
            }
        }
    }
}