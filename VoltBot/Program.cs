using System;
using System.IO;
using System.Reflection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Sinks.SystemConsole.Themes;

namespace VoltBot
{
    internal class Program
    {
        private const string LogOutputTemplate =
            "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] [{SourceContext}] {Message}{NewLine}{Exception}";

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
            if (!Settings.Availability())
            {
                return 0;
            }

            ISettings settings = Settings.Load();

            LoggerConfiguration loggerConfiguration = new LoggerConfiguration()
                .WriteTo.Console(
                    outputTemplate: LogOutputTemplate,
                    theme: SystemConsoleTheme.Colored)
                .WriteTo.File(
                    path: Path.Combine(Directory, "logs", ".log"),
                    outputTemplate: LogOutputTemplate,
                    rollingInterval: RollingInterval.Day)
                .Enrich.FromLogContext();

            loggerConfiguration = settings.BotLogLevel switch
            {
                LogLevel.Critical => loggerConfiguration.MinimumLevel.Fatal(),
                LogLevel.Error => loggerConfiguration.MinimumLevel.Error(),
                LogLevel.Warning => loggerConfiguration.MinimumLevel.Warning(),
                LogLevel.Information => loggerConfiguration.MinimumLevel.Information(),
                _ => loggerConfiguration.MinimumLevel.Debug()
            };

            Log.Logger = loggerConfiguration.CreateLogger();

            //bool isShutdown = false;
            //while (!isShutdown)
            //{
            try
            {
                using Bot volt = new Bot(settings);
                volt.RunAsync().GetAwaiter().GetResult();
                //isShutdown = true;
            }
            catch (Exception ex)
            {
                Log.Logger.ForContext<Program>().Fatal(ex, "Bot failed.");
                return 1;
            }
            //}

            return 0;
        }
    }
}