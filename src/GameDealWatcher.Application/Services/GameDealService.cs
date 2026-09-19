using GameDealWatcher.Domain.Entities;
using GameDealWatcher.Domain.Interfaces;
using GameDealWatcher.Infrastructure.Database;
using GameDealWatcher.Infrastructure.Providers;
using Microsoft.Extensions.Logging;

namespace GameDealWatcher.Application.Services;

public sealed class GameDealService : IGameDealService
{
    private readonly IGameDealRepository _repository;
    private readonly IGameDealProvider _steamProvider;
    private readonly IGameDealProvider _epicProvider;
    private readonly ISettingsRepository _settingsRepository;
    private readonly ILogger<GameDealService> _logger;

    public GameDealService(
        IGameDealRepository repository,
        SteamProvider steamProvider,
        EpicGamesProvider epicProvider,
        ISettingsRepository settingsRepository,
        ILogger<GameDealService> logger)
    {
        _repository = repository;
        _steamProvider = steamProvider;
        _epicProvider = epicProvider;
        _settingsRepository = settingsRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<GameDeal>> GetAllDealsAsync(CancellationToken ct)
    {
        return await _repository.GetAllDealsAsync(ct);
    }

    public async Task<bool> NeedsRefreshAsync(CancellationToken ct)
    {
        var settings = await _settingsRepository.GetSettingsAsync(ct);
        return await _repository.NeedsRefreshAsync(settings.RefreshInterval, ct);
    }

    public async Task RefreshAllDealsAsync(CancellationToken ct)
    {
        _logger.LogInformation("Refresh started at {Time}", DateTimeOffset.UtcNow);
        var settings = await _settingsRepository.GetSettingsAsync(ct);

        var steamDealsTask = RefreshSteamAsync(ct);
        var epicDealsTask = RefreshEpicAsync(ct);
        await Task.WhenAll(steamDealsTask, epicDealsTask);

        var steamDeals = await steamDealsTask;
        var epicDeals = await epicDealsTask;

        await _settingsRepository.SaveSettingsAsync(settings with { RefreshTime = TimeOnly.FromDateTime(DateTime.UtcNow.ToLocalTime()) }, ct);
        await _repository.SaveLastRefreshAsync(settings.RefreshInterval, ct);
        _logger.LogInformation("All refreshes complete at {Time}", DateTimeOffset.UtcNow);
    }

    private async Task<List<GameDeal>> RefreshSteamAsync(CancellationToken ct)
    {
        try
        {
            var deals = (await _steamProvider.GetDealsAsync(ct)).ToList();
            await _repository.InsertDealsAsync(deals, ct);
            await _repository.RecordRefreshAsync("Steam", true, deals.Count, null, ct);
            _logger.LogInformation("Steam refresh complete: {Count} deals", deals.Count);
            return deals;
        }
        catch (Exception ex)
        {
            await _repository.RecordRefreshAsync("Steam", false, 0, ex.Message, ct);
            _logger.LogError(ex, "Steam refresh failed");
            return [];
        }
    }

    private async Task<List<GameDeal>> RefreshEpicAsync(CancellationToken ct)
    {
        try
        {
            var deals = (await _epicProvider.GetDealsAsync(ct)).ToList();
            await _repository.InsertDealsAsync(deals, ct);
            await _repository.RecordRefreshAsync("Epic", true, deals.Count, null, ct);
            _logger.LogInformation("Epic refresh complete: {Count} deals", deals.Count);
            return deals;
        }
        catch (Exception ex)
        {
            await _repository.RecordRefreshAsync("Epic", false, 0, ex.Message, ct);
            _logger.LogError(ex, "Epic refresh failed");
            return [];
        }
    }
}