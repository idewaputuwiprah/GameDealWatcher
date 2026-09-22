using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameDealWatcher.Domain.Entities;
using GameDealWatcher.Domain.Interfaces;
using GameDealWatcher.Infrastructure.Images;

namespace GameDealWatcher.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly IImageCacheService _imageCacheService;
    private readonly ISettingsRepository _settingsRepository;

    [ObservableProperty]
    private TimeSpan refreshInterval = TimeSpan.FromHours(24);

    [ObservableProperty]
    private TimeOnly refreshTime = new(8, 0);

    [ObservableProperty]
    private bool startWithWindows = false;

    [ObservableProperty]
    private bool epicNotifications = true;

    [ObservableProperty]
    private bool steamNotifications = true;

    [ObservableProperty]
    private int minimumSteamDiscount = 0;

    public SettingsViewModel(IImageCacheService imageCacheService, ISettingsRepository settingsRepository)
    {
        _imageCacheService = imageCacheService;
        _settingsRepository = settingsRepository;
    }

    public async Task InitializeAsync()
    {
        await LoadSettingsAsync();
    }

    private async Task LoadSettingsAsync()
    {
        try
        {
            var settings = await _settingsRepository.GetSettingsAsync(CancellationToken.None);
            RefreshInterval = settings.RefreshInterval;
            RefreshTime = settings.RefreshTime;
            StartWithWindows = settings.StartWithWindows;
            EpicNotifications = settings.EpicNotifications;
            SteamNotifications = settings.SteamNotifications;
            MinimumSteamDiscount = settings.MinimumSteamDiscount;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load settings: {ex}");
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        await _settingsRepository.SaveSettingsAsync(new AppSettings
        {
            RefreshInterval = RefreshInterval,
            RefreshTime = RefreshTime,
            StartWithWindows = StartWithWindows,
            EpicNotifications = EpicNotifications,
            SteamNotifications = SteamNotifications,
            MinimumSteamDiscount = MinimumSteamDiscount
        }, CancellationToken.None);
    }

    [RelayCommand]
    private async Task ClearCacheAsync()
    {
        await _imageCacheService.ClearCacheAsync();
    }
}