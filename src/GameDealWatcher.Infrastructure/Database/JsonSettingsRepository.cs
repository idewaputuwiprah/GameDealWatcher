using System.Text.Json;
using GameDealWatcher.Domain.Entities;

namespace GameDealWatcher.Infrastructure.Database;

public interface ISettingsRepository
{
    Task<AppSettings> GetSettingsAsync(CancellationToken ct);
    Task SaveSettingsAsync(AppSettings settings, CancellationToken ct);
}

public sealed class JsonSettingsRepository : ISettingsRepository
{
    private readonly string _filePath;

    public JsonSettingsRepository(string filePath)
    {
        _filePath = filePath;
    }

    public async Task<AppSettings> GetSettingsAsync(CancellationToken ct)
    {
        if (!File.Exists(_filePath)) return new AppSettings();
        await using var stream = File.OpenRead(_filePath);
        var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream, cancellationToken: ct);
        return settings ?? new AppSettings();
    }

    public async Task SaveSettingsAsync(AppSettings settings, CancellationToken ct)
    {
        await using var stream = File.Create(_filePath);
        await JsonSerializer.SerializeAsync(stream, settings, cancellationToken: ct);
        await stream.FlushAsync(ct);
    }
}