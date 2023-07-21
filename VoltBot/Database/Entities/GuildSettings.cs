using System.ComponentModel.DataAnnotations;

namespace VoltBot.Database.Entities
{
    /// <summary>
    /// Entity describing the bot settings for the server (guild)
    /// </summary>
    internal class GuildSettings
    {
        /// <summary>
        /// Gets or sets the server's (guild's) ID. Required field. Is the primary key.
        /// </summary>
        [Key]
        public ulong GuildId { get; set; }

        #region Notification
        /// <summary>
        /// Gets or sets the channel ID for system messages (such as disabling and enabling a bot).
        /// </summary>
        public ulong? NotificationChannelId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether bot shutdown notifications should be received or not.
        /// </summary>
        public bool IsShutdownNotification { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether notifications about the inclusion of the bot should be received or not.
        /// </summary>
        public bool IsReadyNotification { get; set; }
        #endregion

        #region History
        /// <summary>
        /// Gets or sets a value indicating whether the history management module should be enabled for this server (guild) or not.
        /// </summary>
        public bool HistoryModuleIsEnabled { get; set; }

        /// <summary>
        /// Gets or sets the id of the channel with histories.
        /// </summary>
        public ulong? HistoryChannelId { get; set; }

        /// <summary>
        /// Gets or sets the maximum number of words that can be added to the previous message. Refers to the histories management module.
        /// </summary>
        public byte? HistoryWordCount { get; set; }

        /// <summary>
        /// Gets or sets the Id of the channel where rule violation notifications will be sent. Refers to the histories management module.
        /// </summary>
        public ulong? HistoryAdminNotificationChannelId { get; set; }

        /// <summary>
        /// Gets or sets the Id of the role that is pinged in the rule violation message. Refers to the histories management module.
        /// </summary>
        public ulong? HistoryAdminPingRole { get; set; }
        #endregion
    }
}