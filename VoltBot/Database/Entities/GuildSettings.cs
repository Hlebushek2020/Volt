using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

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
        #region
        public bool HistoryModuleIsEnabled { get; set; }
        public ulong? HistoryChannelId { get; set; }
        public byte? HistoryWordCount { get; set; }
        public ulong? HistoryAdminNotificationChannelId { get; set; } 
        //public ulong? HistoryStartMessageId { get; set; }
        #endregion
    }
}
