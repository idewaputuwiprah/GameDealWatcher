using GameDealWatcher.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Toolkit.Uwp.Notifications;

namespace GameDealWatcher.Infrastructure.Notifications;

public sealed class WindowsNotificationService : INotificationService
{
    private readonly ILogger<WindowsNotificationService> _logger;

    public WindowsNotificationService(ILogger<WindowsNotificationService> logger)
    {
        _logger = logger;
    }

    public void ShowDealNotification(string title, string message)
    {
        try
        {
            new ToastContentBuilder()
                .AddText(title)
                .AddText(message)
                .Show();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Notification failed");
        }
    }
}