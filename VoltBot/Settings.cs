using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using VoltBot.Enums;

namespace VoltBot
{
    internal class Settings : ISettings
    {
        private const string FileName = "settings.json";

        #region Property
        public string BotToken { get; set; }
        public string BotPrefix { get; set; }
        public string BotDescription { get; set; }
        public LogLevel BotLogLevel { get; set; }
        public string VkSecret { get; set; }
        public bool BugReport { get; set; }
        public ulong BugReportChannel { get; set; }
        public ulong BugReportServer { get; set; }
        public string PingTheHost { get; set; }
        public IReadOnlyDictionary<HistoryRules, string> TextOfHistoryRules { get; set; }
        #endregion

        private Settings()
        {
            BotPrefix = "volt!";
            BotDescription = $"Список доступных команд. `{BotPrefix}help [команда]` для полной информации.";
            BotLogLevel = LogLevel.Information;
            BugReport = false;
            PingTheHost = "gateway.discord.gg";
            TextOfHistoryRules = new Dictionary<HistoryRules, string>
            {
                {
                    HistoryRules.AddTwoWords,
                    "К предыдущему сообщению можно добавить только одно или 2 слова в самый конец"
                },
                {
                    HistoryRules.TwoMessagesInRow,
                    "Один человек не может написать два сообщения подряд"
                }
            };
        }

        /// <summary>
        /// Loads settings from configuration file
        /// </summary>
        /// <returns>Read only settings</returns>
        public static ISettings Load()
        {
            string settingsFile = Path.Combine(Program.Directory, FileName);
            return JsonConvert.DeserializeObject<Settings>(File.ReadAllText(settingsFile, Encoding.UTF8));
        }

        /// <summary>
        /// Checks for the existence of a configuration file. If the configuration file does not exist, it will be created.
        /// </summary>
        /// <returns><see langword="true"/> if the configuration file exists</returns>
        public static bool Availability()
        {
            string settingsPath = Path.Combine(Program.Directory, FileName);

            if (File.Exists(settingsPath))
            {
                return true;
            }

            using StreamWriter streamWriter = new StreamWriter(settingsPath, false, Encoding.UTF8);
            streamWriter.Write(JsonConvert.SerializeObject(new Settings(), Formatting.Indented));

            return false;
        }
    }
}