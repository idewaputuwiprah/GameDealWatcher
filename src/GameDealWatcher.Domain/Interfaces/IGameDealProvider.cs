namespace GameDealWatcher.Domain.Interfaces;

using GameDealWatcher.Domain.Entities;

public interface IGameDealProvider
{
    string ProviderName { get; }
    Task<IReadOnlyList<GameDeal>> GetDealsAsync(CancellationToken ct);
}
