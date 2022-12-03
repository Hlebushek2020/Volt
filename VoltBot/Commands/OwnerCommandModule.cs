using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace VoltBot.Commands
{
    [RequireOwner]
    internal class OwnerCommandModule : BaseCommandModule
    {
        [Command("shutdown")]
        [Aliases("sd")]
        [Description("Выключить бота")]
        public async Task Shutdown(CommandContext ctx)
        {
            await ctx.RespondAsync("Ok");
            Bot.Current.Shutdown();
        }

        [Command("status")]
        [Description("Сведения о боте")]
        public async Task Status(CommandContext ctx)
        {
            DiscordEmbedBuilder discordEmbed = new DiscordEmbedBuilder()
            {
                Color = DiscordColor.DarkGray
            };

            discordEmbed.AddField("Net", $"v{Environment.Version}");
            discordEmbed.AddField("Сборка", $"v{Assembly.GetExecutingAssembly().GetName().Version} {File.GetCreationTime(Assembly.GetExecutingAssembly().Location):dd.MM.yyyy}");
            discordEmbed.AddField("Дата запуска", $"{Bot.Current.StartDateTime:dd.MM.yyyy} {Bot.Current.StartDateTime:HH:mm:ss zzz}");
            TimeSpan timeSpan = DateTime.Now - Bot.Current.StartDateTime;
            discordEmbed.AddField("Время работы", $"{timeSpan.Days}d, {timeSpan.Hours}h, {timeSpan.Minutes}m, {timeSpan.Seconds}s");

            await ctx.RespondAsync(discordEmbed);
        }
    }
}