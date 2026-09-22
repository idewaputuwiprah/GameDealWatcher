using GameDealWatcher.Domain.Entities;
using GameDealWatcher.Domain.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;

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

    public async Task InsertDealsAsync(IReadOnlyList<GameDeal> deals, CancellationToken ct)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        var cmd = connection.CreateCommand();
        cmd.Transaction = (SqliteTransaction)transaction;
        cmd.CommandText = @"INSERT INTO GameDeals (Id, ProviderName, ProviderGameId, Title, Description, Publisher, Developer, OriginalPrice, CurrentPrice, DiscountPercentage, Currency, StoreUrl, ImageUrl, ThumbnailUrl, StartsAt, EndsAt, IsCurrentlyFree, IsUpcoming, ReviewScore, ReviewCount, ReleaseDate, Genres, LastUpdated) VALUES (@Id, @ProviderName, @ProviderGameId, @Title, @Description, @Publisher, @Developer, @OriginalPrice, @CurrentPrice, @DiscountPercentage, @Currency, @StoreUrl, @ImageUrl, @ThumbnailUrl, @StartsAt, @EndsAt, @IsCurrentlyFree, @IsUpcoming, @ReviewScore, @ReviewCount, @ReleaseDate, @Genres, @LastUpdated) ON CONFLICT(ProviderName, ProviderGameId) DO UPDATE SET Id = excluded.Id, Title = excluded.Title, Description = excluded.Description, Publisher = excluded.Publisher, Developer = excluded.Developer, OriginalPrice = excluded.OriginalPrice, CurrentPrice = excluded.CurrentPrice, DiscountPercentage = excluded.DiscountPercentage, Currency = excluded.Currency, StoreUrl = excluded.StoreUrl, ImageUrl = excluded.ImageUrl, ThumbnailUrl = excluded.ThumbnailUrl, StartsAt = excluded.StartsAt, EndsAt = excluded.EndsAt, IsCurrentlyFree = excluded.IsCurrentlyFree, IsUpcoming = excluded.IsUpcoming, ReviewScore = excluded.ReviewScore, ReviewCount = excluded.ReviewCount, ReleaseDate = excluded.ReleaseDate, Genres = excluded.Genres, LastUpdated = excluded.LastUpdated;";
        var pId = cmd.CreateParameter(); pId.ParameterName = "@Id";
        var pProvider = cmd.CreateParameter(); pProvider.ParameterName = "@ProviderName";
        var pProviderGameId = cmd.CreateParameter(); pProviderGameId.ParameterName = "@ProviderGameId";
        var pTitle = cmd.CreateParameter(); pTitle.ParameterName = "@Title";
        var pDesc = cmd.CreateParameter(); pDesc.ParameterName = "@Description";
        var pPublisher = cmd.CreateParameter(); pPublisher.ParameterName = "@Publisher";
        var pDeveloper = cmd.CreateParameter(); pDeveloper.ParameterName = "@Developer";
        var pOrig = cmd.CreateParameter(); pOrig.ParameterName = "@OriginalPrice";
        var pCurr = cmd.CreateParameter(); pCurr.ParameterName = "@CurrentPrice";
        var pDisc = cmd.CreateParameter(); pDisc.ParameterName = "@DiscountPercentage";
        var pCurrency = cmd.CreateParameter(); pCurrency.ParameterName = "@Currency";
        var pUrl = cmd.CreateParameter(); pUrl.ParameterName = "@StoreUrl";
        var pImg = cmd.CreateParameter(); pImg.ParameterName = "@ImageUrl";
        var pThumb = cmd.CreateParameter(); pThumb.ParameterName = "@ThumbnailUrl";
        var pStart = cmd.CreateParameter(); pStart.ParameterName = "@StartsAt";
        var pEnd = cmd.CreateParameter(); pEnd.ParameterName = "@EndsAt";
        var pFree = cmd.CreateParameter(); pFree.ParameterName = "@IsCurrentlyFree";
        var pUpcoming = cmd.CreateParameter(); pUpcoming.ParameterName = "@IsUpcoming";
        var pReviewScore = cmd.CreateParameter(); pReviewScore.ParameterName = "@ReviewScore";
        var pReviewCount = cmd.CreateParameter(); pReviewCount.ParameterName = "@ReviewCount";
        var pReleaseDate = cmd.CreateParameter(); pReleaseDate.ParameterName = "@ReleaseDate";
        var pGenres = cmd.CreateParameter(); pGenres.ParameterName = "@Genres";
        var pUpdated = cmd.CreateParameter(); pUpdated.ParameterName = "@LastUpdated";
        cmd.Parameters.AddRange(new[] { pId, pProvider, pProviderGameId, pTitle, pDesc, pPublisher, pDeveloper, pOrig, pCurr, pDisc, pCurrency, pUrl, pImg, pThumb, pStart, pEnd, pFree, pUpcoming, pReviewScore, pReviewCount, pReleaseDate, pGenres, pUpdated });
        var failedIds = new List<string>();
        foreach (var d in deals)
        {
            try
            {
                pId.Value = d.Id;
                pProvider.Value = d.ProviderName;
                pProviderGameId.Value = d.ProviderGameId;
                pTitle.Value = d.Title;
                pDesc.Value = (object?)d.Description ?? DBNull.Value;
                pPublisher.Value = (object?)d.Publisher ?? DBNull.Value;
                pDeveloper.Value = (object?)d.Developer ?? DBNull.Value;
                pOrig.Value = d.OriginalPrice;
                pCurr.Value = d.CurrentPrice;
                pDisc.Value = d.DiscountPercentage;
                pCurrency.Value = d.Currency;
                pUrl.Value = d.StoreUrl;
                pImg.Value = (object?)d.ImageUrl ?? DBNull.Value;
                pThumb.Value = (object?)d.ThumbnailUrl ?? DBNull.Value;
                pStart.Value = (object?)d.StartsAt ?? DBNull.Value;
                pEnd.Value = (object?)d.EndsAt ?? DBNull.Value;
                pFree.Value = d.IsCurrentlyFree ? 1 : 0;
                pUpcoming.Value = d.IsUpcoming ? 1 : 0;
                pReviewScore.Value = (object?)d.ReviewScore ?? DBNull.Value;
                pReviewCount.Value = (object?)d.ReviewCount ?? DBNull.Value;
                pReleaseDate.Value = (object?)d.ReleaseDate ?? DBNull.Value;
                pGenres.Value = (object?)(d.Genres != null ? string.Join(",", d.Genres) : null) ?? DBNull.Value;
                pUpdated.Value = DateTimeOffset.UtcNow.ToString("o");
                await cmd.ExecuteNonQueryAsync(ct);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex)
            {
                // Skip this deal and continue with the rest
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
    }

    public async Task<IReadOnlyList<GameDeal>> GetAllDealsAsync(CancellationToken ct)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Id, ProviderName, ProviderGameId, Title, Description, Publisher, Developer, OriginalPrice, CurrentPrice, DiscountPercentage, Currency, StoreUrl, ImageUrl, ThumbnailUrl, StartsAt, EndsAt, IsCurrentlyFree, IsUpcoming, ReviewScore, ReviewCount, ReleaseDate, Genres, LastUpdated FROM GameDeals;";
        var list = new List<GameDeal>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
        {
            list.Add(new GameDeal(
                Id: reader.GetString(0),
                ProviderGameId: reader.GetString(2),
                ProviderName: reader.GetString(1),
                Title: reader.GetString(3),
                Description: reader.IsDBNull(4) ? null : reader.GetString(4),
                Publisher: reader.IsDBNull(5) ? null : reader.GetString(5),
                Developer: reader.IsDBNull(6) ? null : reader.GetString(6),
                OriginalPrice: reader.GetDecimal(7),
                CurrentPrice: reader.GetDecimal(8),
                DiscountPercentage: reader.GetInt32(9),
                Currency: reader.IsDBNull(10) ? "USD" : reader.GetString(10),
                StoreUrl: reader.GetString(11),
                ImageUrl: reader.IsDBNull(12) ? null : reader.GetString(12),
                ThumbnailUrl: reader.IsDBNull(13) ? null : reader.GetString(13),
                StartsAt: reader.IsDBNull(14) ? null : reader.GetDateTimeOffset(14),
                EndsAt: reader.IsDBNull(15) ? null : reader.GetDateTimeOffset(15),
                IsCurrentlyFree: reader.GetInt32(16) != 0,
                IsUpcoming: reader.GetInt32(17) != 0,
                ReviewScore: reader.IsDBNull(18) ? null : (int?)reader.GetInt32(18),
                ReviewCount: reader.IsDBNull(19) ? null : (int?)reader.GetInt32(19),
                ReleaseDate: reader.IsDBNull(20) ? null : reader.GetDateTimeOffset(20),
                Genres: reader.IsDBNull(21) ? null : reader.GetString(21).Split(',', StringSplitOptions.RemoveEmptyEntries)));
        }
        return list;
    }

    public async Task<bool> NeedsRefreshAsync(TimeSpan refreshInterval, CancellationToken ct)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        var cmd = connection.CreateCommand();
        cmd.CommandText = "SELECT Value FROM Settings WHERE Key = 'LastSuccessfulRefresh';";
        var result = await cmd.ExecuteScalarAsync(ct);
        if (result == null || result == DBNull.Value) return true;
        if (!DateTimeOffset.TryParse(result.ToString(), out var lastRefresh)) return true;
        return DateTimeOffset.UtcNow - lastRefresh > refreshInterval;
    }

    public async Task RecordRefreshAsync(string providerName, bool success, int itemCount, string? errorMessage, CancellationToken ct)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"INSERT INTO RefreshHistory (Id, ProviderName, RefreshedAt, Success, ItemCount, ErrorMessage) VALUES (@Id, @ProviderName, @RefreshedAt, @Success, @ItemCount, @ErrorMessage);";
        var pId = cmd.CreateParameter(); pId.ParameterName = "@Id";
        var pProvider = cmd.CreateParameter(); pProvider.ParameterName = "@ProviderName";
        var pRefreshedAt = cmd.CreateParameter(); pRefreshedAt.ParameterName = "@RefreshedAt";
        var pSuccess = cmd.CreateParameter(); pSuccess.ParameterName = "@Success";
        var pItemCount = cmd.CreateParameter(); pItemCount.ParameterName = "@ItemCount";
        var pErrorMessage = cmd.CreateParameter(); pErrorMessage.ParameterName = "@ErrorMessage";
        cmd.Parameters.AddRange(new[] { pId, pProvider, pRefreshedAt, pSuccess, pItemCount, pErrorMessage });
        pId.Value = Guid.NewGuid().ToString();
        pProvider.Value = providerName;
        pRefreshedAt.Value = DateTimeOffset.UtcNow.ToString("o");
        pSuccess.Value = success ? 1 : 0;
        pItemCount.Value = itemCount;
        pErrorMessage.Value = (object?)errorMessage ?? DBNull.Value;
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task SaveLastRefreshAsync(TimeSpan refreshInterval, CancellationToken ct)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"INSERT INTO Settings (Key, Value) VALUES ('LastSuccessfulRefresh', @Value)
            ON CONFLICT(Key) DO UPDATE SET Value = excluded.Value;";
        var pValue = cmd.CreateParameter();
        pValue.ParameterName = "@Value";
        pValue.Value = DateTimeOffset.UtcNow.ToString("o");
        cmd.Parameters.Add(pValue);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public Task RecordPriceChangeAsync(string dealId, decimal originalPrice, decimal currentPrice, int discountPercentage, string currency, CancellationToken ct)
    {
        var deal = new GameDeal(
            Id: dealId,
            ProviderGameId: string.Empty,
            ProviderName: string.Empty,
            Title: string.Empty,
            Description: null,
            Publisher: null,
            Developer: null,
            OriginalPrice: originalPrice,
            CurrentPrice: currentPrice,
            DiscountPercentage: discountPercentage,
            Currency: currency,
            StoreUrl: string.Empty,
            ImageUrl: null,
            ThumbnailUrl: null,
            StartsAt: null,
            EndsAt: null,
            IsCurrentlyFree: false,
            IsUpcoming: false,
            ReviewScore: null,
            ReviewCount: null,
            ReleaseDate: null,
            Genres: null);
        return RecordPriceChangesAsync(new[] { deal }, ct);
    }

    public async Task RecordPriceChangesAsync(IReadOnlyList<GameDeal> deals, CancellationToken ct)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        var cmd = connection.CreateCommand();
        cmd.Transaction = (SqliteTransaction)transaction;
        cmd.CommandText = @"INSERT INTO DealHistory (Id, DealId, OriginalPrice, CurrentPrice, DiscountPercentage, Currency, RecordedAt) VALUES (@Id, @DealId, @OriginalPrice, @CurrentPrice, @DiscountPercentage, @Currency, @RecordedAt);";
        var pId = cmd.CreateParameter(); pId.ParameterName = "@Id";
        var pDealId = cmd.CreateParameter(); pDealId.ParameterName = "@DealId";
        var pOrig = cmd.CreateParameter(); pOrig.ParameterName = "@OriginalPrice";
        var pCurr = cmd.CreateParameter(); pCurr.ParameterName = "@CurrentPrice";
        var pDisc = cmd.CreateParameter(); pDisc.ParameterName = "@DiscountPercentage";
        var pCurrency = cmd.CreateParameter(); pCurrency.ParameterName = "@Currency";
        var pRecordedAt = cmd.CreateParameter(); pRecordedAt.ParameterName = "@RecordedAt";
        cmd.Parameters.AddRange(new[] { pId, pDealId, pOrig, pCurr, pDisc, pCurrency, pRecordedAt });
        foreach (var d in deals)
        {
            pId.Value = Guid.NewGuid().ToString();
            pDealId.Value = d.Id;
            pOrig.Value = d.OriginalPrice;
            pCurr.Value = d.CurrentPrice;
            pDisc.Value = d.DiscountPercentage;
            pCurrency.Value = d.Currency;
            pRecordedAt.Value = DateTimeOffset.UtcNow.ToString("o");
            await cmd.ExecuteNonQueryAsync(ct);
        }
        await transaction.CommitAsync(ct);
    }

    public async Task DeleteStaleDealsAsync(TimeSpan maxAge, CancellationToken ct)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        var cmd = connection.CreateCommand();
        var cutoff = DateTimeOffset.UtcNow - maxAge;
        cmd.CommandText = "DELETE FROM GameDeals WHERE LastUpdated < @Cutoff;";
        var pCutoff = cmd.CreateParameter();
        pCutoff.ParameterName = "@Cutoff";
        pCutoff.Value = cutoff.ToString("o");
        cmd.Parameters.Add(pCutoff);
        await cmd.ExecuteNonQueryAsync(ct);
    }

    public async Task DeleteDealsNotSeenAsync(IReadOnlyList<string> seenDealIds, CancellationToken ct)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        var cmd = connection.CreateCommand();
        cmd.Transaction = (SqliteTransaction)transaction;

        if (seenDealIds.Count == 0)
        {
            // No deals seen — delete all deals
            cmd.CommandText = "DELETE FROM GameDeals;";
            await cmd.ExecuteNonQueryAsync(ct);
        }
        else
        {
            // Build parameterized NOT IN clause
            var placeholders = new List<string>(seenDealIds.Count);
            for (var i = 0; i < seenDealIds.Count; i++)
            {
                var paramName = $"@Id{i}";
                placeholders.Add(paramName);
                var p = cmd.CreateParameter();
                p.ParameterName = paramName;
                p.Value = seenDealIds[i];
                cmd.Parameters.Add(p);
            }
            cmd.CommandText = $"DELETE FROM GameDeals WHERE Id NOT IN ({string.Join(", ", placeholders)});";
            await cmd.ExecuteNonQueryAsync(ct);
        }

        await transaction.CommitAsync(ct);
    }
}