using System.Net.Http.Json;
using System.Text.Json;
using GameDealWatcher.Domain.Entities;
using GameDealWatcher.Domain.Interfaces;
using GameDealWatcher.Infrastructure.Http;

namespace GameDealWatcher.Infrastructure.Providers;

public sealed class EpicGamesProvider : IGameDealProvider
{
    private readonly IHttpClientFactory _httpClientFactory;

    public string ProviderName => "Epic";

    public EpicGamesProvider(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IReadOnlyList<GameDeal>> GetDealsAsync(CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient(HttpClientConfiguration.EpicHandlerName);
        var response = await client.GetAsync("freeGamesPromotions?locale=en-US&country=US&allowCountries=US", ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        var doc = JsonDocument.Parse(json);
        var deals = new List<GameDeal>();
        var root = doc.RootElement;
        if (!root.TryGetProperty("data", out var data) || data.ValueKind == JsonValueKind.Null) return deals;
        var now = DateTimeOffset.UtcNow;
        foreach (var entry in data.EnumerateArray())
        {
            var title = entry.GetProperty("title").GetString() ?? "Unknown";
            var offerId = entry.GetProperty("offerId").GetString() ?? Guid.NewGuid().ToString();
            var dealId = $"epic_{offerId}";
            var imgUrl = entry.TryGetProperty("keyImages", out var images) && images.ValueKind == JsonValueKind.Array
                ? GetImageUrl(images, "OfferImageWide")
                : null;
            var thumbUrl = entry.TryGetProperty("keyImages", out images) && images.ValueKind == JsonValueKind.Array
                ? GetImageUrl(images, "Thumbnail")
                : null;
            var storeUrl = entry.TryGetProperty("url", out var url) ? url.GetString() : $"https://www.epicgames.com/store/en-US/product/{offerId}";
            if (string.IsNullOrEmpty(storeUrl)) storeUrl = $"https://www.epicgames.com/store/en-US/product/{offerId}";
            var currentPrice = 0m;
            var originalPrice = 0m;
            var discountPct = 0;
            var isFree = true;
            var startsAt = DateTimeOffset.MinValue;
            var endsAt = DateTimeOffset.MinValue;
            if (entry.TryGetProperty("promotionalOffers", out var offers) && offers.ValueKind == JsonValueKind.Array && offers.GetArrayLength() > 0)
            {
                var offer = offers[0];
                if (offer.TryGetProperty("price", out var priceObj))
                {
                    currentPrice = priceObj.TryGetProperty("finalPrice", out var fp) ? fp.GetDecimal() / 100m : 0m;
                    originalPrice = priceObj.TryGetProperty("linePrice", out var lp) ? lp.GetDecimal() / 100m : 0m;
                }
                if (offer.TryGetProperty("startDate", out var sd))
                    startsAt = DateTimeOffset.Parse(sd.GetString()!);
                if (offer.TryGetProperty("endDate", out var ed))
                    endsAt = DateTimeOffset.Parse(ed.GetString()!);
                if (offer.TryGetProperty("discount", out var disc))
                    discountPct = disc.GetInt32();
            }
            if (entry.TryGetProperty("upcomingPromotionalOffers", out var upcoming) && upcoming.ValueKind == JsonValueKind.Array && upcoming.GetArrayLength() > 0)
            {
                var uoffer = upcoming[0];
                if (uoffer.TryGetProperty("startDate", out var sd))
                    startsAt = DateTimeOffset.Parse(sd.GetString()!);
                if (uoffer.TryGetProperty("endDate", out var ed))
                    endsAt = DateTimeOffset.Parse(ed.GetString()!);
                isFree = false;
            }
            var deal = new GameDeal(
                Id: dealId,
                ProviderGameId: offerId,
                ProviderName: "Epic",
                Title: title,
                Description: null,
                Publisher: null,
                Developer: null,
                OriginalPrice: originalPrice,
                CurrentPrice: currentPrice,
                DiscountPercentage: discountPct,
                Currency: "USD",
                StoreUrl: storeUrl,
                ImageUrl: imgUrl,
                ThumbnailUrl: thumbUrl,
                StartsAt: startsAt == DateTimeOffset.MinValue ? null : startsAt,
                EndsAt: endsAt == DateTimeOffset.MinValue ? null : endsAt,
                IsCurrentlyFree: isFree,
                IsUpcoming: !isFree && now < startsAt,
                ReviewScore: null,
                ReviewCount: null,
                ReleaseDate: null,
                Genres: null);
            deals.Add(deal);
        }
        return deals;
    }

    private static string? GetImageUrl(JsonElement images, string type)
    {
        foreach (var img in images.EnumerateArray())
        {
            if (img.TryGetProperty("type", out var t) && t.GetString() == type)
                return img.TryGetProperty("url", out var u) ? u.GetString() : null;
        }
        return null;
    }
}