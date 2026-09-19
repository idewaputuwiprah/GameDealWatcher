using GameDealWatcher.Domain.Entities;
using GameDealWatcher.Domain.Interfaces;

namespace GameDealWatcher.Application.Services;

public interface IGameDealService
{
    Task<IReadOnlyList<GameDeal>> GetAllDealsAsync(CancellationToken ct);
    Task<bool> NeedsRefreshAsync(CancellationToken ct);
    Task RefreshAllDealsAsync(CancellationToken ct);
}