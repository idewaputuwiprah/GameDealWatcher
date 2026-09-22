namespace GameDealWatcher.Domain.Interfaces;

public interface INotificationService
{
    void ShowDealNotification(string title, string message);
}
