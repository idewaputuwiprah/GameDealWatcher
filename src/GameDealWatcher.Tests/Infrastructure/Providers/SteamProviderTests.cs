using System.Text.Json;
using GameDealWatcher.Domain.Entities;
using GameDealWatcher.Infrastructure.Providers;
using Xunit;

namespace GameDealWatcher.Tests.Infrastructure.Providers;

public class SteamProviderTests
{
    [Fact]
    public void ParseSteamDeal_ValidData_ReturnsGameDeal()
    {
        var json = """
            {"id": 730, "name": "Counter-Strike 2", "final_price": 1440, "original_price": 2880, "discount_percent": 50, "header_image": "https://store.steampowered.com/api/storefront/image.jpg"}
            """;
        var doc = JsonDocument.Parse(json);
        var item = doc.RootElement;

        var deal = SteamProvider.ParseSteamDeal(item);

        Assert.NotNull(deal);
        Assert.Equal("steam_730", deal.Id);
        Assert.Equal("Counter-Strike 2", deal.Title);
        Assert.Equal(50, deal.DiscountPercentage);
        Assert.Equal(14.40m, deal.CurrentPrice);
        Assert.Equal(28.80m, deal.OriginalPrice);
        Assert.Equal("https://store.steampowered.com/app/730/", deal.StoreUrl);
        Assert.Equal("https://store.steampowered.com/api/storefront/image.jpg", deal.ImageUrl);
    }

    [Fact]
    public void ParseSteamDeal_MissingId_ReturnsNull()
    {
        var json = """
            {"name": "No ID Game", "final_price": 100, "original_price": 200}
            """;
        var doc = JsonDocument.Parse(json);
        var item = doc.RootElement;

        var deal = SteamProvider.ParseSteamDeal(item);

        Assert.Null(deal);
    }

    [Fact]
    public void ParseSteamDeal_MissingPriceFields_DefaultsToZero()
    {
        var json = """
            {"id": 999, "name": "Free Game", "discount_percent": 0}
            """;
        var doc = JsonDocument.Parse(json);
        var item = doc.RootElement;

        var deal = SteamProvider.ParseSteamDeal(item);

        Assert.NotNull(deal);
        Assert.Equal(0m, deal.CurrentPrice);
        Assert.Equal(0m, deal.OriginalPrice);
    }

    [Fact]
    public void ParseSteamDeal_MissingOriginalPrice_DerivesFromDiscountPct()
    {
        var json = """
            {"id": 200, "name": "Derived Price Game", "final_price": 1500, "discount_percent": 50}
            """;
        var doc = JsonDocument.Parse(json);
        var item = doc.RootElement;

        var deal = SteamProvider.ParseSteamDeal(item);

        Assert.NotNull(deal);
        Assert.Equal(15.00m, deal!.CurrentPrice);
        // originalPrice = finalPrice * 100 / (100 - discountPct) = 1500 * 100 / 50 = 3000 cents = $30.00
        Assert.Equal(30.00m, deal.OriginalPrice);
        Assert.Equal(50, deal.DiscountPercentage);
    }

    [Fact]
    public void ParseSteamDeal_FallsBackToLargeCapsuleImage()
    {
        var json = """
            {"id": 100, "name": "Test", "final_price": 500, "original_price": 1000, "discount_percent": 50, "large_capsule_image": "capsule.jpg"}
            """;
        var doc = JsonDocument.Parse(json);
        var item = doc.RootElement;

        var deal = SteamProvider.ParseSteamDeal(item);

        Assert.NotNull(deal);
        Assert.Equal("capsule.jpg", deal!.ImageUrl);
    }
}