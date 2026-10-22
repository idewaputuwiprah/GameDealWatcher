using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using GameDealWatcher.Domain.Entities;
using GameDealWatcher.Domain.Interfaces;
using GameDealWatcher.Infrastructure.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameDealWatcher.Tests.Infrastructure.Providers;

public class SteamProviderTests
{
    private const string ResultsHtml = """
        <a href="https://store.steampowered.com/app/1091500/Cyberpunk_2077/?snr=1_7_7" data-ds-appid="1091500" class="search_result_row">
          <div class="search_capsule"><img src="https://cdn.example.com/cp.jpg"></div>
          <div class="search_name"><span class="title">Cyberpunk 2077</span></div>
          <div class="search_price_discount_combined" data-price-final="1799">
            <div class="discount_block" data-price-final="1799" data-discount="70"></div>
            <div class="discount_original_price">$59.99</div>
          </div>
        </a>
        <a href="https://store.steampowered.com/bundle/123/Some_Bundle/" data-ds-bundleid="123" class="search_result_row">
          <div class="search_name"><span class="title">Some Bundle</span></div>
          <div class="discount_block" data-price-final="999" data-discount="50"></div>
        </a>
        <a href="https://store.steampowered.com/app/730/Counter-Strike_2/" data-ds-appid="730" class="search_result_row">
          <div class="search_capsule"><img src="https://cdn.example.com/cs.jpg"></div>
          <div class="search_name"><span class="title">Tom &amp; Jerry&#39;s Game</span></div>
          <div class="discount_block" data-price-final="0" data-discount="100"></div>
          <div class="discount_original_price">$19.99</div>
        </a>
        <a href="https://store.steampowered.com/app/999/No_Price/" data-ds-appid="999" class="search_result_row">
          <div class="search_name"><span class="title">No Price</span></div>
        </a>
        """;

    [Fact]
    public void ParseSearchResults_ParsesAppRowsAndSkipsBundles()
    {
        var deals = SteamProvider.ParseSearchResults(ResultsHtml, "USD");

        Assert.Equal(2, deals.Count);
        var cyberpunk = deals[0];
        Assert.Equal("steam_1091500", cyberpunk.Id);
        Assert.Equal("1091500", cyberpunk.ProviderGameId);
        Assert.Equal("Steam", cyberpunk.ProviderName);
        Assert.Equal("Cyberpunk 2077", cyberpunk.Title);
        Assert.Equal(70, cyberpunk.DiscountPercentage);
        Assert.Equal(17.99m, cyberpunk.CurrentPrice);
        Assert.Equal(59.99m, cyberpunk.OriginalPrice);
        Assert.Equal("USD", cyberpunk.Currency);
        Assert.Equal("https://store.steampowered.com/app/1091500/", cyberpunk.StoreUrl);
        Assert.Equal("https://cdn.example.com/cp.jpg", cyberpunk.ImageUrl);
        Assert.Equal("https://cdn.example.com/cp.jpg", cyberpunk.ThumbnailUrl);
    }

    [Fact]
    public void ParseSearchResults_DecodesHtmlEntitiesInTitle()
    {
        var deals = SteamProvider.ParseSearchResults(ResultsHtml, "USD");

        Assert.Equal("Tom & Jerry's Game", deals[1].Title);
    }

    [Fact]
    public void ParseSearchResults_FullyDiscountedRow_HasZeroCurrentPrice()
    {
        var deals = SteamProvider.ParseSearchResults(ResultsHtml, "USD");

        Assert.Equal(100, deals[1].DiscountPercentage);
        Assert.Equal(0m, deals[1].CurrentPrice);
        Assert.Equal(19.99m, deals[1].OriginalPrice);
        Assert.True(deals[1].IsCurrentlyFree);
    }

    [Fact]
    public void ParseSearchResults_EmptyHtml_ReturnsEmpty()
    {
        Assert.Empty(SteamProvider.ParseSearchResults("", "USD"));
    }

    [Fact]
    public async Task GetDealsAsync_PagesUntilTotalCount()
    {
        var factory = new FakeHttpClientFactory(request =>
        {
            var start = StartOf(request.RequestUri!);
            return Json(Page(start, Math.Min(100, 250 - start), 250));
        });
        var provider = CreateProvider(factory);

        var deals = await provider.GetDealsAsync(CancellationToken.None);

        Assert.Equal(250, deals.Count);
        Assert.Equal(new[] { 0, 100, 200 }, factory.Requests.Select(StartOf));
    }

