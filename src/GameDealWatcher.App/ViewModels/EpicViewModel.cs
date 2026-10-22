using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameDealWatcher.Application.Services;
using GameDealWatcher.Domain.Entities;
using System.Collections.ObjectModel;

namespace GameDealWatcher.App.ViewModels;

public partial class EpicViewModel : ObservableObject
{
    private readonly IGameDealService _gameDealService;
    private CancellationTokenSource? _loadCts;

    [ObservableProperty]
    private ObservableCollection<GameDeal> epicDeals = new();

    [ObservableProperty]
    private string filterText = "All";

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isEmpty;

    public ObservableCollection<string> FilterOptions { get; } = new() { "All", "Free Now", "Upcoming" };

    public EpicViewModel(IGameDealService gameDealService)
    {
        _gameDealService = gameDealService;
    }

    public async Task InitializeAsync()
    {
        await LoadDealsAsync();
    }

    partial void OnFilterTextChanged(string value)
    {
        _ = LoadDealsAsync();
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
            var epicDeals = fetchedDeals.Where(d => d.ProviderName == ProviderNames.Epic);

            var now = DateTimeOffset.UtcNow;
            var filtered = FilterText switch
            {
                "Free Now" => epicDeals.Where(d => d.IsFreeAt(now)),
                "Upcoming" => epicDeals.Where(d => d.IsUpcomingAt(now)),
                _ => epicDeals
            };

            EpicDeals.Clear();
            foreach (var d in filtered)
            {
                EpicDeals.Add(d);
            }
            IsEmpty = EpicDeals.Count == 0;
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadDealsAsync failed: {ex}");
            EpicDeals.Clear();
            IsEmpty = true;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
