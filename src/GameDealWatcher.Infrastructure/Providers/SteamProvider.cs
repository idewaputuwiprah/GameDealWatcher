using System.Net.Http.Json;
using System.Text.Json;
using GameDealWatcher.Domain.Entities;
using GameDealWatcher.Domain.Interfaces;
using GameDealWatcher.Infrastructure.Http;

namespace GameDealWatcher.Infrastructure.Providers;

public sealed class SteamProvider : IGameDealProvider
{
    private readonly IHttpClientFactory _httpClientFactory;

    public string ProviderName => "Steam";

    public SteamProvider(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IReadOnlyList<GameDeal>> GetDealsAsync(CancellationToken ct)
    {
        var client = _httpClientFactory.CreateClient(HttpClientConfiguration.SteamHandlerName);
        var response = await client.GetAsync("api/featuredcategories/?cc=us&l=english", ct);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct);
        var doc = JsonDocument.Parse(json);
        var deals = new List<GameDeal>();
        var root = doc.RootElement;
        if (root.TryGetProperty("specials", out var specials) && specials.ValueKind == JsonValueKind.Object)
        {
            foreach (var category in specials.EnumerateObject())
            {
                if (category.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in category.Value.EnumerateArray())
                    {
                        var deal = ParseSteamDeal(item);
                        if (deal != null) deals.Add(deal);
                    }
                }
            }
        }
        return deals;
    }

    public static GameDeal? ParseSteamDeal(JsonElement item)
    {
        try
        {
            var appId = item.GetProperty("id").GetInt32();
            var name = item.GetProperty("name").GetString() ?? "Unknown";
            var finalPrice = item.GetProperty("final").GetDecimal() / 100m;
            var originalPrice = item.GetProperty("final2").GetDecimal() / 100m;
            var discountPct = item.TryGetProperty("discount_pct", out var dp) ? dp.GetInt32() : 0;
            var imgUrl = item.TryGetProperty("img", out var img) ? img.GetString() : null;
            var storeUrl = $"https://store.steampowered.com/app/{appId}/";
            var dealId = $"steam_{appId}";
            return new GameDeal(
                Id: dealId,
                ProviderGameId: appId.ToString(),
                ProviderName: "Steam",
                Title: name,
                Description: null,
                Publisher: null,
                Developer: null,
                OriginalPrice: originalPrice,
                CurrentPrice: finalPrice,
                DiscountPercentage: discountPct,
                Currency: "USD",
                StoreUrl: storeUrl,
                ImageUrl: imgUrl,
                ThumbnailUrl: imgUrl,
                StartsAt: null,
                EndsAt: null,
                IsCurrentlyFree: false,
                IsUpcoming: false,
                ReviewScore: null,
                ReviewCount: null,
                ReleaseDate: null,
                Genres: null);
        }
        catch (Exception)
        {
            return null;
        }
    }
}