using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.EventArgs;

namespace VoltBot.Services;

public interface IForwardingPostFromVkByUrlService
{
    Task Handler(DiscordClient sender, MessageCreateEventArgs e);
}