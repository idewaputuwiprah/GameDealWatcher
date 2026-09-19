using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameDealWatcher.Application.Services;
using GameDealWatcher.Domain.Entities;
using System.Collections.ObjectModel;

namespace GameDealWatcher.App.ViewModels;

public partial class EpicViewModel : ObservableObject
{
    private readonly IGameDealService _gameDealService;

    [ObservableProperty]
    private ObservableCollection<GameDeal> epicDeals = new();

    [ObservableProperty]
    private string filterText = "All";

    public ObservableCollection<string> FilterOptions { get; } = new() { "All", "Free Now", "Upcoming" };

    public EpicViewModel(IGameDealService gameDealService)
    {
        _gameDealService = gameDealService;
        _ = LoadDealsAsync();
    }

    [RelayCommand]
    public async Task LoadDealsAsync()
    {
        var fetchedDeals = await _gameDealService.GetAllDealsAsync(CancellationToken.None);
        EpicDeals.Clear();
        foreach (var d in fetchedDeals.Where(d => d.ProviderName == "Epic"))
        {
            EpicDeals.Add(d);
        }
    }
}