    [Fact]
    public async Task GetDealsAsync_EmptyPageBeforeTotalCount_Throws()
    {
        var factory = new FakeHttpClientFactory(request =>
        {
            var start = StartOf(request.RequestUri!);
            return Json(start == 0 ? Page(0, 100, 300) : Page(0, 0, 300));
        });
        var provider = CreateProvider(factory);

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetDealsAsync(CancellationToken.None));
        Assert.Equal(2, factory.Requests.Count);
    }

    [Fact]
    public async Task GetDealsAsync_ZeroTotalCount_Throws()
    {
        var factory = new FakeHttpClientFactory(_ => Json(Page(0, 0, 0)));
        var provider = CreateProvider(factory);

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetDealsAsync(CancellationToken.None));

        Assert.Single(factory.Requests);
    }

    [Fact]
    public async Task GetDealsAsync_AppRowsWithoutParseableDeals_Throws()
    {
        const string html = """<a href="https://store.steampowered.com/app/1/No_Price/" class="search_result_row"><span class="title">No Price</span></a>""";
        var body = System.Text.Json.JsonSerializer.Serialize(new { success = 1, total_count = 1, results_html = html });
        var factory = new FakeHttpClientFactory(_ => Json(body));
        var provider = CreateProvider(factory);

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetDealsAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetDealsAsync_DuplicateAppAcrossPages_ReturnsOne()
    {
        var factory = new FakeHttpClientFactory(_ => Json(Page(0, 1, 101)));
        var provider = CreateProvider(factory);

        var deals = await provider.GetDealsAsync(CancellationToken.None);

        Assert.Single(deals);
        Assert.Equal(2, factory.Requests.Count);
    }

    [Fact]
    public async Task GetDealsAsync_AllBundlePage_Continues()
    {
        var factory = new FakeHttpClientFactory(request =>
        {
            var start = StartOf(request.RequestUri!);
            return Json(start == 0 ? BundlePage(100, 150) : Page(100, 50, 150));
        });
        var provider = CreateProvider(factory);

        var deals = await provider.GetDealsAsync(CancellationToken.None);

        Assert.Equal(50, deals.Count);
        Assert.Equal(2, factory.Requests.Count);
    }

    [Fact]
    public async Task GetDealsAsync_UnsuccessfulResponse_Throws()
    {
        var factory = new FakeHttpClientFactory(_ => Json(Page(0, 100, 250, success: 0)));
        var provider = CreateProvider(factory);

        await Assert.ThrowsAsync<InvalidOperationException>(() => provider.GetDealsAsync(CancellationToken.None));
    }

    private static SteamProvider CreateProvider(IHttpClientFactory factory)
    {
        var settings = new Mock<ISettingsRepository>();
        settings.Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppSettings { SelectedRegionCode = "US" });
        return new SteamProvider(factory, settings.Object, NullLogger<SteamProvider>.Instance);
    }

    private static HttpResponseMessage Json(string body) => new(HttpStatusCode.OK)
    {
        Content = new StringContent(body, Encoding.UTF8, "application/json")
    };

    private static string Page(int start, int rows, int totalCount, int success = 1)
    {
        var html = string.Concat(Enumerable.Range(0, rows).Select(i =>
            $"""<a href="https://store.steampowered.com/app/{start + i}/Game_{start + i}/" class="search_result_row"><span class="title">Game {start + i}</span><div class="discount_block" data-price-final="999" data-discount="50"></div></a>"""));
        return System.Text.Json.JsonSerializer.Serialize(new { success, total_count = totalCount, results_html = html });
    }

    private static string BundlePage(int rows, int totalCount)
    {
        var html = string.Concat(Enumerable.Range(0, rows).Select(i =>
            $"""<a href="https://store.steampowered.com/bundle/{i}/Bundle_{i}/" class="search_result_row"><span class="title">Bundle {i}</span></a>"""));
        return System.Text.Json.JsonSerializer.Serialize(new { success = 1, total_count = totalCount, results_html = html });
    }

    private static int StartOf(Uri uri) =>
        int.Parse(Regex.Match(uri.Query, @"[?&]start=(\d+)").Groups[1].Value);

    private sealed class FakeHttpClientFactory(Func<HttpRequestMessage, HttpResponseMessage> respond) : IHttpClientFactory
    {
        public List<Uri> Requests { get; } = [];

        public HttpClient CreateClient(string name) =>
            new(new Handler(this, respond)) { BaseAddress = new Uri("https://store.steampowered.com/") };

        private sealed class Handler(FakeHttpClientFactory owner, Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                owner.Requests.Add(request.RequestUri!);
                return Task.FromResult(respond(request));
            }
        }
    }
}
