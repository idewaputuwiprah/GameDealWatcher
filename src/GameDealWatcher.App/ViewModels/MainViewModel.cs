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
        await _gameDealService.RefreshAllDealsAsync(CancellationToken.None);
        LastUpdatedText = $"Last updated: {DateTime.Now:t}";
    }
}