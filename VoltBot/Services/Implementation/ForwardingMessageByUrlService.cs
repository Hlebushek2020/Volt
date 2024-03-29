﻿using System;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;

namespace VoltBot.Services.Implementation
{
    internal class ForwardingMessageByUrlService : IForwardingMessageByUrlService
    {
        private static readonly Regex _messagePattern =
            new Regex(
                @"(?<!\\)https?:\/\/(?:ptb\.|canary\.)?discord\.com\/channels\/(\d+)\/(\d+)\/(\d+)",
                RegexOptions.Compiled);

        private readonly ILogger<ForwardingMessageByUrlService> _logger;

        public ForwardingMessageByUrlService(DiscordClient discordClient, ILogger<ForwardingMessageByUrlService> logger)
        {
            _logger = logger;

            discordClient.MessageCreated += Handler;

            _logger.LogInformation($"{nameof(ForwardingMessageByUrlService)} loaded.");
        }

        private static Tuple<ulong, ulong, ulong> GetMessageLocation(string messageText)
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

        public async Task Handler(DiscordClient sender, MessageCreateEventArgs e)
        {
            Tuple<ulong, ulong, ulong> resendMessageLocation = GetMessageLocation(e.Message.Content);

            if (resendMessageLocation != null)
            {
                _logger.LogInformation(
                    $"Guild: {e.Guild.Name}. Channel: {e.Channel.Name}. Jump link: {e.Message.JumpLink}");

                DiscordChannel discordChannel = await sender.GetChannelAsync(resendMessageLocation.Item2);
                DiscordMessage resendMessage = await discordChannel.GetMessageAsync(resendMessageLocation.Item3);

                DiscordEmbedBuilder discordEmbed = new DiscordEmbedBuilder()
                    .WithColor(Constants.SuccessColor)
                    .WithFooter(
                        $"Guild: {resendMessage.Channel.Guild.Name}, Channel: {resendMessage.Channel.Name}, Time: {
                            resendMessage.CreationTimestamp}")
                    .WithDescription(resendMessage.Content);

                if (resendMessage.Author != null)
                {
                    discordEmbed.WithAuthor(
                        name: resendMessage.Author.Username,
                        iconUrl: resendMessage.Author.AvatarUrl);
                }

                DiscordMessageBuilder newMessageBuilder = new DiscordMessageBuilder();

                newMessageBuilder.AddEmbed(discordEmbed);

                if (resendMessage.Embeds?.Count > 0)
                {
                    newMessageBuilder.AddEmbeds(resendMessage.Embeds);
                }

                if (resendMessage.Attachments?.Count > 0)
                {
                    foreach (DiscordAttachment discordAttachment in resendMessage.Attachments)
                    {
                        if (discordAttachment.MediaType != null &&
                            discordAttachment.MediaType.StartsWith(
                                "image",
                                StringComparison.InvariantCultureIgnoreCase))
                        {
                            DiscordEmbedBuilder attacmentEmbed = new DiscordEmbedBuilder()
                                .WithColor(Constants.SuccessColor)
                                .WithImageUrl(discordAttachment.Url);
                            newMessageBuilder.AddEmbed(attacmentEmbed.Build());
                        }
                        else
                        {
                            try
                            {
                                HttpClient client = new HttpClient();
                                Stream fileStream = await client.GetStreamAsync(discordAttachment.Url);
                                string fileName = discordAttachment.FileName ?? Guid.NewGuid().ToString();
                                newMessageBuilder.AddFile(fileName, fileStream);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning($"Failed to download attachment. Message: {ex.Message}.");
                            }
                        }
                    }
                }

                DiscordMessage newMessage = await e.Message.RespondAsync(newMessageBuilder);

                await newMessage.CreateReactionAsync(
                    DiscordEmoji.FromName(
                        sender,
                        Constants.DeleteMessageEmoji,
                        false));
            }
        }
    }
}