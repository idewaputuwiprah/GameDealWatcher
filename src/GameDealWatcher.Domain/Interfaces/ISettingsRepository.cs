using GameDealWatcher.Domain.Entities;

namespace GameDealWatcher.Domain.Interfaces;

public interface ISettingsRepository
{
    Task<AppSettings> GetSettingsAsync(CancellationToken ct);
    Task SaveSettingsAsync(AppSettings settings, CancellationToken ct);
}
