using Microsoft.Data.Sqlite;
using GameDealWatcher.Domain.Entities;
using GameDealWatcher.Infrastructure.Database;
using Xunit;

namespace GameDealWatcher.Tests.Infrastructure.Database;

public class GameDealRepositoryTests
{
    [Fact]
    public async Task InsertAndGetDeals_ReturnsCorrectCount()
    {
        var dbName = $"TestDb_{Guid.NewGuid()}";
        var connStr = $"Data Source={dbName};Mode=Memory;Cache=Shared";
        
        var keepAlive = new SqliteConnection(connStr);
        keepAlive.Open();
        await DatabaseInitializer.InitializeAsync(connStr, CancellationToken.None);
        var repo = new SqliteGameDealRepository(connStr);

        var deals = new[]
        {
            new GameDeal("1", "steam1", "Steam", "Test Game", null, null, null, 59.99m, 29.99m, 50, "USD", "https://store.steampowered.com/app/1/", null, null, null, null, false, false, null, null, null, null),
            new GameDeal("2", "steam2", "Steam", "Another Game", null, null, null, 49.99m, 9.99m, 80, "USD", "https://store.steampowered.com/app/2/", null, null, null, null, false, false, null, null, null, null)
        };

        await repo.InsertDealsAsync(deals, CancellationToken.None);
        var result = await repo.GetAllDealsAsync(CancellationToken.None);

        Assert.Equal(2, result.Count);
        keepAlive.Close();
    }

    [Fact]
    public async Task NeedsRefresh_ReturnsTrue_WhenNoHistory()
    {
        var dbName = $"TestDb_{Guid.NewGuid()}";
        var connStr = $"Data Source={dbName};Mode=Memory;Cache=Shared";

        var keepAlive = new SqliteConnection(connStr);
        keepAlive.Open();
        await DatabaseInitializer.InitializeAsync(connStr, CancellationToken.None);
        var repo = new SqliteGameDealRepository(connStr);

        var needs = await repo.NeedsRefreshAsync(TimeSpan.FromHours(24), CancellationToken.None);
        Assert.True(needs);
        keepAlive.Close();
    }
}