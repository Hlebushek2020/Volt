using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using System.Threading.Tasks;

namespace VoltBot.Commands
{
    internal class UserCommandModule : BaseCommandModule
    {
        [Command("redirect")]
        [Aliases("rd")]
        [Description("redirect")]
        public async Task Redirect(CommandContext ctx)
        {
            await ctx.RespondAsync("NotImplemented");
        }
    }
}
