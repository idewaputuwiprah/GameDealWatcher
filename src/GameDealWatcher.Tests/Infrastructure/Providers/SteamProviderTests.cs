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
            {"id": 730, "name": "Counter-Strike 2", "final": 1440, "final2": 2880, "discount_pct": 50, "img": "test.jpg"}
            """;
        var doc = JsonDocument.Parse(json);
        var item = doc.RootElement;

        var deal = SteamProvider.ParseSteamDeal(item);

        Assert.NotNull(deal);
        Assert.Equal("steam_730", deal.Id);
        Assert.Equal("Counter-Strike 2", deal.Title);
        Assert.Equal(50, deal.DiscountPercentage);
    }
}