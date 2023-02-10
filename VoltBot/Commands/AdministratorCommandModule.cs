using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace VoltBot.Commands
{
    /// <summary>
    /// Сommand module containing only those commands that are available to server (guild) administrators
    /// </summary>
    [RequireUserPermissions(Permissions.Administrator)]
    internal class AdministratorCommandModule : VoltCommandModule
    {
        [Command("resend")]
        [Aliases("r")]
        [Description("Переслать сообщение в другой канал")]
        public async Task Forward(CommandContext ctx,
            [Description("Канал, куда необходимо переслать сообщение")]
            DiscordChannel targetChannel,
            [Description("Причина (необязательно)"), RemainingText]
            string reason = null)
        {
            await Forward(ctx, targetChannel, reason, false, false);
        }

        [Command("resend-delete")]
        [Aliases("rd")]
        [Description(
            "Переслать сообщение в другой канал и удалить его с предыдущего уведомив об этом автора сообщения")]
        public async Task ForwardAndDeleteOriginal(CommandContext ctx,
            [Description("Канал, куда необходимо переслать сообщение")]
            DiscordChannel targetChannel,
            [Description("Причина (необязательно)"), RemainingText]
            string reason = null)
        {
            await Forward(ctx, targetChannel, reason, true, true);
        }

        private async Task Forward(CommandContext ctx, DiscordChannel targetChannel,
            string reason, bool notificationAuthor, bool deleteOriginal)
        {
            EventId eventId = new EventId(0, $"Command: {ctx.Command.Name}");

            DiscordEmbedBuilder discordEmbed = new DiscordEmbedBuilder()
                .WithTitle(ctx.Member.DisplayName)
                .WithColor(EmbedConstants.ErrorColor);

            if (ctx.Message.Reference == null)
            {
                discordEmbed.WithDescription("Вы не указали сообщение, которое необходимо переслать");
                await ctx.RespondAsync(discordEmbed);
            }
            else if (targetChannel == null)
            {
                discordEmbed.WithDescription("Вы не указали канал, куда необходимо переслать сообщение");
                await ctx.RespondAsync(discordEmbed);
            }
            else
            {
                DiscordMessage forwardMessage = await ctx.Channel.GetMessageAsync(ctx.Message.Reference.Message.Id);

                discordEmbed.WithColor(EmbedConstants.SuccessColor)
                    .WithFooter(
                        $"Guild: {forwardMessage.Channel.Guild.Name}, Channel: {forwardMessage.Channel.Name}, Time: {forwardMessage.CreationTimestamp}")
                    .WithDescription(forwardMessage.Content)
                    .WithTitle(null);

                if (!string.IsNullOrEmpty(reason))
                {
                    discordEmbed.AddField("Причина перенаправления", reason);
                }

                if (forwardMessage.Author != null)
                {
                    discordEmbed.WithAuthor(
                        name: forwardMessage.Author.Username,
                        iconUrl: forwardMessage.Author.AvatarUrl);

                    if (!deleteOriginal)
                    {
                        discordEmbed.Author.Url = forwardMessage.JumpLink.ToString();
                    }
                }

                DiscordMessageBuilder newMessageBuilder = new DiscordMessageBuilder();

                newMessageBuilder.AddEmbed(discordEmbed);

                if (forwardMessage.Embeds?.Count > 0)
                {
                    newMessageBuilder.AddEmbeds(forwardMessage.Embeds);
                }

                if (forwardMessage.Attachments?.Count > 0)
                {
                    foreach (DiscordAttachment discordAttachment in forwardMessage.Attachments)
                    {
                        _defaultLogger.LogDebug(eventId,
                            $"[Attachment] Media type: {discordAttachment.MediaType ?? "none"}, File name: {discordAttachment.FileName ?? "none"}, Url: {discordAttachment.Url ?? "none"}");

                        /*if (discordAttachment.MediaType != null &&
                            discordAttachment.MediaType.StartsWith("image",
                                StringComparison.InvariantCultureIgnoreCase))
                        {
                            DiscordEmbedBuilder attacmentEmbed = new DiscordEmbedBuilder()
                                .WithColor(EmbedConstants.SuccessColor)
                                .WithImageUrl(discordAttachment.Url);
                            newMessageBuilder.AddEmbed(attacmentEmbed.Build());
                        }
                        else
                        {*/
                        if (discordAttachment.Url != null)
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
                                _defaultLogger.LogWarning(eventId, ex, "");
                            }
                        }
                        //}
                    }
                }

                DiscordMessage newMessage = await targetChannel.SendMessageAsync(newMessageBuilder);

                await ctx.Message.DeleteAsync();
                if (deleteOriginal)
                {
                    await forwardMessage.DeleteAsync();
                }

                if (forwardMessage.Author != null && notificationAuthor)
                {
                    DiscordMember discordMember = await ctx.Guild.GetMemberAsync(forwardMessage.Author.Id);
                    if (!discordMember.IsBot)
                    {
                        DiscordDmChannel discordDmChannel = await discordMember.CreateDmChannelAsync();

                        DiscordEmbedBuilder dmDiscordEmbed = new DiscordEmbedBuilder()
                            .WithColor(EmbedConstants.WarningColor)
                            .WithTitle("Пересылка сообщения")
                            .WithDescription(
                                $"Администратор сервера {ctx.Guild.Name} переслал ваше сообщение из канала {forwardMessage.Channel.Name} в канал {targetChannel.Name}. Ссылка на пересланное сообщение: {newMessage.JumpLink}");

                        DiscordMessage discordDmMessage = await discordDmChannel.SendMessageAsync(dmDiscordEmbed);

                        DiscordEmoji emoji = DiscordEmoji.FromName(ctx.Client, ":negative_squared_cross_mark:", false);
                        await discordDmMessage.CreateReactionAsync(emoji);
                    }
                }
            }
        }
    }
}