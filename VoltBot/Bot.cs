using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Exceptions;
using DSharpPlus.Entities;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using VoltBot.Commands;
using VoltBot.Services;
using VoltBot.Services.Implementation;

namespace VoltBot
{
    public sealed class Bot : IBot, IDisposable
    {
        public DateTime StartDateTime { get; private set; }

        private volatile bool _isRunning = false;
        private bool _isDisposed = false;
        private readonly DiscordClient _discordClient;
        private readonly ILogger<Bot> _logger;
        private readonly IBotNotificationsService _botNotificationsService;
        private readonly ISettings _settings;
        private readonly IServiceProvider _services;

        public Bot(ISettings settings)
        {
            _settings = settings;

            ILoggerFactory loggerFactory = new LoggerFactory().AddSerilog(dispose: true);
            _logger = loggerFactory.CreateLogger<Bot>();

            _logger.LogInformation("Initializing discord client");

            _discordClient = new DiscordClient(
                new DiscordConfiguration
                {
                    Token = settings.BotToken,
                    TokenType = TokenType.Bot,
                    Intents = DiscordIntents.All,
                    LoggerFactory = new LoggerFactory().AddSerilog(dispose: true)
                });

            _discordClient.Ready += DiscordClient_Ready;

            _services = new ServiceCollection()
                .AddLogging(lb => lb.AddSerilog(dispose: true))
                .AddSingleton(_discordClient)
                .AddSingleton(settings)
                .AddSingleton(typeof(IBot), this)
                .AddSingleton<IForwardingMessageByUrlService, ForwardingMessageByUrlService>()
                .AddSingleton<IForwardingPostFromVkByUrlService, ForwardingPostFromVkByUrlService>()
                .AddSingleton<IBotPingService, BotPingService>()
                .AddSingleton<ICheckingHistoryService, CheckingHistoryService>()
                .AddSingleton<IDeletingMessagesByEmojiService, DeletingMessagesByEmojiService>()
                .AddSingleton<IBotNotificationsService, BotNotificationsService>()
                .BuildServiceProvider();

            _services.GetService<IForwardingMessageByUrlService>();
            _services.GetService<IForwardingPostFromVkByUrlService>();
            _services.GetService<IBotPingService>();
            _services.GetService<ICheckingHistoryService>();
            _services.GetService<IDeletingMessagesByEmojiService>();

            _botNotificationsService = _services.GetService<IBotNotificationsService>();

            CommandsNextExtension commands = _discordClient.UseCommandsNext(
                new CommandsNextConfiguration
                {
                    StringPrefixes = new List<string> { settings.BotPrefix },
                    EnableDefaultHelp = false,
                    Services = _services
                });

            commands.CommandErrored += Commands_CommandErrored;
            commands.CommandExecuted += Commands_CommandExecuted;

            commands.RegisterCommands<HelpCommandModule>();
            commands.RegisterCommands<AdministratorCommandModule>();
            commands.RegisterCommands<OwnerCommandModule>();
        }

        ~Bot() { Dispose(false); }

        private async Task DiscordClient_Ready(DiscordClient sender, ReadyEventArgs e) =>
            await sender.UpdateStatusAsync(
                new DiscordActivity($"на тебя | {_settings.BotPrefix}help", ActivityType.Watching));

        /*private Task DiscordClient_SocketErrored(DiscordClient sender, SocketErrorEventArgs e)
        {
            _defaultLogger.LogCritical(new EventId(0, "Discord Client: Socket Errored"), e.Exception, string.Empty);
            Shutdown();
            return Task.CompletedTask;
        }*/

        private Task Commands_CommandExecuted(CommandsNextExtension sender, CommandExecutionEventArgs e)
        {
            _logger.LogInformation($"{e.Command.Name} command completed successfully");
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
                _logger.LogWarning(
                    $"Error when executing the {e.Command.Name} command. Type: ArgumentException. Message: {
                        exception.Message}");
            }
            else if (exception is CommandNotFoundException commandNotFoundEx)
            {
                embed.WithDescription($"Неизвестная команда `{commandNotFoundEx.CommandName}`");
                _logger.LogWarning(
                    $"Error when executing the {commandNotFoundEx.CommandName
                    } command. Type: CommandNotFoundException. Message: {exception.Message}");
            }
            else if (exception is ChecksFailedException checksFailedEx)
            {
                embed.WithDescription($"У вас нет доступа к команде `{checksFailedEx.Command.Name}`");
                _logger.LogWarning(
                    $"Error when executing the {checksFailedEx.Command.Name
                    } command. Type: CommandNotFoundException. Message: {exception.Message}");
            }
            else
            {
                embed.WithDescription(
                    "При выполнении команды произошла неизвестная ошибка, обратитесь к администраторам сервера (гильдии)");
                _logger.LogError($"Error when executing the: {e.Command?.Name ?? "Unknown"}", exception);
            }

            await context.RespondAsync(embed);
        }

        public async Task RunAsync()
        {
            _logger.LogInformation("Discord client connect");

            await _discordClient.ConnectAsync();
            StartDateTime = DateTime.Now;
            _isRunning = true;

            await _botNotificationsService.SendReadyNotifications();

            while (_isRunning)
            {
                await Task.Delay(200);
            }
        }

        public void Shutdown(string reason = null)
        {
            _logger.LogInformation("Shutdown");

            if (_discordClient != null)
            {
                if (!string.IsNullOrEmpty(reason))
                {
                    _botNotificationsService.SendShutdownNotifications(reason).Wait();
                }

                _logger.LogInformation("Disconnect discord client");
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

            _isDisposed = true;
        }
    }
}