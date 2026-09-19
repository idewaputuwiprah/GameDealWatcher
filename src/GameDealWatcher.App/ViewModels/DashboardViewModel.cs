using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using GameDealWatcher.Application.Services;
using GameDealWatcher.Domain.Entities;
using System.Collections.ObjectModel;

namespace GameDealWatcher.App.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly IGameDealService _gameDealService;

    [ObservableProperty]
    private ObservableCollection<GameDeal> deals = new();

    public DashboardViewModel(IGameDealService gameDealService)
    {
        _gameDealService = gameDealService;
        _ = LoadDealsAsync();
    }

    [RelayCommand]
    public async Task LoadDealsAsync()
    {
        var fetchedDeals = await _gameDealService.GetAllDealsAsync(CancellationToken.None);
        Deals.Clear();
        foreach (var deal in fetchedDeals)
        {
            Deals.Add(deal);
        }
    }
}