using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using GameDealWatcher.Domain.Entities;
using GameDealWatcher.Domain.Formatting;
using GameDealWatcher.Domain.Interfaces;
using GameDealWatcher.Infrastructure.Http;
using Microsoft.Extensions.Logging;

namespace GameDealWatcher.Infrastructure.Providers;

public sealed class SteamProvider : IGameDealProvider
{
    // Steam's featuredcategories endpoint only exposes ~10 curated specials, so the full
    // discount catalogue comes from paging the store search (100 rows per page).
    // shortcut: caps at 1000 relevance-ranked deals out of ~20k; page further if needed.
    private const int PageSize = 100;
    private const int MaxPages = 10;

    private static readonly Regex RowRegex = new(
        """(?s)<a href="https://store\.steampowered\.com/app/(\d+)/.*?</a>""",
        RegexOptions.Compiled);

    // Counts every result row (apps, bundles, packages) so an all-bundle page is not mistaken for an empty one.
    private static readonly Regex SearchRowRegex = new("""class="search_result_row""", RegexOptions.Compiled);

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
            var deals = new List<GameDeal>();
            var totalCount = -1;

            for (var page = 0; page < MaxPages; page++)
            {
                var start = page * PageSize;
                var url = $"search/results/?query&start={start}&count={PageSize}&specials=1&infinite=1&cc={region.Code}&l={region.SteamLocale}";
                _logger.LogDebug("Steam API request: {Url}", url);
                var response = await client.GetAsync(url, ct);
                response.EnsureSuccessStatusCode();
                var json = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("success", out var success) || success.ValueKind != JsonValueKind.Number || success.GetInt32() != 1)
                    throw new InvalidOperationException("Steam search response did not report success");

                if (totalCount < 0)
                {
                    if (!root.TryGetProperty("total_count", out var total) || !total.TryGetInt32(out totalCount))
                        throw new InvalidOperationException("Steam search response missing total_count");
                }
                if (totalCount == 0)
                    throw new InvalidOperationException("Steam search returned zero deals");

                if (!root.TryGetProperty("results_html", out var html) || html.ValueKind != JsonValueKind.String)
                    throw new InvalidOperationException($"Steam search page start={start} missing results_html");

                var resultsHtml = html.GetString()!;
                if (SearchRowRegex.Matches(resultsHtml).Count == 0)
                    throw new InvalidOperationException($"Steam search page start={start} returned no rows (total {totalCount})");

                var pageDeals = ParseSearchResults(resultsHtml, region.CurrencyCode);
                if (pageDeals.Count == 0 && RowRegex.IsMatch(resultsHtml))
                    throw new InvalidOperationException($"Steam search page start={start} contained app rows but no parseable deals");
                deals.AddRange(pageDeals);

                // A page can be all bundles and yield no app deals; only stop at the real end.
                if (start + PageSize >= totalCount) break;
            }

            var uniqueDeals = deals.DistinctBy(d => d.Id).ToList();
            _logger.LogInformation("Steam API returned {Count} deals", uniqueDeals.Count);
            return uniqueDeals;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SteamProvider.GetDealsAsync failed");
            throw;
        }
    }

    public static IReadOnlyList<GameDeal> ParseSearchResults(string resultsHtml, string currency)
    {
        var deals = new List<GameDeal>();
        foreach (Match row in RowRegex.Matches(resultsHtml))
        {
            var deal = ParseSearchRow(row, currency);
            if (deal != null) deals.Add(deal);
        }
        return deals;
    }

    private static GameDeal? ParseSearchRow(Match row, string currency)
    {
        try
        {
            var html = row.Value;
            var title = Regex.Match(html, @"<span class=""title"">(.*?)</span>");
            var price = Regex.Match(html, @"data-price-final=""(\d+)""");
            if (!title.Success || !price.Success) return null;

            var discountMatch = Regex.Match(html, @"data-discount=""(\d+)""");
            var imageMatch = Regex.Match(html, @"<img src=""([^""]+)""");
            var originalMatch = Regex.Match(html, @"discount_original_price"">([^<]+)<");

            // Steam reports prices in hundredths regardless of the currency's minor unit.
            var discountPct = discountMatch.Success ? int.Parse(discountMatch.Groups[1].Value, CultureInfo.InvariantCulture) : 0;
            var finalPrice = decimal.Parse(price.Groups[1].Value, CultureInfo.InvariantCulture) / 100m;
            var originalPrice = discountPct is > 0 and < 100
                ? Math.Round(finalPrice * 100m / (100m - discountPct), 2)
                : finalPrice;
            if (originalMatch.Success
                && PriceFormatter.TryParse(WebUtility.HtmlDecode(originalMatch.Groups[1].Value), currency, out var parsedOriginal)
                && parsedOriginal > 0)
            {
                originalPrice = parsedOriginal;
            }

            var appId = row.Groups[1].Value;
            var imageUrl = imageMatch.Success ? WebUtility.HtmlDecode(imageMatch.Groups[1].Value) : null;

            return new GameDeal(
                Id: $"steam_{appId}",
                ProviderGameId: appId,
                ProviderName: ProviderNames.Steam,
                Title: WebUtility.HtmlDecode(title.Groups[1].Value),
                Description: null,
                Publisher: null,
                Developer: null,
                OriginalPrice: originalPrice,
                CurrentPrice: finalPrice,
                DiscountPercentage: discountPct,
                Currency: currency,
                StoreUrl: $"https://store.steampowered.com/app/{appId}/",
                ImageUrl: imageUrl,
                ThumbnailUrl: imageUrl,
                StartsAt: null,
                EndsAt: null,
                IsCurrentlyFree: discountPct == 100,
                IsUpcoming: false,
                ReviewScore: null,
                ReviewCount: null,
                ReleaseDate: null,
                Genres: null);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to parse Steam deal: {ex.Message}");
            return null;
        }
    }
}
