using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameDealWatcher.Application.Services;
using GameDealWatcher.App.Views;
using Microsoft.Extensions.DependencyInjection;

namespace GameDealWatcher.App.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IGameDealService _gameDealService;
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private object currentView;

    [ObservableProperty]
    private string lastUpdatedText = "Not refreshed";

    public MainViewModel(
        IGameDealService gameDealService,
        IServiceProvider serviceProvider)
    {
        _gameDealService = gameDealService;
        _serviceProvider = serviceProvider;
        currentView = _serviceProvider.GetRequiredService<DashboardView>();
    }

    [RelayCommand]
    private void ShowDashboard() => CurrentView = _serviceProvider.GetRequiredService<DashboardView>();

    [RelayCommand]
    private void ShowEpic() => CurrentView = _serviceProvider.GetRequiredService<EpicView>();

    [RelayCommand]
    private void ShowSteam() => CurrentView = _serviceProvider.GetRequiredService<SteamView>();

    [RelayCommand]
    private void ShowSettings() => CurrentView = _serviceProvider.GetRequiredService<SettingsView>();

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