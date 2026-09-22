using Microsoft.Data.Sqlite;
using GameDealWatcher.Domain.Entities;
using GameDealWatcher.Infrastructure.Database;
using Xunit;

namespace GameDealWatcher.Tests.Infrastructure.Database;

public class GameDealRepositoryTests
{
    private static async Task<(SqliteGameDealRepository repo, SqliteConnection keepAlive)> CreateTestRepoAsync()
    {
        var dbName = $"TestDb_{Guid.NewGuid()}";
        var connStr = $"Data Source={dbName};Mode=Memory;Cache=Shared";
        var keepAlive = new SqliteConnection(connStr);
        await keepAlive.OpenAsync();
        await DatabaseInitializer.InitializeAsync(connStr, CancellationToken.None);
        var repo = new SqliteGameDealRepository(connStr);
        return (repo, keepAlive);
    }

    private static GameDeal CreateDeal(string id, string providerGameId, string providerName, string title,
        decimal originalPrice, decimal currentPrice, int discountPct) =>
        new(id, providerGameId, providerName, title, null, null, null,
            originalPrice, currentPrice, discountPct, "USD",
            $"https://store.example.com/app/{providerGameId}/",
            null, null, null, null, false, false, null, null, null, null);

    [Fact]
    public async Task InsertAndGetDeals_ReturnsCorrectCount()
    {
        var (repo, keepAlive) = await CreateTestRepoAsync();
        try
        {
            var deals = new[]
            {
                CreateDeal("1", "steam1", "Steam", "Test Game", 59.99m, 29.99m, 50),
                CreateDeal("2", "steam2", "Steam", "Another Game", 49.99m, 9.99m, 80)
            };

            await repo.InsertDealsAsync(deals, CancellationToken.None);
            var result = await repo.GetAllDealsAsync(CancellationToken.None);

            Assert.Equal(2, result.Count);
        }
        finally
        {
            keepAlive.Dispose();
        }
    }

    [Fact]
    public async Task NeedsRefresh_ReturnsTrue_WhenNoHistory()
    {
        var (repo, keepAlive) = await CreateTestRepoAsync();
        try
        {
            var needs = await repo.NeedsRefreshAsync(TimeSpan.FromHours(24), CancellationToken.None);
            Assert.True(needs);
        }
        finally
        {
            keepAlive.Dispose();
        }
    }

    [Fact]
    public async Task NeedsRefresh_ReturnsTrue_WhenTimestampCorrupted()
    {
        var (repo, keepAlive) = await CreateTestRepoAsync();
        try
        {
            // Insert a corrupted timestamp
            await using (var conn = new SqliteConnection(keepAlive.ConnectionString))
            {
                await conn.OpenAsync();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "INSERT INTO Settings (Key, Value) VALUES ('LastSuccessfulRefresh', 'not-a-date');";
                await cmd.ExecuteNonQueryAsync();
            }

            // Should not throw — should return true (needs refresh)
            var needs = await repo.NeedsRefreshAsync(TimeSpan.FromHours(24), CancellationToken.None);
            Assert.True(needs);
        }
        finally
        {
            keepAlive.Dispose();
        }
    }

    [Fact]
    public async Task InsertDeals_UpdatesExistingDeal_OnConflict()
    {
        var (repo, keepAlive) = await CreateTestRepoAsync();
        try
        {
            var deal = CreateDeal("1", "steam1", "Steam", "Test Game", 59.99m, 29.99m, 50);
            await repo.InsertDealsAsync(new[] { deal }, CancellationToken.None);

            // Insert same deal with updated price
            var updatedDeal = deal with { CurrentPrice = 19.99m, DiscountPercentage = 67 };
            await repo.InsertDealsAsync(new[] { updatedDeal }, CancellationToken.None);

            var result = await repo.GetAllDealsAsync(CancellationToken.None);
            Assert.Single(result);
            Assert.Equal(19.99m, result[0].CurrentPrice);
            Assert.Equal(67, result[0].DiscountPercentage);
        }
        finally
        {
            keepAlive.Dispose();
        }
    }

    [Fact]
    public async Task DeleteDealsNotSeen_RemovesUnseenDeals_KeepsSeenDeals()
    {
        var (repo, keepAlive) = await CreateTestRepoAsync();
        try
        {
            var deals = new[]
            {
                CreateDeal("1", "steam1", "Steam", "Game A", 59.99m, 29.99m, 50),
                CreateDeal("2", "steam2", "Steam", "Game B", 49.99m, 9.99m, 80),
                CreateDeal("3", "steam3", "Steam", "Game C", 19.99m, 4.99m, 75)
            };
            await repo.InsertDealsAsync(deals, CancellationToken.None);

            // Keep only deals 1 and 3 — deal 2 should be deleted
            await repo.DeleteDealsNotSeenAsync(new[] { "1", "3" }, CancellationToken.None);

            var result = await repo.GetAllDealsAsync(CancellationToken.None);
            Assert.Equal(2, result.Count);
            Assert.Contains(result, d => d.Id == "1");
            Assert.Contains(result, d => d.Id == "3");
            Assert.DoesNotContain(result, d => d.Id == "2");
        }
        finally
        {
            keepAlive.Dispose();
        }
    }

    [Fact]
    public async Task DeleteDealsNotSeen_WithEmptyList_DeletesAll()
    {
        var (repo, keepAlive) = await CreateTestRepoAsync();
        try
        {
            var deals = new[]
            {
                CreateDeal("1", "steam1", "Steam", "Game A", 59.99m, 29.99m, 50),
                CreateDeal("2", "steam2", "Steam", "Game B", 49.99m, 9.99m, 80)
            };
            await repo.InsertDealsAsync(deals, CancellationToken.None);

            await repo.DeleteDealsNotSeenAsync(Array.Empty<string>(), CancellationToken.None);

            var result = await repo.GetAllDealsAsync(CancellationToken.None);
            Assert.Empty(result);
        }
        finally
        {
            keepAlive.Dispose();
        }
    }
}
