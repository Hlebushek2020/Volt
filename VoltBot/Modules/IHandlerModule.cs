using DSharpPlus;
using System.Threading.Tasks;

namespace VoltBot.Modules
{
    internal interface IHandlerModule<THandlerArgs>
    {
        Task Handler(DiscordClient sender, THandlerArgs e);
    }
}