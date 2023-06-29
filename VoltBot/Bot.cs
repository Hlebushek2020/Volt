using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;
using VoltBot.Commands;
using VoltBot.Commands.Formatter;
using VoltBot.Logs;
using VoltBot.Logs.Providers;
using VoltBot.Modules;
using VoltBot.Modules.Notifications;
using VoltBot.Settings;

namespace VoltBot
{
    public sealed class Bot : IDisposable
    {
        public DateTime StartDateTime { get; private set; }

        #region Instance
        private static Bot _currentInstance;

        public static Bot Current => _currentInstance ??= new Bot();
        #endregion

        private volatile bool _isRunning = false;
        private bool _isDisposed = false;
        private readonly DiscordClient _discordClient;
        private readonly ILogger _defaultLogger;
        private readonly BotNotificationsModule _botNotificationsModule;

        public Bot()
        {
            IReadOnlySettings settings = Settings.Settings.Current;

            LoggerFactory loggerFactory = LoggerFactory.Current;
            loggerFactory.AddProvider(new DiscordClientLoggerProvider(settings.DiscordApiLogLevel));
            _defaultLogger = loggerFactory.CreateLogger<DefaultLoggerProvider>();

            _defaultLogger.LogInformation(new EventId(0, "Init"), "Initializing discord client");

            _discordClient = new DiscordClient(
                new DiscordConfiguration
                {
                    Token = settings.BotToken,
                    TokenType = TokenType.Bot,
                    Intents = DiscordIntents.All,
                    LoggerFactory = loggerFactory
                });

            _discordClient.Ready += DiscordClient_Ready;
            //_discordClient.SocketErrored += DiscordClient_SocketErrored;

            _discordClient.MessageCreated += new ForwardingMessageByUrlModule().Handler;
            _discordClient.MessageCreated += new ForwardingPostFromVkByUrlModule().Handler;
            _discordClient.MessageCreated += new BotPingModule().Handler;
            _discordClient.MessageReactionAdded += new DeletingMessagesByEmojiModule().Handler;

            _botNotificationsModule = new BotNotificationsModule(_discordClient);

            CommandsNextExtension commands = _discordClient.UseCommandsNext(
                new CommandsNextConfiguration
                {
                    StringPrefixes = new List<string> { Settings.Settings.Current.BotPrefix },
                    EnableDefaultHelp = false
                });

            commands.CommandErrored += Commands_CommandErrored;
            commands.CommandExecuted += Commands_CommandExecuted;

            //commands.SetHelpFormatter<CustomHelpFormatter>();

            commands.RegisterCommands<HelpCommandModule>();
            commands.RegisterCommands<AdministratorCommandModule>();
            commands.RegisterCommands<OwnerCommandModule>();
        }

        ~Bot() { Dispose(false); }

        private static async Task DiscordClient_Ready(DiscordClient sender, ReadyEventArgs e) =>
            await sender.UpdateStatusAsync(
                new DiscordActivity($"на тебя | {Settings.Settings.Current.BotPrefix}help", ActivityType.Watching));

        /*private Task DiscordClient_SocketErrored(DiscordClient sender, SocketErrorEventArgs e)
        {
            _defaultLogger.LogCritical(new EventId(0, "Discord Client: Socket Errored"), e.Exception, string.Empty);
            Shutdown();
            return Task.CompletedTask;
        }*/

        private Task Commands_CommandExecuted(CommandsNextExtension sender, CommandExecutionEventArgs e)
        {
            _defaultLogger.LogInformation(
                new EventId(0, $"Command: {e.Command.Name}"),
                "Command completed successfully");
            return Task.CompletedTask;
        }

        private async Task Commands_CommandErrored(CommandsNextExtension sender, CommandErrorEventArgs e)
        {
            CommandContext context = e.Context;
            Exception exception = e.Exception;

            DiscordEmbedBuilder embed = new DiscordEmbedBuilder()
            {
                Title = context.Member?.DisplayName,
                Color = DiscordColor.Red
            };

            if (exception is ArgumentException)
            {
                embed.WithDescription(
                    $"В команде `{e.Command.Name}` ошибка один или несколько параметров введены неверно");
                _defaultLogger.LogWarning(new EventId(0, $"Command: {e.Command.Name}"), exception, string.Empty);
            }
            else if (exception is CommandNotFoundException commandNotFoundEx)
            {
                embed.WithDescription($"Неизвестная команда `{commandNotFoundEx.CommandName}`");
                _defaultLogger.LogWarning(
                    new EventId(0, $"Command: {commandNotFoundEx.CommandName}"),
                    exception,
                    string.Empty);
            }
            else if (exception is ChecksFailedException checksFailedEx)
            {
                embed.WithDescription($"У вас нет доступа к команде `{checksFailedEx.Command.Name}`");
                _defaultLogger.LogWarning(
                    new EventId(0, $"Command: {checksFailedEx.Command.Name}"),
                    exception,
                    string.Empty);
            }
            else
            {
                embed.WithDescription(
                    "При выполнении команды произошла неизвестная ошибка, обратитесь к администраторам сервера (гильдии)");
                _defaultLogger.LogError(
                    new EventId(0, $"Command: {e.Command?.Name ?? "Unknown"}"),
                    exception,
                    string.Empty);
            }

            await context.RespondAsync(embed);
        }

        public async Task RunAsync()
        {
            _defaultLogger.LogInformation(new EventId(0, "Run"), "Discord client connect");

            await _discordClient.ConnectAsync();
            StartDateTime = DateTime.Now;
            _isRunning = true;

            await _botNotificationsModule.SendReadyNotifications();

            while (_isRunning)
            {
                await Task.Delay(200);
            }
        }

        public void Shutdown(string reason = null)
        {
            EventId eventId = new EventId(0, "Shutdown");
            _defaultLogger.LogInformation(eventId, "Shutdown");

            if (_discordClient != null)
            {
                if (!string.IsNullOrEmpty(reason))
                {
                    _botNotificationsModule.SendShutdownNotifications(reason).Wait();
                }

                _defaultLogger.LogInformation(eventId, "Disconnect discord client");
                _discordClient.DisconnectAsync().Wait();
            }

            _isRunning = false;
        }

        /*private async Task Restart()
        {
            EventId eventId = new EventId(0, "Restart");
            _defaultLogger.LogInformation(eventId, "Restart");

            _defaultLogger.LogInformation(eventId, "Disconnect discord client");
            await _discordClient.DisconnectAsync();

            _defaultLogger.LogInformation(eventId, "Reconnect after 2 seconds");
            await Task.Delay(2000);

            _defaultLogger.LogInformation(eventId, "Discord client connect");
            await _discordClient.ConnectAsync();
        }*/

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                if (_isRunning)
                    Shutdown();

                _discordClient?.Dispose();
            }

            _currentInstance = null;

            _isDisposed = true;
        }
    }
}