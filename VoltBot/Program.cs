﻿using System;
using System.Reflection;
using Microsoft.Extensions.Logging;
using VoltBot.Logs;
using VoltBot.Logs.Providers;

namespace VoltBot
{
    internal class Program
    {
        public static string Version { get; }

        static Program()
        {
            Assembly currentAssembly = Assembly.GetExecutingAssembly();
            AssemblyInformationalVersionAttribute informationalVersionAttribute =
                currentAssembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>();
            if (informationalVersionAttribute != null)
            {
                Version = informationalVersionAttribute.InformationalVersion;
            }
            else
            {
                Version version = Assembly.GetExecutingAssembly().GetName().Version;
                Version = $"{version.Major}.{version.Minor}.{version.Build}";
            }
        }

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