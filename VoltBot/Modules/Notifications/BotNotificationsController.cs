using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using VoltBot.Modules.Notifications;

namespace VoltBot;

public class BotNotificationsController
{
    #region Instance
    private static BotNotificationsController _controller;

    public static BotNotificationsController Current => _controller ??= new BotNotificationsController();
    #endregion

    private readonly ConcurrentDictionary<ulong, GuildNotification> _guildsForNotification =
        new ConcurrentDictionary<ulong, GuildNotification>();

    public BotNotificationsController()
    {
        string settingsPath =
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty,
                "guildsForNotification.bin");
        using FileStream fileStream = new FileStream(settingsPath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
        using BinaryReader binaryReader = new BinaryReader(fileStream);
        while (fileStream.Position < fileStream.Length)
        {
            ulong guildId = binaryReader.ReadUInt64();
            ulong channelId = binaryReader.ReadUInt64();
            bool isReady = binaryReader.ReadBoolean();
            bool isShutdown = binaryReader.ReadBoolean();
            _guildsForNotification.TryAdd(guildId, new GuildNotification(guildId, channelId, isReady, isShutdown));
        }
    }

    public List<GuildNotification> GetAll() => _guildsForNotification.Values.ToList();

    public bool Get(ulong guildId, out GuildNotification guildNotification) =>
        _guildsForNotification.TryGetValue(guildId, out guildNotification);

    public void AddOrUpdate(GuildNotification guildNotification)
    {
        GuildNotification newValue = _guildsForNotification.AddOrUpdate(guildNotification.GuildId, guildNotification,
            (k, ov) => guildNotification);
        Save();
    }

    public bool Remove(GuildNotification guildNotification) => Remove(guildNotification.GuildId);

    public bool Remove(ulong guild)
    {
        bool result = _guildsForNotification.TryRemove(guild, out _);
        Save();
        return result;
    }

    private void Save()
    {
        string settingsPath =
            Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? string.Empty,
                "guildsForNotification.bin");
        using FileStream fileStream = new FileStream(settingsPath, FileMode.Create, FileAccess.Write);
        using BinaryWriter binaryWriter = new BinaryWriter(fileStream);
        foreach (GuildNotification guildNotification in _guildsForNotification.Values)
        {
            binaryWriter.Write(guildNotification.GuildId);
            binaryWriter.Write(guildNotification.ChannelId);
            binaryWriter.Write(guildNotification.IsReady);
            binaryWriter.Write(guildNotification.IsShutdown);
        }
    }
}