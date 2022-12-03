using Newtonsoft.Json;
using System.IO;
using System.Reflection;
using System.Text;

namespace VoltBot.Settings
{
    internal class Settings : IReadOnlySettings
    {
        public string BotToken { get; set; }
        public string BotPrefix { get; set; } = ">volt";
        public string BotDescription { get; set; } = string.Empty;

        #region Instance
        private static Settings settings;

        [JsonIgnore]
        public static IReadOnlySettings Current
        {
            get
            {
                if (settings == null)
                {
                    string settingsFile = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "settings.json");
                    settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText(settingsFile, Encoding.UTF8));
                }
                return settings;
            }
        }
        #endregion

        public static bool Availability()
        {
            string settingsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "settings.json");

            if (File.Exists(settingsPath))
            {
                return true;
            }

            using (StreamWriter streamWriter = new StreamWriter(settingsPath, false, Encoding.UTF8))
            {
                streamWriter.Write(JsonConvert.SerializeObject(new Settings(), Formatting.Indented));
            }

            return false;
        }
    }
}