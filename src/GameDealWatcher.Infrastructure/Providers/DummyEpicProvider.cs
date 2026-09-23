using GameDealWatcher.Domain.Entities;
using GameDealWatcher.Domain.Interfaces;

namespace GameDealWatcher.Infrastructure.Providers;

/// <summary>
/// Dummy provider that returns hardcoded Epic Games deals for development/testing.
/// No network calls — always returns the same data.
/// </summary>
public sealed class DummyEpicProvider : IGameDealProvider
{
    public string ProviderName => ProviderNames.Epic;

    public Task<IReadOnlyList<GameDeal>> GetDealsAsync(CancellationToken ct)
    {
        var now = DateTimeOffset.UtcNow;

        var deals = new List<GameDeal>
        {
            // Currently free (active promotion)
            CreateDeal(
                "epic_death_stranding", "death-stranding", "Death Stranding",
                39.99m, 0m, 100,
                "https://www.epicgames.com/store/en-US/p/death-stranding",
                "https://cdn1.epicgames.com/offer/death-stranding/offer.jpg",
                startsAt: now.AddDays(-3),
                endsAt: now.AddDays(4),
                isCurrentlyFree: true,
                isUpcoming: false),

            // Upcoming free game
            CreateDeal(
                "epic_epic_games_store", "epic-games-store", "Epic Games Store",
                0m, 0m, 0,
                "https://www.epicgames.com/store/en-US/",
                null,
                startsAt: now.AddDays(4),
                endsAt: now.AddDays(11),
                isCurrentlyFree: false,
                isUpcoming: true),

            // Discounted game
            CreateDeal(
                "epic_control", "control", "Control Ultimate Edition",
                39.99m, 9.99m, 75,
                "https://www.epicgames.com/store/en-US/p/control",
                "https://cdn1.epicgames.com/offer/control/offer.jpg",
                startsAt: now.AddDays(-2),
                endsAt: now.AddDays(5),
                isCurrentlyFree: false,
                isUpcoming: false),

            // Always free
            CreateDeal(
                "epic_fortnite", "fortnite", "Fortnite",
                0m, 0m, 0,
                "https://www.epicgames.com/store/en-US/p/fortnite",
                "https://cdn1.epicgames.com/offer/fortnite/offer.jpg",
                startsAt: null,
                endsAt: null,
                isCurrentlyFree: true,
                isUpcoming: false),

            // Another discounted game
            CreateDeal(
                "epic_gta_v", "grand-theft-auto-v", "Grand Theft Auto V: Premium Edition",
                29.99m, 7.49m, 75,
                "https://www.epicgames.com/store/en-US/p/grand-theft-auto-v",
                "https://cdn1.epicgames.com/offer/gta-v/offer.jpg",
                startsAt: now.AddDays(-1),
                endsAt: now.AddDays(6),
                isCurrentlyFree: false,
                isUpcoming: false),
        };

        return Task.FromResult<IReadOnlyList<GameDeal>>(deals);
    }

    private static GameDeal CreateDeal(
        string id, string slug, string title,
        decimal originalPrice, decimal currentPrice, int discountPct,
        string storeUrl, string? imageUrl,
        DateTimeOffset? startsAt, DateTimeOffset? endsAt,
        bool isCurrentlyFree, bool isUpcoming) =>
        new(
            Id: id,
            ProviderGameId: slug,
            ProviderName: ProviderNames.Epic,
            Title: title,
            Description: null,
            Publisher: null,
            Developer: null,
            OriginalPrice: originalPrice,
            CurrentPrice: currentPrice,
            DiscountPercentage: discountPct,
            Currency: "USD",
            StoreUrl: storeUrl,
            ImageUrl: imageUrl,
            ThumbnailUrl: imageUrl,
            StartsAt: startsAt,
            EndsAt: endsAt,
            IsCurrentlyFree: isCurrentlyFree,
            IsUpcoming: isUpcoming,
            ReviewScore: null,
            ReviewCount: null,
            ReleaseDate: null,
            Genres: null);
}
