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
using VoltBot.Database;
using VoltBot.Database.Entities;

namespace VoltBot.Commands
{
    /// <summary>
    /// Command module containing only those commands that are available to server (guild) administrators
    /// </summary>
    [RequireUserPermissions(Permissions.Administrator)]
    internal class AdministratorCommandModule : BaseCommandModule
    {
        private readonly ISettings _settings;
        private readonly VoltDbContext _dbContext;
        private readonly ILogger<AdministratorCommandModule> _logger;

        public AdministratorCommandModule(
            VoltDbContext dbContext,
            ISettings settings,
            ILogger<AdministratorCommandModule> logger)
        {
            _dbContext = dbContext;
            _settings = settings;
            _logger = logger;
        }

        #region Forward Commands
        [Command("resend")]
        [Aliases("r")]
        [Description("Переслать сообщение в другой канал.")]
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
            "Переслать сообщение в другой канал и удалить его с предыдущего уведомив об этом автора сообщения.")]
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

            if (_settings.BugReport)
            {
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
                            _logger.LogWarning($"Failed to download attachment. Message: {ex.Message}.");
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

                    DiscordGuild reportGuild = await ctx.Client.GetGuildAsync(_settings.BugReportServer);
                    DiscordChannel reportChannel = reportGuild.GetChannel(_settings.BugReportChannel);

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
        [Description("Задать канал для отправки системных уведомлений.")]
        public async Task SetNotificationChannel(
            CommandContext ctx,
            [Description("Канал")] DiscordChannel target)
        {
            DiscordEmbedBuilder discordEmbed = new DiscordEmbedBuilder()
                .WithTitle(ctx.Member.DisplayName)
                .WithDescription("Канал установлен!")
                .WithColor(Constants.SuccessColor);

            GuildSettings guildSettings = await _dbContext.GuildSettings.FindAsync(ctx.Guild.Id);

            if (guildSettings == null)
            {
                guildSettings = new GuildSettings { GuildId = ctx.Guild.Id };
                _dbContext.GuildSettings.Add(guildSettings);
            }

            guildSettings.NotificationChannelId = target.Id;

            _dbContext.SaveChanges();

            await ctx.RespondAsync(discordEmbed);
        }

        [Command("on-notification")]
        [Aliases("on-notif")]
        [Description("Включить / Отключить уведомление о включении бота.")]
        public async Task ReadyNotification(
            CommandContext ctx,
            [Description("true - включить / false - выключить")]
            bool isEnabled)
        {
            DiscordEmbedBuilder discordEmbed = new DiscordEmbedBuilder()
                .WithTitle(ctx.Member.DisplayName)
                .WithDescription("Канал для отправки системных уведомлений не установлен!")
                .WithColor(Constants.ErrorColor);

            GuildSettings guildSettings = await _dbContext.GuildSettings.FindAsync(ctx.Guild.Id);

            if (guildSettings?.NotificationChannelId != null)
            {
                if (guildSettings.IsReadyNotification != isEnabled)
                {
                    guildSettings.IsReadyNotification = isEnabled;
                    await _dbContext.SaveChangesAsync();
                }

                discordEmbed.WithDescription($"Уведомления о включении бота {(isEnabled ? "включены" : "отключены")}!")
                    .WithColor(Constants.SuccessColor);
            }

            await ctx.RespondAsync(discordEmbed);
        }

        [Command("off-notification")]
        [Aliases("off-notif")]
        [Description("Включить / Отключить уведомление о выключении бота.")]
        public async Task ShutdownNotification(
            CommandContext ctx,
            [Description("true - включить / false - выключить")]
            bool isEnabled)
        {
            DiscordEmbedBuilder discordEmbed = new DiscordEmbedBuilder()
                .WithTitle(ctx.Member.DisplayName)
                .WithDescription("Канал для отправки системных уведомлений не установлен!")
                .WithColor(Constants.ErrorColor);

            GuildSettings guildSettings = await _dbContext.GuildSettings.FindAsync(ctx.Guild.Id);

            if (guildSettings?.NotificationChannelId != null)
            {
                if (guildSettings.IsShutdownNotification != isEnabled)
                {
                    guildSettings.IsShutdownNotification = isEnabled;
                    await _dbContext.SaveChangesAsync();
                }

                discordEmbed.WithDescription($"Уведомления о выключении бота {(isEnabled ? "включены" : "отключены")}!")
                    .WithColor(Constants.SuccessColor);
            }

            await ctx.RespondAsync(discordEmbed);
        }
        #endregion

        #region History
        [Command("checking-history")]
        [Aliases("ch-hist")]
        [Description("Включить / Отключить управление историями.")]
        public async Task CheckingHistory(
            CommandContext ctx,
            [Description("true - включить / false - выключить")]
            bool isEnabled)
        {
            DiscordEmbedBuilder discordEmbed = new DiscordEmbedBuilder()
                .WithTitle(ctx.Member.DisplayName)
                .WithDescription("Канал историй не установлен!")
                .WithColor(Constants.ErrorColor);

            GuildSettings guildSettings = await _dbContext.GuildSettings.FindAsync(ctx.Guild.Id);

            if (guildSettings?.HistoryChannelId != null)
            {
                if (guildSettings.HistoryModuleIsEnabled != isEnabled)
                {
                    guildSettings.HistoryModuleIsEnabled = isEnabled;
                    await _dbContext.SaveChangesAsync();
                }

                discordEmbed.WithDescription($"Управление историями {(isEnabled ? "включено" : "отключено")}!")
                    .WithColor(Constants.SuccessColor);
            }

            await ctx.RespondAsync(discordEmbed);
        }

        [Command("checking-history-settings")]
        [Aliases("hist-settings")]
        [Description("Задать настройки для управления историями.")]
        public async Task CheckingHistorySettings(
            CommandContext ctx,
            [Description("Канал историй")] DiscordChannel historyChannel,
            [Description("Количество допустимых слов для добавления за сообщение (Допустимые значения: 1 - 255)")]
            byte wordCount,
            [Description("Канал для уведомлений о некорректном сообщении")]
            DiscordChannel adminNotificationChannel,
            [Description("Пингуемая роль при некорректном сообщении")]
            DiscordRole adminPingRole)
        {
            if (wordCount == 0)
                throw new ArgumentException("", nameof(wordCount));

            DiscordEmbedBuilder discordEmbed = new DiscordEmbedBuilder()
                .WithTitle(ctx.Member.DisplayName)
                .WithDescription("Настройки установлены!")
                .WithColor(Constants.SuccessColor);

            GuildSettings guildSettings = await _dbContext.GuildSettings.FindAsync(ctx.Guild.Id);

            if (guildSettings == null)
            {
                guildSettings = new GuildSettings { GuildId = ctx.Guild.Id };
                _dbContext.GuildSettings.Add(guildSettings);
            }

            guildSettings.HistoryChannelId = historyChannel.Id;
            guildSettings.HistoryWordCount = wordCount;
            guildSettings.HistoryAdminNotificationChannelId = adminNotificationChannel.Id;
            guildSettings.HistoryAdminPingRole = adminPingRole.Id;

            await _dbContext.SaveChangesAsync();

            await ctx.RespondAsync(discordEmbed);
        }

        [Command("history-start-message")]
        [Aliases("hist-sm")]
        [Description("Задает текст вложенного сообщения как текст, сигнализирующий о начале новой истории.")]
        public async Task HistoryStartMessage(CommandContext ctx)
        {
            DiscordEmbedBuilder discordEmbed = new DiscordEmbedBuilder()
                .WithTitle(ctx.Member.DisplayName)
                .WithDescription("Канал историй не установлен!")
                .WithColor(Constants.ErrorColor);

            if (ctx.Message.ReferencedMessage == null)
            {
                discordEmbed.WithDescription("Данная команда должна быть ответом на сообщение!");
            }
            else if (ctx.Message.ReferencedMessage.Content.Length > 256)
            {
                discordEmbed.WithDescription("Текст не может быть больше чем 256 символов!");
            }
            else
            {
                GuildSettings guildSettings = await _dbContext.GuildSettings.FindAsync(ctx.Guild.Id);

                if (guildSettings != null)
                {
                    guildSettings.HistoryStartMessage = ctx.Message.ReferencedMessage.Content;

                    await _dbContext.SaveChangesAsync();

                    discordEmbed.WithDescription("Текст задан!")
                        .WithColor(Constants.SuccessColor);
                }
            }

            await ctx.RespondAsync(discordEmbed);
        }
        #endregion

        [Command("show-settings")]
        [Aliases("settings")]
        [Description("Показать текущие настройки для гильдии (сервера).")]
        public async Task ShowSettings(CommandContext ctx)
        {
            DiscordEmbedBuilder discordEmbed = new DiscordEmbedBuilder()
                .WithTitle(ctx.Member.DisplayName)
                .WithColor(Constants.SuccessColor);

            GuildSettings guildSettings = await _dbContext.GuildSettings.FindAsync(ctx.Guild.Id);

            if (guildSettings != null)
            {
                if (guildSettings.NotificationChannelId.HasValue)
                    discordEmbed.AddField(
                        "Канал для системных уведомлений",
                        guildSettings.NotificationChannelId.ToString());

                discordEmbed.AddField(
                    "Уведомление о включении бота",
                    guildSettings.IsReadyNotification.ToString());

                discordEmbed.AddField(
                    "Уведомление о выключении бота",
                    guildSettings.IsShutdownNotification.ToString());

                if (guildSettings.HistoryChannelId.HasValue)
                    discordEmbed.AddField(
                        "Канал историй",
                        guildSettings.HistoryChannelId.ToString());

                if (guildSettings.HistoryWordCount.HasValue)
                    discordEmbed.AddField(
                        "Количество допустимых слов",
                        guildSettings.HistoryWordCount.ToString());

                if (guildSettings.HistoryAdminNotificationChannelId.HasValue)
                    discordEmbed.AddField(
                        "Канал уведомлений о некорректном сообщении (история)",
                        guildSettings.HistoryAdminNotificationChannelId.ToString());

                if (guildSettings.HistoryAdminPingRole.HasValue)
                    discordEmbed.AddField(
                        "Пингуемая роль при некорректном сообщении (история)",
                        guildSettings.HistoryAdminPingRole.ToString());

                discordEmbed.AddField(
                    "Управление историями",
                    guildSettings.HistoryModuleIsEnabled.ToString());

                if (!string.IsNullOrWhiteSpace(guildSettings.HistoryStartMessage))
                    discordEmbed.AddField(
                        "Сообщение о начале новой истории",
                        $"```\n{guildSettings.HistoryStartMessage}\n```");
            }

            if (discordEmbed.Fields.Count > 0)
                await ctx.RespondAsync(discordEmbed);
        }

        #region NOT COMMAND
        private async Task Forward(
            CommandContext ctx,
            DiscordChannel targetChannel,
            string reason,
            bool notificationAuthor,
            bool deleteOriginal)
        {
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
                        _logger.LogDebug(
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
                            _logger.LogWarning($"Failed to download attachment. Message: {ex.Message}.");
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