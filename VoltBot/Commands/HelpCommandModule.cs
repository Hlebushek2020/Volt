using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;

namespace VoltBot.Commands;

internal class HelpCommandModule : BaseCommandModule
{
    private readonly ISettings _settings;
    public HelpCommandModule(ISettings settings) { _settings = settings; }

    [Command("help")]
    [Description(
        "Показать список команд, если для команды не указан аргумент. Если в качестве аргумента указана команда, то показывает ее полное описание.")]
    public async Task Help(
        CommandContext ctx,
        [Description("Команда (Необязательно)")]
        string command = null)
    {
        if (command != null)
        {
            Command commandObj = ctx.CommandsNext.FindCommand(command, out string args);

            if (commandObj == null)
                throw new CommandNotFoundException(command);

            IEnumerable<CheckBaseAttribute> failedChecks = await commandObj.RunChecksAsync(ctx, true);
            if (failedChecks.Any())
                throw new ChecksFailedException(commandObj, ctx, failedChecks);

            StringBuilder descriptionBuilder = new StringBuilder();

            bool countOverloads = commandObj.Overloads.Count > 1;

            for (int i = 0; i < commandObj.Overloads.Count; i++)
            {
                CommandOverload commandOverload = commandObj.Overloads[i];

                if (countOverloads)
                {
                    descriptionBuilder.AppendLine($"**__Вариант {i + 1}__**");
                }

                descriptionBuilder
                    .AppendLine(
                        $"```\n{_settings.BotPrefix}{commandObj.Name} {string.Join(
                            ' ',
                            commandOverload.Arguments.Select(x => $"[{x.Name}]").ToList())}```{commandObj.Description}")
                    .AppendLine();

                if (commandObj.Aliases?.Count != 0)
                {
                    descriptionBuilder.AppendLine("**Алиасы:**");
                    foreach (string alias in commandObj.Aliases)
                    {
                        descriptionBuilder.Append($"{alias} ");
                    }
                    descriptionBuilder.AppendLine().AppendLine();
                }

                if (commandOverload.Arguments.Count != 0)
                {
                    descriptionBuilder.AppendLine("**Аргументы:**");
                    foreach (CommandArgument argument in commandOverload.Arguments)
                    {
                        string defaultValue = (argument.DefaultValue != null)
                            ? $" (Необязательно, по умолчанию: {argument.DefaultValue})"
                            : string.Empty;
                        descriptionBuilder.AppendLine(
                            $"`{argument.Name}`: {argument.Description}{defaultValue}");
                    }
                    descriptionBuilder.AppendLine();
                }
            }

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                .WithTitle(commandObj.Name)
                .WithDescription(descriptionBuilder.ToString())
                .WithColor(Constants.SuccessColor)
                .WithFooter($"v{Program.Version}");

            await ctx.RespondAsync(embed);
        }
        else
        {
            IEnumerable<Command> commands = ctx.CommandsNext.RegisteredCommands.Values.Distinct()
                .Where(x => !x.IsHidden && !x.RunChecksAsync(ctx, true).Result.Any());

            Dictionary<string, string> aviableCommands = new Dictionary<string, string>();
            foreach (Command commandObj in commands)
            {

                string key = commandObj.Name;
                if (commandObj.Aliases.Count > 0)
                {
                    key += $" ({string.Join(", ", commandObj.Aliases)})";
                }
                aviableCommands.Add(key, commandObj.Description);

            }

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
                .WithTitle("help")
                .WithDescription(_settings.BotDescription)
                .WithColor(Constants.SuccessColor)
                .WithFooter($"v{Program.Version}");

            const string commandListText = "Список команд";

            if (aviableCommands.Count == 0)
            {
                embed.AddField(commandListText, "Нет доступных команд");
            }
            else
            {
                embed.AddField(commandListText, new string('=', commandListText.Length));
                foreach (string commandKey in aviableCommands.Keys)
                {
                    embed.AddField(commandKey, aviableCommands[commandKey]);
                }
            }

            await ctx.RespondAsync(embed);
        }
    }
}