namespace GameDealWatcher.Domain.Entities;

public sealed record AppSettings
{
    public TimeSpan RefreshInterval { get; set; } = TimeSpan.FromHours(24);
    public TimeOnly RefreshTime { get; set; } = new(8, 0);
    public bool StartWithWindows { get; set; } = false;
    public bool EpicNotifications { get; set; } = true;
    public bool SteamNotifications { get; set; } = true;
    public int MinimumSteamDiscount { get; set; } = 0;
}