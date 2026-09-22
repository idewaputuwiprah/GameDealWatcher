using GameDealWatcher.Domain.Entities;

namespace GameDealWatcher.Domain.Interfaces;

public interface IGameDealRepository
{
    Task InsertDealsAsync(IReadOnlyList<GameDeal> deals, CancellationToken ct);
    Task<IReadOnlyList<GameDeal>> GetAllDealsAsync(CancellationToken ct);
    Task<bool> NeedsRefreshAsync(TimeSpan refreshInterval, CancellationToken ct);
    Task RecordRefreshAsync(string providerName, bool success, int itemCount, string? errorMessage, CancellationToken ct);
    Task RecordPriceChangeAsync(string dealId, decimal originalPrice, decimal currentPrice, int discountPercentage, string currency, CancellationToken ct);
    Task RecordPriceChangesAsync(IReadOnlyList<GameDeal> deals, CancellationToken ct);
    Task SaveLastRefreshAsync(TimeSpan refreshInterval, CancellationToken ct);
    Task DeleteStaleDealsAsync(TimeSpan maxAge, CancellationToken ct);
    Task DeleteDealsNotSeenAsync(IReadOnlyList<string> seenDealIds, CancellationToken ct);
}
