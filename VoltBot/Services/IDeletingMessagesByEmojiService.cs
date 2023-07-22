using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.EventArgs;

namespace VoltBot.Services;

public interface IDeletingMessagesByEmojiService
{
    Task Handler(DiscordClient sender, MessageReactionAddEventArgs e);
}