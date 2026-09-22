using System.Text.Json;
using GameDealWatcher.Domain.Entities;
using GameDealWatcher.Domain.Interfaces;

namespace GameDealWatcher.Infrastructure.Database;

public sealed class JsonSettingsRepository : ISettingsRepository
{
    private readonly string _filePath;
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public JsonSettingsRepository(string filePath)
    {
        _filePath = filePath;
    }

    public async Task<AppSettings> GetSettingsAsync(CancellationToken ct)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            if (!File.Exists(_filePath)) return new AppSettings();
            await using var stream = File.OpenRead(_filePath);
            var settings = await JsonSerializer.DeserializeAsync<AppSettings>(stream, cancellationToken: ct);
            return settings ?? new AppSettings();
        }
        catch (JsonException)
        {
            // Corrupted settings file — return defaults rather than crashing
            return new AppSettings();
        }
        catch (IOException)
        {
            return new AppSettings();
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task SaveSettingsAsync(AppSettings settings, CancellationToken ct)
    {
        await _semaphore.WaitAsync(ct);
        try
        {
            // Write to a temp file first, then atomically rename to avoid corruption
            var tmpPath = _filePath + ".tmp";
            await using (var stream = File.Create(tmpPath))
            {
                await JsonSerializer.SerializeAsync(stream, settings, cancellationToken: ct);
                await stream.FlushAsync(ct);
            }
            File.Move(tmpPath, _filePath, overwrite: true);
        }
        finally
        {
            _semaphore.Release();
        }
    }
}