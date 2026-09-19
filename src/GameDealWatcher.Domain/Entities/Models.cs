namespace GameDealWatcher.Domain.Entities;

public sealed record Game(string Id, string Title, string? Publisher, string? Developer);

public sealed record GameDeal(
    string Id,
    string ProviderGameId,
    string ProviderName,
    string Title,
    string? Description,
    string? Publisher,
    string? Developer,
    decimal OriginalPrice,
    decimal CurrentPrice,
    int DiscountPercentage,
    string Currency,
    string StoreUrl,
    string? ImageUrl,
    string? ThumbnailUrl,
    DateTimeOffset? StartsAt,
    DateTimeOffset? EndsAt,
    bool IsCurrentlyFree,
    bool IsUpcoming,
    int? ReviewScore,
    int? ReviewCount,
    DateTimeOffset? ReleaseDate,
    string[]? Genres);

public sealed record DealHistoryEntry(
    string DealId,
    decimal OriginalPrice,
    decimal CurrentPrice,
    int DiscountPercentage,
    DateTimeOffset RecordedAt);