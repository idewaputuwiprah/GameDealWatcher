using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameDealWatcher.Application.Services;
using GameDealWatcher.App.Views;

namespace GameDealWatcher.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IGameDealService _gameDealService;

    [ObservableProperty]
    private object currentView;

    [ObservableProperty]
    private string lastUpdatedText = "Not refreshed";

    private readonly DashboardView _dashboardView;
    private readonly EpicView _epicView;
    private readonly SteamView _steamView;
    private readonly SettingsView _settingsView;

    public MainViewModel(
        IGameDealService gameDealService,
        DashboardView dashboardView,
        EpicView epicView,
        SteamView steamView,
        SettingsView settingsView)
    {
        _gameDealService = gameDealService;
        _dashboardView = dashboardView;
        _epicView = epicView;
        _steamView = steamView;
        _settingsView = settingsView;
        currentView = _dashboardView;
    }

    [RelayCommand]
    private void ShowDashboard() => CurrentView = _dashboardView;

    [RelayCommand]
    private void ShowEpic() => CurrentView = _epicView;

    [RelayCommand]
    private void ShowSteam() => CurrentView = _steamView;

    [RelayCommand]
    private void ShowSettings() => CurrentView = _settingsView;

    [RelayCommand]
    private async Task ExecuteRefreshAsync()
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
            await _gameDealService.RefreshAllDealsAsync(cts.Token);
            LastUpdatedText = $"Last updated: {DateTime.Now:t}";
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Refresh failed: {ex}");
            LastUpdatedText = "Refresh failed";
        }
    }
}