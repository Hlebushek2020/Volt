using System.IO;
using System.Reflection;
using System.Text;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace VoltBot.Settings
{
    internal class Settings : IReadOnlySettings
    {
        private const string FileName = "settings.json";

        #region Property
        public string BotToken { get; set; }
        public string BotPrefix { get; set; } = "volt>";
        public string BotDescription { get; set; } = string.Empty;
        public string VkSecret { get; set; }
        public bool BugReport { get; set; } = false;
        public ulong BugReportChannel { get; set; }
        public ulong BugReportServer { get; set; }
        public LogLevel DiscordApiLogLevel { get; set; } = LogLevel.Information;
        public LogLevel BotLogLevel { get; set; } = LogLevel.Information;
        #endregion

        #region Instance
        private static Settings _settings;

        [JsonIgnore]
        public static IReadOnlySettings Current
        {
            get
            {
                if (_settings == null)
                {
                    string settingsFile = Path.Combine(Program.Directory, FileName);
                    _settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(settingsFile, Encoding.UTF8));
                }
                return _settings;
            }
        }
        #endregion

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