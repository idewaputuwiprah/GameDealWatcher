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
        var connStr = $"Data Source={dbName};Mode=Memory;Cache=Shared;Foreign Keys=True";
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

    private static async Task<int> CountDealHistoryRowsAsync(string connectionString)
    {
        await using var conn = new SqliteConnection(connectionString);
        await conn.OpenAsync();
        var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT COUNT(*) FROM DealHistory;";
        return Convert.ToInt32(await cmd.ExecuteScalarAsync());
    }

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
            await repo.DeleteDealsNotSeenAsync("Steam", new[] { "1", "3" }, CancellationToken.None);

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

            await repo.DeleteDealsNotSeenAsync("Steam", Array.Empty<string>(), CancellationToken.None);

            var result = await repo.GetAllDealsAsync(CancellationToken.None);
            Assert.Empty(result);
        }
        finally
        {
            keepAlive.Dispose();
        }
    }

    [Fact]
    public async Task DeleteDealsNotSeen_OnlyAffectsSpecifiedProvider()
    {
        var (repo, keepAlive) = await CreateTestRepoAsync();
        try
        {
            var deals = new[]
            {
                CreateDeal("steam_1", "1", "Steam", "Steam Game", 59.99m, 29.99m, 50),
                CreateDeal("epic_1", "1", "Epic", "Epic Game", 0m, 0m, 0)
            };
            await repo.InsertDealsAsync(deals, CancellationToken.None);

            // Steam's refresh saw nothing this cycle — only Steam's deals should be pruned
            await repo.DeleteDealsNotSeenAsync("Steam", Array.Empty<string>(), CancellationToken.None);

            var result = await repo.GetAllDealsAsync(CancellationToken.None);
            Assert.Single(result);
            Assert.Equal("epic_1", result[0].Id);
        }
        finally
        {
            keepAlive.Dispose();
        }
    }

    [Fact]
    public async Task DeletingDeal_CascadesToDealHistory_WhenForeignKeysEnabled()
    {
        var (repo, keepAlive) = await CreateTestRepoAsync();
        try
        {
            var deal = CreateDeal("1", "steam1", "Steam", "Test Game", 59.99m, 29.99m, 50);
            await repo.InsertDealsAsync(new[] { deal }, CancellationToken.None);
            await repo.RecordPriceChangesAsync(new[] { deal }, CancellationToken.None);

            Assert.Equal(1, await CountDealHistoryRowsAsync(keepAlive.ConnectionString));

            // Steam's refresh saw nothing this cycle, so the deal (and its history via
            // ON DELETE CASCADE) should be removed now that Foreign Keys=True is set.
            await repo.DeleteDealsNotSeenAsync("Steam", Array.Empty<string>(), CancellationToken.None);

            Assert.Equal(0, await CountDealHistoryRowsAsync(keepAlive.ConnectionString));
        }
        finally
        {
            keepAlive.Dispose();
        }
    }

    [Fact]
    public async Task Migration_CleansUpOrphanedDealHistory_FromBeforeForeignKeysWereEnabled()
    {
        var dbName = $"TestDb_{Guid.NewGuid()}";
        // No "Foreign Keys=True" here — this reproduces the pre-fix connection string,
        // where deleting a GameDeal left its DealHistory rows orphaned.
        var legacyConnStr = $"Data Source={dbName};Mode=Memory;Cache=Shared";
        var keepAlive = new SqliteConnection(legacyConnStr);
        await keepAlive.OpenAsync();
        try
        {
            await DatabaseInitializer.InitializeAsync(legacyConnStr, CancellationToken.None);

            var repo = new SqliteGameDealRepository(legacyConnStr);
            var deal = CreateDeal("1", "steam1", "Steam", "Test Game", 59.99m, 29.99m, 50);
            await repo.InsertDealsAsync(new[] { deal }, CancellationToken.None);
            await repo.RecordPriceChangesAsync(new[] { deal }, CancellationToken.None);

            await using (var conn = new SqliteConnection(legacyConnStr))
            {
                await conn.OpenAsync();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "DELETE FROM GameDeals WHERE Id = '1';";
                await cmd.ExecuteNonQueryAsync();
            }

            // Roll the recorded schema version back to simulate a DB created before this fix shipped
            await using (var conn = new SqliteConnection(legacyConnStr))
            {
                await conn.OpenAsync();
                var cmd = conn.CreateCommand();
                cmd.CommandText = "UPDATE Settings SET Value = '1' WHERE Key = 'SchemaVersion';";
                await cmd.ExecuteNonQueryAsync();
            }

            Assert.Equal(1, await CountDealHistoryRowsAsync(legacyConnStr));

            // Re-running InitializeAsync, as happens on every app launch, should now migrate
            // to schema version 2 and purge the orphaned history row.
            await DatabaseInitializer.InitializeAsync(legacyConnStr, CancellationToken.None);

            Assert.Equal(0, await CountDealHistoryRowsAsync(legacyConnStr));
        }
        finally
        {
            keepAlive.Dispose();
        }
    }
}
