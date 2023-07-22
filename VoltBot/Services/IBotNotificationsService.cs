using System.Threading.Tasks;

namespace VoltBot.Services;

public interface IBotNotificationsService
{
    Task SendReadyNotifications();
    Task SendShutdownNotifications(string reason);
}