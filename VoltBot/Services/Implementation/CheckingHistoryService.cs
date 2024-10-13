using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using VoltBot.Database;
using VoltBot.Database.Entities;
using VoltBot.Enums;

namespace VoltBot.Services.Implementation
{
    internal class CheckingHistoryService : ICheckingHistoryService
    {
        private readonly VoltDbContext _dbContext;
        private readonly ISettings _settings;
        private readonly ILogger<CheckingHistoryService> _logger;

        public CheckingHistoryService(
            DiscordClient discordClient,
            VoltDbContext dbContext,
            ISettings settings,
            ILogger<CheckingHistoryService> logger)
        {
            _dbContext = dbContext;
            _settings = settings;
            _logger = logger;

            discordClient.MessageCreated += Handler;

            _logger.LogInformation($"{nameof(CheckingHistoryService)} loaded.");
        }

        public async Task Handler(DiscordClient sender, MessageCreateEventArgs e)
        {
            GuildSettings guildSettings = await _dbContext.GuildSettings.FindAsync(e.Guild.Id);

            if (guildSettings is { HistoryModuleIsEnabled: true } &&
                e.Channel.Id == guildSettings.HistoryChannelId)
            {
                _logger.LogInformation(
                    $"Guild {e.Guild.Name}. Channel: {e.Channel.Name}. Jump link: {e.Message.JumpLink}");

                if (guildSettings.HistoryStartMessage.Equals(e.Message.Content))
                    return;

                DiscordMessage beforeMessage = e.Channel
                    .GetMessagesBeforeAsync(e.Message.Id, 1)
                    .ToBlockingEnumerable()
                    .First();

                if (guildSettings.HistoryStartMessage.Equals(beforeMessage.Content))
                    return;

                string[] beforeParts = CleanMessage(beforeMessage.Content);
                string[] currentParts = CleanMessage(e.Message.Content);

                string breakingRule = "";

                if (beforeParts.Length < currentParts.Length - guildSettings.HistoryWordCount)
                    breakingRule += $"- {_settings.TextOfHistoryRules[HistoryRules.AddTwoWords]}";

                if (e.Message.Author.Id == beforeMessage.Author.Id)
                    breakingRule += $"- {_settings.TextOfHistoryRules[HistoryRules.TwoMessagesInRow]}";

                breakingRule = breakingRule.Trim('\r', '\n');

                if (breakingRule.Length > 0)
                {
                    DiscordChannel discordChannel =
                        await sender.GetChannelAsync(guildSettings.HistoryAdminNotificationChannelId.Value);

                    DiscordEmbedBuilder discordEmbed = new DiscordEmbedBuilder()
                        .WithTitle("Checking history")
                        .WithDescription(e.Message.JumpLink.ToString())
                        .AddField("Нарушения", breakingRule)
                        .WithColor(Constants.WarningColor);

                    RoleMention roleMention = new RoleMention(guildSettings.HistoryAdminPingRole.Value);

                    try
                    {
                        DiscordMember discordMember = await e.Guild.GetMemberAsync(e.Message.Author.Id);

                        if (discordMember.IsBot)
                            return;

                        DiscordDmChannel discordDmChannel = await discordMember.CreateDmChannelAsync();

                        DiscordEmbedBuilder dmDiscordEmbed = new DiscordEmbedBuilder()
                            .WithColor(Constants.WarningColor)
                            .WithTitle("Игра \"История\"")
                            .WithDescription(
                                $"Вы нарушили следующие правила игры: \n{breakingRule}\n\nСсылка на сообщение: {e.Message.JumpLink}");

                        await discordDmChannel.SendMessageAsync(dmDiscordEmbed);

                        discordEmbed.AddField("Уведомление", "Отправлено");
                    }
                    catch
                    {
                        discordEmbed.AddField("Уведомление", "**Не отправлено**");
                    }

                    // For ping to work, the role must be specified in the Content and Mention
                    DiscordMessageBuilder discordMessage = new DiscordMessageBuilder()
                        .WithContent($"<@&{guildSettings.HistoryAdminPingRole.Value}>")
                        .AddMention(roleMention)
                        .AddEmbed(discordEmbed.Build());

                    await discordChannel.SendMessageAsync(discordMessage);
                }
            }
        }

        private static string[] CleanMessage(string message)
        {
            return message.Split(' ')
                .Select(x => x.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x) && !"-".Equals(x))
                .ToArray();
        }
    }
}