using Dapper;
using GameDealWatcher.Domain.Entities;
using GameDealWatcher.Domain.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

using System.Globalization;

namespace GameDealWatcher.Infrastructure.Database;

public sealed class SqliteGameDealRepository : IGameDealRepository
{
    private readonly string _connectionString;
    private readonly ILogger<SqliteGameDealRepository>? _logger;

    public SqliteGameDealRepository(string connectionString, ILogger<SqliteGameDealRepository>? logger = null)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task<IReadOnlyList<string>> InsertDealsAsync(IReadOnlyList<GameDeal> deals, CancellationToken ct)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        var failedIds = new List<string>();
        foreach (var d in deals)
        {
            try
            {
                var parameters = new
                {
                    Id = d.Id,
                    ProviderName = d.ProviderName,
                    ProviderGameId = d.ProviderGameId,
                    Title = d.Title,
                    Description = d.Description,
                    Publisher = d.Publisher,
                    Developer = d.Developer,
                    OriginalPrice = d.OriginalPrice,
                    CurrentPrice = d.CurrentPrice,
                    DiscountPercentage = d.DiscountPercentage,
                    Currency = d.Currency,
                    StoreUrl = d.StoreUrl,
                    ImageUrl = d.ImageUrl,
                    ThumbnailUrl = d.ThumbnailUrl,
                    StartsAt = d.StartsAt?.ToString("o"),
                    EndsAt = d.EndsAt?.ToString("o"),
                    IsCurrentlyFree = d.IsCurrentlyFree ? 1 : 0,
                    IsUpcoming = d.IsUpcoming ? 1 : 0,
                    ReviewScore = d.ReviewScore,
                    ReviewCount = d.ReviewCount,
                    ReleaseDate = d.ReleaseDate?.ToString("o"),
                    Genres = d.Genres != null ? string.Join(",", d.Genres) : null,
                    LastUpdated = DateTimeOffset.UtcNow.ToString("o")
                };

                await connection.ExecuteAsync(
                    new CommandDefinition(
                        @"INSERT INTO GameDeals (Id, ProviderName, ProviderGameId, Title, Description, Publisher, Developer, OriginalPrice, CurrentPrice, DiscountPercentage, Currency, StoreUrl, ImageUrl, ThumbnailUrl, StartsAt, EndsAt, IsCurrentlyFree, IsUpcoming, ReviewScore, ReviewCount, ReleaseDate, Genres, LastUpdated)
                      VALUES (@Id, @ProviderName, @ProviderGameId, @Title, @Description, @Publisher, @Developer, @OriginalPrice, @CurrentPrice, @DiscountPercentage, @Currency, @StoreUrl, @ImageUrl, @ThumbnailUrl, @StartsAt, @EndsAt, @IsCurrentlyFree, @IsUpcoming, @ReviewScore, @ReviewCount, @ReleaseDate, @Genres, @LastUpdated)
                      ON CONFLICT(ProviderName, ProviderGameId) DO UPDATE SET
                          Id = excluded.Id, Title = excluded.Title, Description = excluded.Description,
                          Publisher = excluded.Publisher, Developer = excluded.Developer,
                          OriginalPrice = excluded.OriginalPrice, CurrentPrice = excluded.CurrentPrice,
                          DiscountPercentage = excluded.DiscountPercentage, Currency = excluded.Currency,
                          StoreUrl = excluded.StoreUrl, ImageUrl = excluded.ImageUrl, ThumbnailUrl = excluded.ThumbnailUrl,
                          StartsAt = excluded.StartsAt, EndsAt = excluded.EndsAt, IsCurrentlyFree = excluded.IsCurrentlyFree,
                          IsUpcoming = excluded.IsUpcoming, ReviewScore = excluded.ReviewScore, ReviewCount = excluded.ReviewCount,
                          ReleaseDate = excluded.ReleaseDate, Genres = excluded.Genres, LastUpdated = excluded.LastUpdated;",
                        parameters,
                        transaction,
                        cancellationToken: ct));
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                failedIds.Add(d.Id);
                _logger?.LogWarning(ex, "Failed to insert deal {DealId}", d.Id);
            }
        }
        if (failedIds.Count > 0)
        {
            _logger?.LogWarning("Inserted {Success} deals, failed to insert {Failed} deals: {FailedIds}",
                deals.Count - failedIds.Count, failedIds.Count, string.Join(", ", failedIds));
        }
        await transaction.CommitAsync(ct);
        return failedIds;
    }

    public async Task<IReadOnlyList<GameDeal>> GetAllDealsAsync(CancellationToken ct)
    {
        await using var connection = new SqliteConnection(_connectionString);
        var rows = await connection.QueryAsync<GameDealRow>(
            new CommandDefinition(
                "SELECT Id, ProviderName, ProviderGameId, Title, Description, Publisher, Developer, OriginalPrice, CurrentPrice, DiscountPercentage, Currency, StoreUrl, ImageUrl, ThumbnailUrl, StartsAt, EndsAt, IsCurrentlyFree, IsUpcoming, ReviewScore, ReviewCount, ReleaseDate, Genres, LastUpdated FROM GameDeals;",
                cancellationToken: ct));
        return rows.Select(MapRowToDeal).ToList();
    }

    public async Task<bool> NeedsRefreshAsync(TimeSpan refreshInterval, CancellationToken ct)
    {
        await using var connection = new SqliteConnection(_connectionString);
        var result = await connection.QuerySingleOrDefaultAsync<string?>(
            new CommandDefinition(
                "SELECT Value FROM Settings WHERE Key = 'LastSuccessfulRefresh';",
                cancellationToken: ct));
        if (result == null) return true;
        if (!DateTimeOffset.TryParse(result, out var lastRefresh)) return true;
        return DateTimeOffset.UtcNow - lastRefresh > refreshInterval;
    }

    public async Task RecordRefreshAsync(string providerName, bool success, int itemCount, string? errorMessage, CancellationToken ct)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.ExecuteAsync(
            new CommandDefinition(
                @"INSERT INTO RefreshHistory (Id, ProviderName, RefreshedAt, Success, ItemCount, ErrorMessage) VALUES (@Id, @ProviderName, @RefreshedAt, @Success, @ItemCount, @ErrorMessage);",
                new
                {
                    Id = Guid.NewGuid().ToString(),
                    ProviderName = providerName,
                    RefreshedAt = DateTimeOffset.UtcNow.ToString("o"),
                    Success = success ? 1 : 0,
                    ItemCount = itemCount,
                    ErrorMessage = errorMessage
                },
                cancellationToken: ct));
    }

    public async Task SaveLastRefreshAsync(TimeSpan refreshInterval, CancellationToken ct)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.ExecuteAsync(
            new CommandDefinition(
                @"INSERT INTO Settings (Key, Value) VALUES ('LastSuccessfulRefresh', @Value)
            ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;",
                new { Value = DateTimeOffset.UtcNow.ToString("o") },
                cancellationToken: ct));
    }

    public async Task RecordPriceChangesAsync(IReadOnlyList<GameDeal> deals, CancellationToken ct)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        var recordedAt = DateTimeOffset.UtcNow.ToString("o");
        var parameters = deals.Select(d => new
        {
            Id = Guid.NewGuid().ToString(),
            DealId = d.Id,
            OriginalPrice = d.OriginalPrice,
            CurrentPrice = d.CurrentPrice,
            DiscountPercentage = d.DiscountPercentage,
            Currency = d.Currency,
            RecordedAt = recordedAt
        });
        await connection.ExecuteAsync(
            new CommandDefinition(
                @"INSERT INTO DealHistory (Id, DealId, OriginalPrice, CurrentPrice, DiscountPercentage, Currency, RecordedAt) VALUES (@Id, @DealId, @OriginalPrice, @CurrentPrice, @DiscountPercentage, @Currency, @RecordedAt);",
                parameters,
                transaction,
                cancellationToken: ct));
        await transaction.CommitAsync(ct);
    }

    public async Task DeleteStaleDealsAsync(TimeSpan maxAge, CancellationToken ct)
    {
        await using var connection = new SqliteConnection(_connectionString);
        var cutoff = (DateTimeOffset.UtcNow - maxAge).ToString("o");
        await connection.ExecuteAsync(
            new CommandDefinition(
                "DELETE FROM GameDeals WHERE LastUpdated < @Cutoff;",
                new { Cutoff = cutoff },
                cancellationToken: ct));
    }

    public async Task DeleteDealsNotSeenAsync(string providerName, IReadOnlyList<string> seenDealIds, CancellationToken ct)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        if (seenDealIds.Count == 0)
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    "DELETE FROM GameDeals WHERE ProviderName = @ProviderName;",
                    new { ProviderName = providerName },
                    transaction,
                    cancellationToken: ct));
        }
        else
        {
            await connection.ExecuteAsync(
                new CommandDefinition(
                    "CREATE TEMP TABLE _SeenIds (Id TEXT NOT NULL);",
                    transaction: transaction,
                    cancellationToken: ct));

            const int batchSize = 200;
            for (int i = 0; i < seenDealIds.Count; i += batchSize)
            {
                var batch = seenDealIds.Skip(i).Take(batchSize).Select(id => new { Id = id }).ToList();
                await connection.ExecuteAsync(
                    new CommandDefinition(
                        "INSERT INTO _SeenIds (Id) VALUES (@Id);",
                        batch,
                        transaction,
                        cancellationToken: ct));
            }

            await connection.ExecuteAsync(
                new CommandDefinition(
                    "DELETE FROM GameDeals WHERE ProviderName = @ProviderName AND Id NOT IN (SELECT Id FROM _SeenIds);",
                    new { ProviderName = providerName },
                    transaction,
                    cancellationToken: ct));

            await connection.ExecuteAsync(
                new CommandDefinition(
                    "DROP TABLE _SeenIds;",
                    transaction: transaction,
                    cancellationToken: ct));
        }

        await transaction.CommitAsync(ct);
    }

    private static GameDeal MapRowToDeal(GameDealRow r) => new(
        Id: r.Id,
        ProviderGameId: r.ProviderGameId,
        ProviderName: r.ProviderName,
        Title: r.Title,
        Description: r.Description,
        Publisher: r.Publisher,
        Developer: r.Developer,
        OriginalPrice: decimal.Round((decimal)r.OriginalPrice, 2, MidpointRounding.AwayFromZero),
        CurrentPrice: decimal.Round((decimal)r.CurrentPrice, 2, MidpointRounding.AwayFromZero),
        DiscountPercentage: (int)r.DiscountPercentage,
        Currency: r.Currency ?? "USD",
        StoreUrl: r.StoreUrl,
        ImageUrl: r.ImageUrl,
        ThumbnailUrl: r.ThumbnailUrl,
        StartsAt: TryParseDate(r.StartsAt),
        EndsAt: TryParseDate(r.EndsAt),
        IsCurrentlyFree: r.IsCurrentlyFree != 0,
        IsUpcoming: r.IsUpcoming != 0,
        ReviewScore: r.ReviewScore != null ? (int?)r.ReviewScore : null,
        ReviewCount: r.ReviewCount != null ? (int?)r.ReviewCount : null,
        ReleaseDate: TryParseDate(r.ReleaseDate),
        Genres: r.Genres?.Split(',', StringSplitOptions.RemoveEmptyEntries));

    private static DateTimeOffset? TryParseDate(string? value) =>
        value != null && DateTimeOffset.TryParse(value, out var dto) ? dto : null;

    private sealed class GameDealRow
    {
        public string Id { get; set; } = "";
        public string ProviderName { get; set; } = "";
        public string ProviderGameId { get; set; } = "";
        public string Title { get; set; } = "";
        public string? Description { get; set; }
        public string? Publisher { get; set; }
        public string? Developer { get; set; }
        public double OriginalPrice { get; set; }
        public double CurrentPrice { get; set; }
        public long DiscountPercentage { get; set; }
        public string? Currency { get; set; }
        public string StoreUrl { get; set; } = "";
        public string? ImageUrl { get; set; }
        public string? ThumbnailUrl { get; set; }
        public string? StartsAt { get; set; }
        public string? EndsAt { get; set; }
        public long IsCurrentlyFree { get; set; }
        public long IsUpcoming { get; set; }
        public long? ReviewScore { get; set; }
        public long? ReviewCount { get; set; }
        public string? ReleaseDate { get; set; }
        public string? Genres { get; set; }
        public string? LastUpdated { get; set; }
    }
}
