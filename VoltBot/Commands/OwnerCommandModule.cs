using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace VoltBot.Commands
{
    /// <summary>
    /// Command module containing only those commands that are available to the bot owner
    /// </summary>
    [RequireOwner]
    internal class OwnerCommandModule : VoltCommandModule
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
                .WithColor(Constants.StatusColor)
                .AddField("Net", $"v{Environment.Version}")
                .AddField("DSharpPlus", $"v{ctx.Client.VersionString}")
                .AddField("Сборка",
                    $"v{Program.Version} {File.GetCreationTime(Assembly.GetExecutingAssembly().Location):dd.MM.yyyy}")
                .AddField("Дата запуска",
                    $"{Bot.Current.StartDateTime:dd.MM.yyyy} {Bot.Current.StartDateTime:HH:mm:ss zzz}");

            TimeSpan timeSpan = DateTime.Now - Bot.Current.StartDateTime;

            discordEmbed.AddField("Время работы",
                $"{timeSpan.Days}d, {timeSpan.Hours}h, {timeSpan.Minutes}m, {timeSpan.Seconds}s");

            await ctx.RespondAsync(discordEmbed);
        }
    }
}