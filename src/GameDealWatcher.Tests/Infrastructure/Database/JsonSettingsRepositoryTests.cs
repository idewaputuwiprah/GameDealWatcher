using GameDealWatcher.Domain.Entities;
using GameDealWatcher.Infrastructure.Database;
using Xunit;

namespace GameDealWatcher.Tests.Infrastructure.Database;

public class JsonSettingsRepositoryTests : IDisposable
{
    private readonly string _tempFile;

    public JsonSettingsRepositoryTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"settings_test_{Guid.NewGuid()}.json");
    }

    public void Dispose()
    {
        try { if (File.Exists(_tempFile)) File.Delete(_tempFile); } catch { }
    }

    [Fact]
    public async Task GetSettingsAsync_NoFile_ReturnsDefaults()
    {
        var repo = new JsonSettingsRepository(_tempFile);
        var settings = await repo.GetSettingsAsync(CancellationToken.None);

        Assert.Equal(TimeSpan.FromHours(24), settings.RefreshInterval);
        Assert.True(settings.EpicNotifications);
        Assert.True(settings.SteamNotifications);
    }

    [Fact]
    public async Task SaveThenGet_RoundTripsCorrectly()
    {
        var repo = new JsonSettingsRepository(_tempFile);
        var original = new AppSettings
        {
            RefreshInterval = TimeSpan.FromHours(12),
            EpicNotifications = false,
            MinimumSteamDiscount = 75
        };

        await repo.SaveSettingsAsync(original, CancellationToken.None);
        var loaded = await repo.GetSettingsAsync(CancellationToken.None);

        Assert.Equal(TimeSpan.FromHours(12), loaded.RefreshInterval);
        Assert.False(loaded.EpicNotifications);
        Assert.Equal(75, loaded.MinimumSteamDiscount);
    }

    [Fact]
    public async Task GetSettingsAsync_CorruptedFile_ReturnsDefaults()
    {
        await File.WriteAllTextAsync(_tempFile, "{ this is not valid JSON !!!");
        var repo = new JsonSettingsRepository(_tempFile);

        var settings = await repo.GetSettingsAsync(CancellationToken.None);

        Assert.Equal(TimeSpan.FromHours(24), settings.RefreshInterval);
    }

    [Fact]
    public async Task SaveSettingsAsync_DoesNotLeaveTempFile()
    {
        var repo = new JsonSettingsRepository(_tempFile);
        await repo.SaveSettingsAsync(new AppSettings(), CancellationToken.None);

        Assert.False(File.Exists(_tempFile + ".tmp"), "Temp file should not remain after save");
        Assert.True(File.Exists(_tempFile), "Settings file should exist after save");
    }
}
