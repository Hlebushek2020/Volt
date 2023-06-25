using System;

namespace VoltBot.Modules.Notifications;

public struct GuildNotification : IEquatable<GuildNotification>
{
    public ulong GuildId { get; }
    public ulong ChannelId { get; }
    public bool IsReady { get; }
    public bool IsShutdown { get; }

    public GuildNotification(ulong guildId, ulong channelId, bool isReady = false, bool isShutdown = false)
    {
        GuildId = guildId;
        ChannelId = channelId;
        IsReady = isReady;
        IsShutdown = isShutdown;
    }

    public bool Equals(GuildNotification other) =>
        GuildId == other.GuildId && ChannelId == other.ChannelId && IsReady == other.IsReady &&
        IsShutdown == other.IsShutdown;

    public override bool Equals(object obj) => obj is GuildNotification other && Equals(other);

    public override int GetHashCode() => GuildId.GetHashCode();
}