namespace GameDealWatcher.Domain.Entities;

public sealed record AppSettings
{
    private int _minimumSteamDiscount = 0;
    private TimeSpan _refreshInterval = TimeSpan.FromHours(24);

    public TimeSpan RefreshInterval
    {
        get => _refreshInterval;
        init => _refreshInterval = value < TimeSpan.FromMinutes(5)
            ? TimeSpan.FromMinutes(5)
            : value;
    }
    public TimeOnly RefreshTime { get; init; } = new(8, 0);
    public bool StartWithWindows { get; init; } = false;
    public bool EpicNotifications { get; init; } = true;
    public bool SteamNotifications { get; init; } = true;
    public int MinimumSteamDiscount
    {
        get => _minimumSteamDiscount;
        init => _minimumSteamDiscount = Math.Clamp(value, 0, 100);
    }
}