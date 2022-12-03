using DSharpPlus;
using DSharpPlus.CommandsNext;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VoltBot.Commands;
using VoltBot.Commands.Formatter;
using VoltBot.Logs;
using VoltBot.Logs.Providers;

namespace VoltBot
{
    public class Bot : IDisposable
    {
        public DateTime StartDateTime { get; private set; }

        #region Instance
        private static Bot _bot;

        public static Bot Current
        {
            get
            {
                if (_bot == null)
                {
                    _bot = new Bot();
                }
                return _bot;
            }
        }
        #endregion

        private volatile bool _isRunning = false;
        private bool _isDisposed = false;
        private DiscordClient _discordClient;

        public Bot()
        {
            LoggerFactory loggerFactory = new LoggerFactory();
            loggerFactory.AddProvider(new DiscordClientLoggerProvider(LogLevel.Debug));

            _discordClient = new DiscordClient(new DiscordConfiguration
            {
                Token = Settings.Settings.Current.BotToken,
                TokenType = TokenType.Bot,
                Intents = DiscordIntents.All,
                LoggerFactory = loggerFactory
            });

            CommandsNextExtension commands = _discordClient.UseCommandsNext(new CommandsNextConfiguration
            {
                StringPrefixes = new List<string> { Settings.Settings.Current.BotPrefix }
            });

            commands.SetHelpFormatter<CustomHelpFormatter>();

            commands.RegisterCommands<OwnerCommandModule>();
            commands.RegisterCommands<UserCommandModule>();
        }

        public async Task RunAsync()
        {
            await _discordClient.ConnectAsync();
            StartDateTime = DateTime.Now;
            _isRunning = true;
            while (_isRunning)
            {
                await Task.Delay(200);
            }
        }

        public void Shutdown()
        {
            _isRunning = false;

            if (_discordClient != null)
            {
                _discordClient.DisconnectAsync().Wait();
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                Shutdown();
            }

            _isDisposed = true;
        }
    }
}