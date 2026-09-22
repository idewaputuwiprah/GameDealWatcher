namespace GameDealWatcher.Domain.Entities;

/// <summary>
/// Canonical provider name constants used across all layers.
/// Avoids hardcoding "Epic" / "Steam" strings in multiple locations.
/// </summary>
public static class ProviderNames
{
    public const string Epic = "Epic";
    public const string Steam = "Steam";
}

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