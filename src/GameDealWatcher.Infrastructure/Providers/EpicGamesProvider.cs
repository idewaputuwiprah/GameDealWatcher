using System.Globalization;
using System.Text.Json;
using GameDealWatcher.Domain.Entities;
using GameDealWatcher.Domain.Interfaces;
using GameDealWatcher.Infrastructure.Http;

namespace GameDealWatcher.Infrastructure.Providers;

public sealed class EpicGamesProvider : IGameDealProvider
{
    private readonly IHttpClientFactory _httpClientFactory;

    public string ProviderName => ProviderNames.Epic;

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

        // Navigate: data.Catalog.searchStore.elements[]
        if (!root.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Object) return deals;
        if (!data.TryGetProperty("Catalog", out var catalog) || catalog.ValueKind != JsonValueKind.Object) return deals;
        if (!catalog.TryGetProperty("searchStore", out var searchStore) || searchStore.ValueKind != JsonValueKind.Object) return deals;
        if (!searchStore.TryGetProperty("elements", out var elements) || elements.ValueKind != JsonValueKind.Array) return deals;

        var now = DateTimeOffset.UtcNow;

        foreach (var entry in elements.EnumerateArray())
        {
            var deal = ParseEpicElement(entry, now);
            if (deal != null) deals.Add(deal);
        }
        return deals;
    }

    public static GameDeal? ParseEpicElement(JsonElement entry, DateTimeOffset now)
    {
        try
        {
            // Filter: only include base games, skip DLCs, soundtracks, bundles
            if (entry.TryGetProperty("offerType", out var offerType) && offerType.ValueKind == JsonValueKind.String)
            {
                var offerTypeValue = offerType.GetString();
                if (offerTypeValue != null && offerTypeValue != "BASEGAME") return null;
            }

            var title = entry.TryGetProperty("title", out var titleProp) ? titleProp.GetString() ?? "Unknown" : "Unknown";

            // Epic uses "id" — deterministic fallback if missing (SHA256 of title)
            var offerId = entry.TryGetProperty("id", out var idProp) ? idProp.GetString() : null;
            if (string.IsNullOrEmpty(offerId))
            {
                var hashBytes = System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(title));
                offerId = $"unknown_{Convert.ToHexString(hashBytes)[..8]}";
            }
            var dealId = $"epic_{offerId}";

            // Images
            string? imgUrl = null;
            string? thumbUrl = null;
            if (entry.TryGetProperty("keyImages", out var images) && images.ValueKind == JsonValueKind.Array)
            {
                imgUrl = GetImageUrl(images, "OfferImageWide") ?? GetImageUrl(images, "VaultClosed");
                thumbUrl = GetImageUrl(images, "Thumbnail");
            }

            // Store URL — multi-level fallback
            var storeUrl = BuildStoreUrl(entry, offerId);

            // Price data — Epic stores prices in cents
            var originalPrice = 0m;
            var currentPrice = 0m;
            var discountPct = 0;
            if (entry.TryGetProperty("price", out var price) && price.ValueKind == JsonValueKind.Object)
            {
                if (price.TryGetProperty("totalPrice", out var total) && total.ValueKind == JsonValueKind.Object)
                {
                    // Convert cents to dollars
                    originalPrice = total.TryGetProperty("originalPrice", out var op) ? op.GetDecimal() / 100m : 0m;
                    currentPrice = total.TryGetProperty("discountPrice", out var dp) ? dp.GetDecimal() / 100m : 0m;
                    // Compute discount percentage from dollar values
                    if (originalPrice > 0)
                        discountPct = (int)Math.Round((1 - currentPrice / originalPrice) * 100);
                }
            }

            // Promotional offers — nested structure:
            // promotions.promotionalOffers[].promotionalOffers[] (active)
            // promotions.upcomingPromotionalOffers[].promotionalOffers[] (upcoming)
            DateTimeOffset? activeStartsAt = null;
            DateTimeOffset? activeEndsAt = null;
            DateTimeOffset? upcomingStartsAt = null;
            DateTimeOffset? upcomingEndsAt = null;
            var hasActiveOffer = false;
            var hasUpcomingOffer = false;

            if (entry.TryGetProperty("promotions", out var promotions) && promotions.ValueKind == JsonValueKind.Object)
            {
                if (promotions.TryGetProperty("promotionalOffers", out var active) && active.ValueKind == JsonValueKind.Array && active.GetArrayLength() > 0)
                {
                    var activeOffer = active[0];
                    if (activeOffer.TryGetProperty("promotionalOffers", out var innerActive) && innerActive.ValueKind == JsonValueKind.Array && innerActive.GetArrayLength() > 0)
                    {
                        TryParseDateRange(innerActive[0], out activeStartsAt, out activeEndsAt);
                        hasActiveOffer = true;
                    }
                }

                // Always parse upcoming dates — not gated by !hasActiveOffer
                if (promotions.TryGetProperty("upcomingPromotionalOffers", out var upcoming) && upcoming.ValueKind == JsonValueKind.Array && upcoming.GetArrayLength() > 0)
                {
                    var upcomingOffer = upcoming[0];
                    if (upcomingOffer.TryGetProperty("promotionalOffers", out var innerUpcoming) && innerUpcoming.ValueKind == JsonValueKind.Array && innerUpcoming.GetArrayLength() > 0)
                    {
                        TryParseDateRange(innerUpcoming[0], out upcomingStartsAt, out upcomingEndsAt);
                        hasUpcomingOffer = true;
                    }
                }
            }

            // Determine free/upcoming status
            var isCurrentlyFree = hasActiveOffer && activeStartsAt.HasValue && activeEndsAt.HasValue
                ? now >= activeStartsAt && now < activeEndsAt
                : (originalPrice == 0 && currentPrice == 0);
            var isUpcoming = hasUpcomingOffer && !isCurrentlyFree
                && upcomingStartsAt.HasValue && now < upcomingStartsAt;

            // Select which dates to return
            DateTimeOffset? startsAt;
            DateTimeOffset? endsAt;
            if (isCurrentlyFree)
            {
                startsAt = activeStartsAt;
                endsAt = activeEndsAt;
            }
            else if (isUpcoming)
            {
                startsAt = upcomingStartsAt;
                endsAt = upcomingEndsAt;
            }
            else
            {
                startsAt = activeStartsAt ?? upcomingStartsAt;
                endsAt = activeEndsAt ?? upcomingEndsAt;
            }

            return new GameDeal(
                Id: dealId,
                ProviderGameId: offerId,
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
                ImageUrl: imgUrl,
                ThumbnailUrl: thumbUrl,
                StartsAt: startsAt,
                EndsAt: endsAt,
                IsCurrentlyFree: isCurrentlyFree,
                IsUpcoming: isUpcoming,
                ReviewScore: null,
                ReviewCount: null,
                ReleaseDate: null,
                Genres: null);
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to parse Epic element: {ex.Message}");
            return null;
        }
    }

    private static void TryParseDateRange(JsonElement inner, out DateTimeOffset? start, out DateTimeOffset? end)
    {
        start = inner.TryGetProperty("startDate", out var sd) && sd.ValueKind == JsonValueKind.String
            && DateTimeOffset.TryParse(sd.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var startDate)
                ? startDate : null;
        end = inner.TryGetProperty("endDate", out var ed) && ed.ValueKind == JsonValueKind.String
            && DateTimeOffset.TryParse(ed.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var endDate)
                ? endDate : null;
    }

    private static string BuildStoreUrl(JsonElement entry, string offerId)
    {
        if (entry.TryGetProperty("productSlug", out var slug) && slug.ValueKind == JsonValueKind.String)
        {
            var slugStr = slug.GetString();
            if (!string.IsNullOrEmpty(slugStr))
                return $"https://www.epicgames.com/store/en-US/p/{slugStr}";
        }

        if (entry.TryGetProperty("catalogNs", out var catalogNs) && catalogNs.ValueKind == JsonValueKind.Object)
        {
            if (catalogNs.TryGetProperty("mappings", out var mappings) && mappings.ValueKind == JsonValueKind.Array && mappings.GetArrayLength() > 0)
            {
                var mapping = mappings[0];
                if (mapping.TryGetProperty("pageSlug", out var pageSlug) && pageSlug.ValueKind == JsonValueKind.String)
                {
                    var slugStr = pageSlug.GetString();
                    if (!string.IsNullOrEmpty(slugStr))
                        return $"https://www.epicgames.com/store/en-US/p/{slugStr}";
                }
            }
        }

        if (entry.TryGetProperty("url", out var url) && url.ValueKind == JsonValueKind.String)
        {
            var urlStr = url.GetString();
            if (!string.IsNullOrEmpty(urlStr)) return urlStr;
        }

        return $"https://www.epicgames.com/store/en-US/p/{offerId}";
    }

    private static string? GetImageUrl(JsonElement images, string type)
    {
        foreach (var img in images.EnumerateArray())
        {
            if (img.TryGetProperty("type", out var t) && t.ValueKind == JsonValueKind.String && t.GetString() == type)
                return img.TryGetProperty("url", out var u) ? u.GetString() : null;
        }
        return null;
    }
}
