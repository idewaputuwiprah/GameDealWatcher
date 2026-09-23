using GameDealWatcher.Domain.Entities;
using GameDealWatcher.Domain.Interfaces;
using Microsoft.Extensions.Logging;

namespace GameDealWatcher.Application.Services;

public sealed class GameDealService : IGameDealService
{
    private readonly IGameDealRepository _repository;
    private readonly IEnumerable<IGameDealProvider> _providers;
    private readonly ISettingsRepository _settingsRepository;
    private readonly INotificationService _notificationService;
    private readonly ILogger<GameDealService> _logger;

    private static readonly TimeSpan StaleDealThreshold = TimeSpan.FromDays(30);

    public GameDealService(
        IGameDealRepository repository,
        IEnumerable<IGameDealProvider> providers,
        ISettingsRepository settingsRepository,
        INotificationService notificationService,
        ILogger<GameDealService> logger)
    {
        _repository = repository;
        _providers = providers;
        _settingsRepository = settingsRepository;
        _notificationService = notificationService;
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

        // 1. Snapshot existing deals BEFORE refresh for price-change comparison
        var existingDeals = await _repository.GetAllDealsAsync(ct);
        var existingMap = existingDeals.GroupBy(d => (d.ProviderName, d.ProviderGameId)).ToDictionary(g => g.Key, g => g.First());

        // 2. Refresh all providers concurrently with isolated failure boundaries
        var refreshTasks = _providers.Select(p => RefreshProviderAsync(p, ct)).ToList();
        var providerResults = await Task.WhenAll(refreshTasks);

        // 3. Collect all results
        var allNewDeals = providerResults.SelectMany(d => d).ToList();

        // 4. Compare old vs new — record price changes + detect new deals
        var newDealNotifications = new List<GameDeal>();
        var priceChangedDeals = new List<GameDeal>();
        foreach (var newDeal in allNewDeals)
        {
            var key = (newDeal.ProviderName, newDeal.ProviderGameId);
            if (existingMap.TryGetValue(key, out var oldDeal))
            {
                if (oldDeal.CurrentPrice != newDeal.CurrentPrice)
                    priceChangedDeals.Add(newDeal);
            }
            else
            {
                newDealNotifications.Add(newDeal);
            }
        }
        if (priceChangedDeals.Count > 0)
            await _repository.RecordPriceChangesAsync(priceChangedDeals, ct);

        // 5. Send notifications (respecting user settings)
        // Skip on first refresh (empty database) to avoid spamming the user
        if (existingDeals.Count > 0)
        {
            SendNotifications(newDealNotifications, settings);
        }

        // 6. Only mark refresh if at least one provider returned data
        if (allNewDeals.Count > 0)
        {
            // Delete deals that were not seen in this refresh (no longer active)
            try
            {
                var seenIds = allNewDeals.Select(d => d.Id).ToList();
                await _repository.DeleteDealsNotSeenAsync(seenIds, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete unseen deals");
            }

            try
            {
                await _repository.SaveLastRefreshAsync(settings.RefreshInterval, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save last refresh timestamp");
            }

            // Clean up deals not seen in 30 days (secondary safety net)
            try
            {
                await _repository.DeleteStaleDealsAsync(StaleDealThreshold, ct);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to clean up stale deals");
            }
        }
        else
        {
            _logger.LogWarning("All providers returned no deals — not updating refresh timestamp");
        }

        _logger.LogInformation("All refreshes complete at {Time}", DateTimeOffset.UtcNow);
    }

    private async Task<List<GameDeal>> RefreshProviderAsync(IGameDealProvider provider, CancellationToken ct)
    {
        try
        {
            var deals = (await provider.GetDealsAsync(ct)).ToList();
            await _repository.InsertDealsAsync(deals, ct);
            await _repository.RecordRefreshAsync(provider.ProviderName, true, deals.Count, null, ct);
            _logger.LogInformation("{Provider} refresh complete: {Count} deals", provider.ProviderName, deals.Count);
            return deals;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            try
            {
                await _repository.RecordRefreshAsync(provider.ProviderName, false, 0, ex.Message, ct);
            }
            catch (Exception logEx)
            {
                _logger.LogError(logEx, "Failed to record refresh failure for {Provider}", provider.ProviderName);
            }
            _logger.LogError(ex, "{Provider} refresh failed", provider.ProviderName);
            return [];
        }
    }

    private void SendNotifications(List<GameDeal> newDeals, AppSettings settings)
    {
        foreach (var deal in newDeals)
        {
            if (deal.ProviderName == ProviderNames.Epic && settings.EpicNotifications && deal.IsCurrentlyFree)
            {
                _notificationService.ShowDealNotification("Free Game!", $"{deal.Title} is now free on Epic Games Store!");
            }
            else if (deal.ProviderName == ProviderNames.Steam && settings.SteamNotifications
                     && deal.DiscountPercentage >= settings.MinimumSteamDiscount)
            {
                _notificationService.ShowDealNotification("Steam Deal!", $"{deal.Title} is {deal.DiscountPercentage}% off on Steam!");
            }
        }
    }
}
