using GameDealWatcher.Domain.Entities;
using GameDealWatcher.Domain.Interfaces;

namespace GameDealWatcher.Infrastructure.Providers;

/// <summary>
/// Dummy provider that returns hardcoded Steam deals for development/testing.
/// No network calls — always returns the same data.
/// </summary>
public sealed class DummySteamProvider : IGameDealProvider
{
    public string ProviderName => ProviderNames.Steam;

    public Task<IReadOnlyList<GameDeal>> GetDealsAsync(CancellationToken ct)
    {
        var deals = new List<GameDeal>
        {
            CreateDeal("steam_730", "730", "Counter-Strike 2", 14.99m, 0m, 100,
                "https://store.steampowered.com/app/730/",
                "https://cdn.cloudflare.steamstatic.com/steam/apps/730/header.jpg"),
            CreateDeal("steam_1245620", "1245620", "ELDEN RING", 59.99m, 29.99m, 50,
                "https://store.steampowered.com/app/1245620/",
                "https://cdn.cloudflare.steamstatic.com/steam/apps/1245620/header.jpg"),
            CreateDeal("steam_292030", "292030", "The Witcher 3: Wild Hunt", 39.99m, 9.99m, 75,
                "https://store.steampowered.com/app/292030/",
                "https://cdn.cloudflare.steamstatic.com/steam/apps/292030/header.jpg"),
            CreateDeal("steam_1174180", "1174180", "Red Dead Redemption 2", 59.99m, 14.99m, 75,
                "https://store.steampowered.com/app/1174180/",
                "https://cdn.cloudflare.steamstatic.com/steam/apps/1174180/header.jpg"),
            CreateDeal("steam_1091500", "1091500", "Cyberpunk 2077", 59.99m, 29.99m, 50,
                "https://store.steampowered.com/app/1091500/",
                "https://cdn.cloudflare.steamstatic.com/steam/apps/1091500/header.jpg"),
            CreateDeal("steam_271590", "271590", "Grand Theft Auto V", 29.99m, 9.89m, 67,
                "https://store.steampowered.com/app/271590/",
                "https://cdn.cloudflare.steamstatic.com/steam/apps/271590/header.jpg"),
            CreateDeal("steam_1599340", "1599340", "Hogwarts Legacy", 59.99m, 35.99m, 40,
                "https://store.steampowered.com/app/1599340/",
                "https://cdn.cloudflare.steamstatic.com/steam/apps/1599340/header.jpg"),
            CreateDeal("steam_1086940", "1086940", "Baldur's Gate 3", 59.99m, 47.99m, 20,
                "https://store.steampowered.com/app/1086940/",
                "https://cdn.cloudflare.steamstatic.com/steam/apps/1086940/header.jpg"),
            CreateDeal("steam_427120", "427120", "Factorio", 35.00m, 24.50m, 30,
                "https://store.steampowered.com/app/427120/",
                "https://cdn.cloudflare.steamstatic.com/steam/apps/427120/header.jpg"),
            CreateDeal("steam_1145360", "1145360", "Hades", 24.99m, 12.49m, 50,
                "https://store.steampowered.com/app/1145360/",
                "https://cdn.cloudflare.steamstatic.com/steam/apps/1145360/header.jpg"),
        };

        return Task.FromResult<IReadOnlyList<GameDeal>>(deals);
    }

    private static GameDeal CreateDeal(string id, string providerGameId, string title,
        decimal originalPrice, decimal currentPrice, int discountPct,
        string storeUrl, string imageUrl) =>
        new(
            Id: id,
            ProviderGameId: providerGameId,
            ProviderName: ProviderNames.Steam,
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
            StartsAt: null,
            EndsAt: null,
            IsCurrentlyFree: false,
            IsUpcoming: false,
            ReviewScore: null,
            ReviewCount: null,
            ReleaseDate: null,
            Genres: null);
}
