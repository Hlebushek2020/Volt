using Microsoft.Extensions.Logging;
using System;
using VoltBot.Logs;
using VoltBot.Logs.Providers;

namespace VoltBot
{
    internal class Program
    {
        static int Main(string[] args)
        {
            if (!Settings.Settings.Availability())
            {
                return 0;
            }

            try
            {
                using (Bot volt = Bot.Current)
                {
                    volt.RunAsync().GetAwaiter().GetResult();
                }
            }
            catch (Exception ex)
            {
                ILogger defaultLogger = LoggerFactory.Current.CreateLogger<DefaultLoggerProvider>();
                defaultLogger.LogCritical(new EventId(0, "App"), ex, "");
                return 1;
            }

            return 0;
        }
    }
}