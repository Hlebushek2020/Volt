using System.ComponentModel.DataAnnotations;

namespace VoltBot.Database.Entities
{
    internal class GuildSettings
    {
        [Key]
        public ulong GuildId { get; set; }

        #region Notification
        public ulong? NotificationChannelId { get; set; }
        public bool IsShutdownNotification { get; set; }
        public bool IsReadyNotification { get; set; }
        #endregion

        #region History
        public bool HistoryModuleIsEnabled { get; set; }
        public ulong? HistoryChannelId { get; set; }
        public byte? HistoryWordCount { get; set; }
        public ulong? HistoryAdminNotificationChannelId { get; set; }
        public ulong? HistoryAdminPingRole { get; set; }
        #endregion
    }
}