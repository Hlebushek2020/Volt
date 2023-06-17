using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace VoltBot;

public class BotReadyNotificationsContainer
{
    #region Instance
    private static BotReadyNotificationsContainer _container;

    public static BotReadyNotificationsContainer Current => _container ??= new BotReadyNotificationsContainer();
    #endregion

    private readonly ConcurrentDictionary<ulong, ulong> _guildsForNotification =
        new ConcurrentDictionary<ulong, ulong>();

    public BotReadyNotificationsContainer()
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
            _guildsForNotification.TryAdd(guildId, channelId);
        }
    }

    public KeyValuePair<ulong, ulong>[] Get() => _guildsForNotification.ToArray();

    public bool AddGuild(ulong guild, ulong channel)
    {
        ulong newValue = _guildsForNotification.AddOrUpdate(guild, channel, (k, ov) => channel);
        Save();
        return newValue == channel;
    }

    public bool RemoveGuild(ulong guild)
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
        foreach (KeyValuePair<ulong, ulong> keyValuePair in _guildsForNotification)
        {
            binaryWriter.Write(keyValuePair.Key);
            binaryWriter.Write(keyValuePair.Value);
        }
    }
}