using GameDealWatcher.Domain.Entities;
using Microsoft.Data.Sqlite;

namespace GameDealWatcher.Infrastructure.Database;

public interface IGameDealRepository
{
    Task InsertDealsAsync(IReadOnlyList<GameDeal> deals, CancellationToken ct);
    Task<IReadOnlyList<GameDeal>> GetAllDealsAsync(CancellationToken ct);
    Task<bool> NeedsRefreshAsync(TimeSpan refreshInterval, CancellationToken ct);
    Task RecordRefreshAsync(string providerName, bool success, int itemCount, string? errorMessage, CancellationToken ct);
    Task RecordPriceChangeAsync(string dealId, decimal originalPrice, decimal currentPrice, int discountPercentage, string currency, CancellationToken ct);
    Task SaveLastRefreshAsync(TimeSpan refreshInterval, CancellationToken ct);
}

public sealed class SqliteGameDealRepository : IGameDealRepository
{
    private readonly string _connectionString;

    public SqliteGameDealRepository(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task InsertDealsAsync(IReadOnlyList<GameDeal> deals, CancellationToken ct)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);
        var cmd = connection.CreateCommand();
        cmd.Transaction = (SqliteTransaction)transaction;
        cmd.CommandText = @"INSERT OR IGNORE INTO GameDeals (Id, ProviderName, ProviderGameId, Title, Description, Publisher, Developer, OriginalPrice, CurrentPrice, DiscountPercentage, Currency, StoreUrl, ImageUrl, ThumbnailUrl, StartsAt, EndsAt, IsCurrentlyFree, IsUpcoming, ReviewScore, ReviewCount, ReleaseDate, Genres, LastUpdated) VALUES (@Id, @ProviderName, @ProviderGameId, @Title, @Description, @Publisher, @Developer, @OriginalPrice, @CurrentPrice, @DiscountPercentage, @Currency, @StoreUrl, @ImageUrl, @ThumbnailUrl, @StartsAt, @EndsAt, @IsCurrentlyFree, @IsUpcoming, @ReviewScore, @ReviewCount, @ReleaseDate, @Genres, @LastUpdated);";
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
        foreach (var d in deals)
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
            pGenres.Value = (object?)string.Join(",", d.Genres ?? []) ?? DBNull.Value;
            pUpdated.Value = DateTimeOffset.UtcNow.ToString("o");
            await cmd.ExecuteNonQueryAsync(ct);
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
        var lastRefresh = DateTimeOffset.Parse(result.ToString()!);
        return DateTimeOffset.UtcNow - lastRefresh > refreshInterval;
    }

    public async Task RecordRefreshAsync(string providerName, bool success, int itemCount, string? errorMessage, CancellationToken ct)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"INSERT INTO RefreshHistory (Id, ProviderName, RefreshedAt, Success, ItemCount, ErrorMessage) VALUES (@Id, @ProviderName, @RefreshedAt, @Success, @ItemCount, @ErrorMessage);";
        var pId = cmd.CreateParameter();
        var pProvider = cmd.CreateParameter();
        var pRefreshedAt = cmd.CreateParameter();
        var pSuccess = cmd.CreateParameter();
        var pItemCount = cmd.CreateParameter();
        var pErrorMessage = cmd.CreateParameter();
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

    public async Task RecordPriceChangeAsync(string dealId, decimal originalPrice, decimal currentPrice, int discountPercentage, string currency, CancellationToken ct)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct);
        var cmd = connection.CreateCommand();
        cmd.CommandText = @"INSERT OR IGNORE INTO DealHistory (Id, DealId, OriginalPrice, CurrentPrice, DiscountPercentage, Currency, RecordedAt) VALUES (@Id, @DealId, @OriginalPrice, @CurrentPrice, @DiscountPercentage, @Currency, @RecordedAt);";
        var pId = cmd.CreateParameter();
        var pDealId = cmd.CreateParameter();
        var pOrig = cmd.CreateParameter();
        var pCurr = cmd.CreateParameter();
        var pDisc = cmd.CreateParameter();
        var pCurrency = cmd.CreateParameter();
        var pRecordedAt = cmd.CreateParameter();
        cmd.Parameters.AddRange(new[] { pId, pDealId, pOrig, pCurr, pDisc, pCurrency, pRecordedAt });
        pId.Value = Guid.NewGuid().ToString();
        pDealId.Value = dealId;
        pOrig.Value = originalPrice;
        pCurr.Value = currentPrice;
        pDisc.Value = discountPercentage;
        pCurrency.Value = currency;
        pRecordedAt.Value = DateTimeOffset.UtcNow.ToString("o");
        await cmd.ExecuteNonQueryAsync(ct);
    }
}