using System.Text.Json;
using GameDealWatcher.Domain.Entities;
using GameDealWatcher.Domain.Interfaces;
using GameDealWatcher.Infrastructure.Http;
using Microsoft.Extensions.Logging;

namespace GameDealWatcher.Infrastructure.Providers;

public sealed class SteamProvider : IGameDealProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISettingsRepository _settingsRepository;
    private readonly ILogger<SteamProvider> _logger;

    public string ProviderName => ProviderNames.Steam;

    public SteamProvider(IHttpClientFactory httpClientFactory, ISettingsRepository settingsRepository, ILogger<SteamProvider> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settingsRepository = settingsRepository;
        _logger = logger;
    }

    public async Task<IReadOnlyList<GameDeal>> GetDealsAsync(CancellationToken ct)
    {
        try
        {
            var settings = await _settingsRepository.GetSettingsAsync(ct);
            var region = RegionCatalog.ByCode(settings.SelectedRegionCode) ?? RegionCatalog.Default;

            var client = _httpClientFactory.CreateClient(HttpClientConfiguration.SteamHandlerName);
            var url = $"api/featuredcategories/?cc={region.Code}&l={region.SteamLocale}";
            _logger.LogDebug("Steam API request: {Url}", url);
            var response = await client.GetAsync(url, ct);
            response.EnsureSuccessStatusCode();
            var json = await response.Content.ReadAsStringAsync(ct);
            var doc = JsonDocument.Parse(json);
            var deals = new List<GameDeal>();
            var root = doc.RootElement;

            // Read currency from API response (Steam returns it at root level)
            var currency = root.TryGetProperty("currency", out var curProp) && curProp.ValueKind == JsonValueKind.String
                ? curProp.GetString() ?? region.CurrencyCode
                : region.CurrencyCode;

            // Steam API structure: root.specials.items[]
            if (!root.TryGetProperty("specials", out var specials) || specials.ValueKind != JsonValueKind.Object)
                return deals;
            if (!specials.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
                return deals;

            foreach (var item in items.EnumerateArray())
            {
                var deal = ParseSteamDeal(item, currency);
                if (deal != null) deals.Add(deal);
            }
            _logger.LogInformation("Steam API returned {Count} deals", deals.Count);
            return deals;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SteamProvider.GetDealsAsync failed");
            throw;
        }
    }

    public static GameDeal? ParseSteamDeal(JsonElement item, string currency)
    {
        try
        {
            // App ID — required field
            if (!item.TryGetProperty("id", out var idProp)) return null;
            var appId = idProp.GetInt32();

            // Name — required field
            var name = item.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? "Unknown" : "Unknown";

            // Prices — Steam stores prices in cents (integers)
            // Parse discountPct first, then derive originalPrice if missing
            var discountPct = item.TryGetProperty("discount_percent", out var dp) ? dp.GetInt32() : 0;
            var finalPrice = item.TryGetProperty("final_price", out var fp) ? fp.GetDecimal() / 100m : 0m;
            var originalPrice = item.TryGetProperty("original_price", out var op)
                ? op.GetDecimal() / 100m
                : (discountPct > 0 && discountPct < 100
                    ? Math.Round(finalPrice * 100m / (100m - discountPct), 2)
                    : finalPrice);

            // Image — prefer header_image, fall back to large_capsule_image
            var imgUrl = item.TryGetProperty("header_image", out var img) ? img.GetString() : null;
            if (string.IsNullOrEmpty(imgUrl))
                imgUrl = item.TryGetProperty("large_capsule_image", out var lci) ? lci.GetString() : null;

            var storeUrl = $"https://store.steampowered.com/app/{appId}/";
            var dealId = $"steam_{appId}";

            return new GameDeal(
                Id: dealId,
                ProviderGameId: appId.ToString(),
                ProviderName: ProviderNames.Steam,
                Title: name,
                Description: null,
                Publisher: null,
                Developer: null,
                OriginalPrice: originalPrice,
                CurrentPrice: finalPrice,
                DiscountPercentage: discountPct,
                Currency: currency,
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
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to parse Steam deal: {ex.Message}");
            return null;
        }
    }
}