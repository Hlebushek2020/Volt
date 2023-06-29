using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace VoltBot.Commands;

internal class HelpCommandModule : VoltCommandModule
{
    [Command("help")]
    [Description(
        "Показать список команд, если для команды не указан аргумент. Если в качестве аргумента указана команда, то показывает ее полное описание.")]
    public async Task Help(
        CommandContext ctx,
        [Description("Команда (Необязательно)")]
        string command = null)
    {
        if (command != null) { }
        else
        {
            IEnumerable<Command> commands = ctx.CommandsNext.RegisteredCommands.Values.Distinct()
                .Where(x => !x.IsHidden && !x.RunChecksAsync(ctx, true).Result.Any());
        }
    }
}