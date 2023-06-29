using System;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using VoltBot.Modules.Notifications;

namespace VoltBot.Commands
{
    /// <summary>
    /// Сommand module containing only those commands that are available to server (guild) administrators
    /// </summary>
    [RequireUserPermissions(Permissions.Administrator)]
    internal class AdministratorCommandModule : VoltCommandModule
    {
        #region Forward Commands
        [Command("resend")]
        [Aliases("r")]
        [Description("Переслать сообщение в другой канал")]
        public async Task Forward(
            CommandContext ctx,
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
        public async Task ForwardAndDeleteOriginal(
            CommandContext ctx,
            [Description("Канал, куда необходимо переслать сообщение")]
            DiscordChannel targetChannel,
            [Description("Причина (необязательно)"), RemainingText]
            string reason = null)
        {
            await Forward(ctx, targetChannel, reason, true, true);
        }
        #endregion

        #region Command: bug-report
        [Command("bug-report")]
        [Description(
            "Сообщить об ошибке. Убедительная просьба прикладывать как можно больше информации об ошибке (действия которые к ней привели, скриншоты и т.д.) к сообщению с данной командой.")]
        public async Task BugReport(
            CommandContext ctx,
            [Description("Описание ошибки (необязательно)"), RemainingText]
            string description)
        {
            DiscordEmbedBuilder discordEmbed = new DiscordEmbedBuilder()
                .WithTitle(ctx.Member.DisplayName)
                .WithColor(Constants.SuccessColor);

            if (Settings.BugReport)
            {
                EventId eventId = new EventId(0, $"Command: {ctx.Command.Name}");
                DiscordMessage discordMessage = ctx.Message;

                DiscordMessage referencedMessage = discordMessage.ReferencedMessage;

                if (referencedMessage == null && discordMessage.Attachments.Count == 0 &&
                    string.IsNullOrEmpty(description))
                {
                    discordEmbed.WithColor(Constants.ErrorColor)
                        .WithDescription(
                            "Пустой баг-репорт! Баг-репорт должен содержать описание и/или вложения и/или быть ответом на другое сообщение!");
                }
                else
                {
                    DiscordEmbedBuilder reportEmbed = new DiscordEmbedBuilder()
                        .WithColor(Constants.SuccessColor)
                        .WithTitle("Bug-Report")
                        .AddField("Author", ctx.User.Username + "#" + ctx.User.Discriminator)
                        .AddField("Guild", ctx.Guild.Name)
                        .AddField(
                            "Date",
                            discordMessage.CreationTimestamp.LocalDateTime.ToString("dd.MM.yyyy HH:mm:ss"));

                    DiscordMessageBuilder reportMessage = new DiscordMessageBuilder().WithEmbed(reportEmbed);

                    bool hasDescription = !string.IsNullOrEmpty(description);
                    if (hasDescription)
                    {
                        reportMessage.WithContent($"**Description:** {description}");
                    }

                    foreach (DiscordAttachment attachment in discordMessage.Attachments)
                    {
                        try
                        {
                            using HttpClient client = new HttpClient();
                            Stream fileStream = await client.GetStreamAsync(attachment.Url);
                            string fileName = attachment.FileName ?? Guid.NewGuid().ToString();
                            reportMessage.AddFile(fileName, fileStream);
                        }
                        catch (Exception ex)
                        {
                            DefaultLogger.LogWarning(eventId, ex, string.Empty);
                        }
                    }

                    if (referencedMessage != null)
                    {
                        if (!string.IsNullOrEmpty(referencedMessage.Content))
                        {
                            string referencedMessageContent = referencedMessage.Content;
                            if (hasDescription)
                            {
                                if (referencedMessageContent.Length > 1024)
                                    referencedMessageContent = referencedMessageContent[..1024];
                                reportEmbed.AddField("Reference Message", referencedMessageContent);
                            }
                            else
                            {
                                reportMessage.WithContent($"**Reference Message:** {referencedMessageContent}");
                            }
                        }

                        if (referencedMessage.Embeds != null)
                        {
                            reportMessage.AddEmbeds(referencedMessage.Embeds);
                        }
                    }

                    DiscordGuild reportGuild = await ctx.Client.GetGuildAsync(Settings.BugReportServer);
                    DiscordChannel reportChannel = reportGuild.GetChannel(Settings.BugReportChannel);

                    await reportChannel.SendMessageAsync(reportMessage);

                    discordEmbed.WithDescription("Баг-репорт успешно отправлен!");
                }
            }
            else
            {
                discordEmbed.WithDescription("Эта команда отключена!");
            }

            await ctx.RespondAsync(discordEmbed);
        }
        #endregion

        #region Notifications command
        [Command("notification-channel")]
        [Aliases("notif-channel")]
        [Description("Задать канал для отправки системных уведомлений")]
        public async Task SetNotificationChannel(
            CommandContext ctx,
            [Description("Канал")]
            DiscordChannel target)
        {
            DiscordEmbedBuilder discordEmbed = new DiscordEmbedBuilder()
                .WithTitle(ctx.Member.DisplayName)
                .WithDescription("Канал установлен!")
                .WithColor(Constants.SuccessColor);

            GuildNotification guildNotification = new GuildNotification(ctx.Guild.Id, target.Id);
            if (BotNotificationsController.Current.Get(ctx.Guild.Id, out GuildNotification existGuildNotification))
            {
                guildNotification = new GuildNotification(
                    ctx.Guild.Id,
                    target.Id,
                    existGuildNotification.IsReady,
                    existGuildNotification.IsShutdown);
            }

            BotNotificationsController.Current.AddOrUpdate(guildNotification);

            await ctx.RespondAsync(discordEmbed);
        }

        [Command("on-notification")]
        [Aliases("on-notif")]
        [Description("Включить / Отключить уведомление о включении бота")]
        public async Task ReadyNotification(
            CommandContext ctx,
            [Description("true - включить / false - выключить")]
            bool isEnabled)
        {
            DiscordEmbedBuilder discordEmbed = new DiscordEmbedBuilder()
                .WithTitle(ctx.Member.DisplayName)
                .WithDescription("Канал для отправки системных уведомлений не установлен!")
                .WithColor(Constants.ErrorColor);

            if (BotNotificationsController.Current.Get(ctx.Guild.Id, out GuildNotification guildNotification))
            {
                if (guildNotification.IsReady != isEnabled)
                {
                    GuildNotification newGuildNotification = new GuildNotification(
                        guildNotification.GuildId,
                        guildNotification.ChannelId,
                        isEnabled,
                        guildNotification.IsShutdown);
                    BotNotificationsController.Current.AddOrUpdate(newGuildNotification);
                }

                discordEmbed.WithDescription($"Уведомления о включении бота {(isEnabled ? "включены" : "отключены")}!")
                    .WithColor(Constants.SuccessColor);
            }

            await ctx.RespondAsync(discordEmbed);
        }

        [Command("off-notification")]
        [Aliases("off-notif")]
        [Description("Включить / Отключить уведомление о выключении бота")]
        public async Task ShutdownNotification(
            CommandContext ctx,
            [Description("true - включить / false - выключить")]
            bool isEnabled)
        {
            DiscordEmbedBuilder discordEmbed = new DiscordEmbedBuilder()
                .WithTitle(ctx.Member.DisplayName)
                .WithDescription("Канал для отправки системных уведомлений не установлен!")
                .WithColor(Constants.ErrorColor);

            if (BotNotificationsController.Current.Get(ctx.Guild.Id, out GuildNotification guildNotification))
            {
                if (guildNotification.IsShutdown != isEnabled)
                {
                    GuildNotification newGuildNotification = new GuildNotification(
                        guildNotification.GuildId,
                        guildNotification.ChannelId,
                        guildNotification.IsReady,
                        isEnabled);
                    BotNotificationsController.Current.AddOrUpdate(newGuildNotification);
                }

                discordEmbed.WithDescription($"Уведомления о выключении бота {(isEnabled ? "включены" : "отключены")}!")
                    .WithColor(Constants.SuccessColor);
            }

            await ctx.RespondAsync(discordEmbed);
        }
        #endregion

        #region NOT COMMAND
        private static async Task Forward(
            CommandContext ctx,
            DiscordChannel targetChannel,
            string reason,
            bool notificationAuthor,
            bool deleteOriginal)
        {
            EventId eventId = new EventId(0, $"Command: {ctx.Command.Name}");

            DiscordEmbedBuilder discordEmbed = new DiscordEmbedBuilder()
                .WithTitle(ctx.Member.DisplayName)
                .WithColor(Constants.ErrorColor);

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

                discordEmbed.WithColor(Constants.SuccessColor)
                    .WithFooter(
                        $"Guild: {forwardMessage.Channel.Guild.Name}, Channel: {forwardMessage.Channel.Name}, Time: {
                            forwardMessage.CreationTimestamp}")
                    .WithDescription(forwardMessage.Content);

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
                DiscordMessageBuilder newMessageLinksBuilder = null;

                newMessageBuilder.AddEmbed(discordEmbed);

                if (forwardMessage.Embeds?.Count > 0)
                {
                    StringBuilder attacmentsLinks = new StringBuilder();
                    foreach (DiscordEmbed forwardMessageEmbed in forwardMessage.Embeds)
                    {
                        if (forwardMessageEmbed.Url != null &&
                            forwardMessageEmbed.Url.AbsoluteUri.StartsWith("https://cdn.discordapp.com/attachments"))
                        {
                            if (attacmentsLinks.Length > 0)
                                attacmentsLinks.AppendLine();
                            attacmentsLinks.Append(forwardMessageEmbed.Url.AbsoluteUri);
                        }
                        else
                        {
                            newMessageBuilder.AddEmbed(forwardMessageEmbed);
                        }
                    }

                    if (attacmentsLinks.Length > 0)
                    {
                        newMessageLinksBuilder = new DiscordMessageBuilder()
                            .WithContent(attacmentsLinks.ToString());
                    }
                }

                if (forwardMessage.Attachments?.Count > 0)
                {
                    foreach (DiscordAttachment discordAttachment in forwardMessage.Attachments)
                    {
                        DefaultLogger.LogDebug(
                            eventId,
                            $"[Attachment] Media type: {discordAttachment.MediaType ?? "none"}, File name: {
                                discordAttachment.FileName ?? "none"}, Url: {discordAttachment.Url ?? "none"}");

                        try
                        {
                            HttpClient client = new HttpClient();
                            Stream fileStream = await client.GetStreamAsync(discordAttachment.Url);
                            string fileName = discordAttachment.FileName ?? Guid.NewGuid().ToString();
                            newMessageBuilder.AddFile(fileName, fileStream);
                        }
                        catch (Exception ex)
                        {
                            DefaultLogger.LogWarning(eventId, ex, string.Empty);
                        }
                    }
                }

                DiscordMessage newMessage = await targetChannel.SendMessageAsync(newMessageBuilder);
                if (newMessageLinksBuilder != null)
                {
                    await newMessage.RespondAsync(newMessageLinksBuilder);
                }

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
                            .WithColor(Constants.WarningColor)
                            .WithTitle("Пересылка сообщения")
                            .WithDescription(
                                $"Администратор сервера {ctx.Guild.Name} переслал ваше сообщение из канала {
                                    forwardMessage.Channel.Name} в канал {targetChannel.Name
                                    }. Ссылка на пересланное сообщение: {newMessage.JumpLink}");

                        DiscordMessage discordDmMessage = await discordDmChannel.SendMessageAsync(dmDiscordEmbed);

                        DiscordEmoji emoji = DiscordEmoji.FromName(ctx.Client, Constants.DeleteMessageEmoji, false);
                        await discordDmMessage.CreateReactionAsync(emoji);
                    }
                }
            }
        }
        #endregion
    }
}