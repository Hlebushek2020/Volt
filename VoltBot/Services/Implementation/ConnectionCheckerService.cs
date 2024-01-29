using System;
using System.Net.NetworkInformation;
using Microsoft.Extensions.Logging;

namespace VoltBot.Services.Implementation;

public class ConnectionCheckerService : IConnectionCheckerService
{
    private readonly ISettings _settings;
    private readonly ILogger<ConnectionCheckerService> _logger;

    public ConnectionCheckerService(ISettings settings, ILogger<ConnectionCheckerService> logger)
    {
        _settings = settings;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(settings.PingTheHost))
            throw new ArgumentException($"Parameter {nameof(settings.PingTheHost)} is not set in settings",
                nameof(settings.PingTheHost));

        _logger.LogInformation($"{nameof(ConnectionCheckerService)} loaded.");
    }

    public bool Check()
    {
        try
        {
            _logger.LogInformation("Checking the connection");
            Ping ping = new Ping();
            PingReply pingReply = ping.Send(_settings.PingTheHost);
            return pingReply.Status == IPStatus.Success;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            return false;
        }
    }
}