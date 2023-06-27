using System.Collections.Generic;
using System.Linq;
using System.Text;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.Entities;

namespace VoltBot.Commands.Formatter
{
    internal class CustomHelpFormatter : BaseHelpFormatter
    {
        private readonly DiscordEmbedBuilder _embed;

        public CustomHelpFormatter(CommandContext ctx) : base(ctx)
        {
            _embed = new DiscordEmbedBuilder()
                .WithColor(Constants.SuccessColor)
                .WithFooter($"v{Program.Version}");
        }

        public override CommandHelpMessage Build() { return new CommandHelpMessage(embed: _embed); }

        public override BaseHelpFormatter WithCommand(Command command)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < command.Overloads.Count; i++)
            {
                CommandOverload commandOverload = command.Overloads[i];

                if (command.Overloads.Count > 1)
                    sb.AppendLine($"**__Вариант {i + 1}__**");

                sb.AppendLine(
                    $"```{Settings.Settings.Current.BotPrefix}{command.QualifiedName} {
                        string.Join(' ', commandOverload.Arguments.Select(x => $"[{x.Name}]").ToList())}```\n{
                            command.Description}");
                sb.AppendLine();

                if (command.Aliases?.Count != 0)
                {
                    sb.AppendLine("**Алиасы:**")
                        .AppendJoin(", ", command.Aliases.Select(x => $"`{x}`"))
                        .AppendLine().AppendLine();
                }

                if (commandOverload?.Arguments.Count != 0)
                {
                    sb.AppendLine("**Аргументы:**");
                    foreach (var c in commandOverload.Arguments)
                        sb.AppendLine($"`{c.Name}`: {c.Description}");
                    sb.AppendLine();
                }
            }

            _embed.WithTitle($"help: {command.Name}")
                .WithDescription(sb.ToString());

            return this;
        }

        public override BaseHelpFormatter WithSubcommands(IEnumerable<Command> commands)
        {
            Dictionary<string, string> aviableCommands = new Dictionary<string, string>();
            foreach (Command command in commands)
            {
                if (!command.RunChecksAsync(Context, true).Result.Any())
                {
                    string key = command.Name;
                    if (command.Aliases.Count > 0)
                    {
                        key += $" ({string.Join(", ", command.Aliases)})";
                    }
                    aviableCommands.Add(key, command.Description);
                }
            }

            // aviableCommands["help"] = "Отображает информацию по команде";

            _embed.WithTitle("help")
                .WithDescription(Settings.Settings.Current.BotDescription);

            if (aviableCommands.Count == 0)
            {
                _embed.AddField("Список команд", "Нет доступных команд");
            }
            else
            {
                _embed.AddField("Список команд", new string('=', 13));
                foreach (string commandKey in aviableCommands.Keys)
                {
                    _embed.AddField(commandKey, aviableCommands[commandKey]);
                }
            }

            return this;
        }
    }
}