using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameDealWatcher.Application.Services;
using GameDealWatcher.Domain.Entities;
using System.Collections.ObjectModel;

namespace GameDealWatcher.App.ViewModels;

public partial class SteamViewModel : ObservableObject
{
    private readonly IGameDealService _gameDealService;

    [ObservableProperty]
    private ObservableCollection<GameDeal> steamDeals = new();

    [ObservableProperty]
    private int minimumDiscount = 0;

    public SteamViewModel(IGameDealService gameDealService)
    {
        _gameDealService = gameDealService;
        _ = LoadDealsAsync();
    }

    [RelayCommand]
    public async Task LoadDealsAsync()
    {
        var fetchedDeals = await _gameDealService.GetAllDealsAsync(CancellationToken.None);
        SteamDeals.Clear();
        foreach (var d in fetchedDeals.Where(d => d.ProviderName == "Steam" && d.DiscountPercentage >= MinimumDiscount))
        {
            SteamDeals.Add(d);
        }
    }
}