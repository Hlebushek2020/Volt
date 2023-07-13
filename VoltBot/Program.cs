using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Logging;
using VoltBot.Logs;
using VoltBot.Logs.Providers;
using LoggerFactory = VoltBot.Logs.LoggerFactory;

namespace VoltBot
{
    internal class Program
    {
        public static string Version { get; }
        public static string Directory { get; }

        static Program()
        {
            Assembly currentAssembly = Assembly.GetExecutingAssembly();
            Directory = Path.GetDirectoryName(currentAssembly.Location) ?? string.Empty;
            AssemblyInformationalVersionAttribute informationalVersionAttribute =
                currentAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (informationalVersionAttribute != null)
            {
                Version = informationalVersionAttribute.InformationalVersion;
            }
            else
            {
                Version version = currentAssembly.GetName().Version;
                Version = $"{version.Major}.{version.Minor}.{version.Build}";
            }
        }

        static int Main(string[] args)
        {
            if (!Settings.Settings.Availability())
            {
                return 0;
            }

            bool isShutdown = false;
            while (!isShutdown)
            {
                try
                {
                    using Bot volt = Bot.Current;
                    volt.RunAsync().GetAwaiter().GetResult();
                    isShutdown = true;
                }
                catch (Exception ex)
                {
                    ILogger defaultLogger = LoggerFactory.Current.CreateLogger<DefaultLoggerProvider>();
                    defaultLogger.LogCritical(new EventId(0, "App"), ex, string.Empty);
                }
            }

            return 0;
        }
    }
}