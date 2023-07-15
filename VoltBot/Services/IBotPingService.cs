using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.EventArgs;

namespace VoltBot.Services;

public interface IBotPingService
{
    Task Handler(DiscordClient sender, MessageCreateEventArgs e);
}