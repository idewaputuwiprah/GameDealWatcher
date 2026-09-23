using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameDealWatcher.Application.Services;
using GameDealWatcher.Domain.Entities;
using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;

namespace GameDealWatcher.App.ViewModels;

public partial class SteamViewModel : ObservableObject
{
    private readonly IGameDealService _gameDealService;
    private CancellationTokenSource? _loadCts;
    private readonly DispatcherTimer _debounceTimer;

    [ObservableProperty]
    private ObservableCollection<GameDeal> steamDeals = new();

    [ObservableProperty]
    private int minimumDiscount = 0;

    [ObservableProperty]
    private bool isLoading;

    public SteamViewModel(IGameDealService gameDealService)
    {
        _gameDealService = gameDealService;
        _debounceTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _debounceTimer.Tick += (s, e) => { _debounceTimer.Stop(); _ = LoadDealsAsync(); };
    }

    public async Task InitializeAsync()
    {
        await LoadDealsAsync();
    }

    partial void OnMinimumDiscountChanged(int value)
    {
        _debounceTimer.Stop();
        _debounceTimer.Start();
    }

    [RelayCommand]
    public async Task LoadDealsAsync()
    {
        _loadCts?.Cancel();
        _loadCts = new CancellationTokenSource();

        try
        {
            IsLoading = true;
            var fetchedDeals = await _gameDealService.GetAllDealsAsync(_loadCts.Token);
            SteamDeals.Clear();
            foreach (var d in fetchedDeals.Where(d => d.ProviderName == ProviderNames.Steam && d.DiscountPercentage >= MinimumDiscount))
            {
                SteamDeals.Add(d);
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadDealsAsync failed: {ex}");
        }
        finally
        {
            IsLoading = false;
        }
    }
}
