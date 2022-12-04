using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Converters;
using DSharpPlus.CommandsNext.Entities;
using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VoltBot.Commands.Formatter
{
    internal class CustomHelpFormatter : BaseHelpFormatter
    {
        private readonly DiscordEmbedBuilder _embed;

        public CustomHelpFormatter(CommandContext ctx) : base(ctx)
        {
            _embed = new DiscordEmbedBuilder()
                .WithColor(DiscordColor.Rose);
        }

        public override CommandHelpMessage Build()
        {
            return new CommandHelpMessage(embed: _embed);
        }

        public override BaseHelpFormatter WithCommand(Command command)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < command.Overloads.Count; i++)
            {
                CommandOverload commandOverload = command.Overloads[i];

                if (command.Overloads.Count > 1)
                    sb.AppendLine($"**__Вариант {i + 1}__**");

                sb.AppendLine($"```{Settings.Settings.Current.BotPrefix}{command.QualifiedName} {string.Join(' ', commandOverload.Arguments.Select(x => $"[{x.Name}]").ToList())}```\n{command.Description}");
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
            StringBuilder commandSb = new StringBuilder();
            foreach (Command command in commands)
            {
                if (!command.RunChecksAsync(Context, true).Result.Any())
                {
                    if (commandSb.Length > 0)
                    {
                        commandSb.Append("; ");
                    }
                    commandSb.Append("`");
                    commandSb.Append(command.Name);
                    if (command.Aliases.Count > 0)
                    {
                        commandSb.Append(" (");
                        commandSb.AppendJoin(", ", command.Aliases);
                        commandSb.Append(")");
                    }
                    commandSb.Append("`");
                }
            }
            _embed.WithTitle("help")
                .WithDescription(Settings.Settings.Current.BotDescription)
                .AddField("Список команд:", commandSb.Length > 0 ? commandSb.ToString() : "Нет доступных команд");
            return this;
        }
    }
}