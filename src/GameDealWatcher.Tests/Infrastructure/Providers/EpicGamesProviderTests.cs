using System.Text.Json;
using GameDealWatcher.Infrastructure.Providers;
using Xunit;

namespace GameDealWatcher.Tests.Infrastructure.Providers;

public class EpicGamesProviderTests
{
    private static readonly DateTimeOffset Now = new(2024, 1, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ParseEpicElement_ValidFreeGame_ReturnsDeal()
    {
        var json = """
            {
                "title": "Death Stranding",
                "id": "abc123",
                "offerType": "BASEGAME",
                "price": {
                    "totalPrice": {
                        "originalPrice": 3999,
                        "discountPrice": 0,
                        "discount": 3999,
                        "currencyCode": "USD"
                    }
                },
                "promotions": {
                    "promotionalOffers": [
                        {
                            "promotionalOffers": [
                                {
                                    "startDate": "2024-01-10T00:00:00.000Z",
                                    "endDate": "2024-01-20T00:00:00.000Z"
                                }
                            ]
                        }
                    ]
                },
                "keyImages": [
                    {"type": "OfferImageWide", "url": "https://cdn.epicgames.com/wide.jpg"},
                    {"type": "Thumbnail", "url": "https://cdn.epicgames.com/thumb.jpg"}
                ],
                "productSlug": "death-stranding"
            }
            """;
        var doc = JsonDocument.Parse(json);
        var entry = doc.RootElement;

        var deal = EpicGamesProvider.ParseEpicElement(entry, Now);

        Assert.NotNull(deal);
        Assert.Equal("epic_abc123", deal!.Id);
        Assert.Equal("Death Stranding", deal.Title);
        Assert.Equal("Epic", deal.ProviderName);
        Assert.Equal(39.99m, deal.OriginalPrice);
        Assert.Equal(0m, deal.CurrentPrice);
        Assert.Equal(100, deal.DiscountPercentage);
        Assert.True(deal.IsCurrentlyFree);
        Assert.False(deal.IsUpcoming);
        Assert.Equal("https://www.epicgames.com/store/en-US/p/death-stranding", deal.StoreUrl);
        Assert.Equal("https://cdn.epicgames.com/wide.jpg", deal.ImageUrl);
        Assert.Equal("https://cdn.epicgames.com/thumb.jpg", deal.ThumbnailUrl);
    }

    [Fact]
    public void ParseEpicElement_DlcOfferType_ReturnsNull()
    {
        var json = """
            {
                "title": "Some DLC",
                "id": "dlc1",
                "offerType": "DLC",
                "price": {
                    "totalPrice": {"originalPrice": 1000, "discountPrice": 500}
                }
            }
            """;
        var doc = JsonDocument.Parse(json);
        var entry = doc.RootElement;

        var deal = EpicGamesProvider.ParseEpicElement(entry, Now);

        Assert.Null(deal);
    }

    [Fact]
    public void ParseEpicElement_UpcomingFreeGame_ReturnsAsUpcoming()
    {
        var json = """
            {
                "title": "Future Free Game",
                "id": "future1",
                "offerType": "BASEGAME",
                "price": {
                    "totalPrice": {"originalPrice": 2999, "discountPrice": 2999}
                },
                "promotions": {
                    "upcomingPromotionalOffers": [
                        {
                            "promotionalOffers": [
                                {
                                    "startDate": "2024-02-01T00:00:00.000Z",
                                    "endDate": "2024-02-08T00:00:00.000Z"
                                }
                            ]
                        }
                    ]
                },
                "productSlug": "future-game"
            }
            """;
        var doc = JsonDocument.Parse(json);
        var entry = doc.RootElement;

        var deal = EpicGamesProvider.ParseEpicElement(entry, Now);

        Assert.NotNull(deal);
        Assert.False(deal!.IsCurrentlyFree);
        Assert.True(deal.IsUpcoming);
        Assert.Equal(29.99m, deal.OriginalPrice);
        Assert.Equal(29.99m, deal.CurrentPrice);
    }

    [Fact]
    public void ParseEpicElement_BothActiveAndUpcoming_ReturnsAsCurrentlyFree()
    {
        var json = """
            {
                "title": "Active Plus Upcoming",
                "id": "both1",
                "offerType": "BASEGAME",
                "price": {
                    "totalPrice": {"originalPrice": 2999, "discountPrice": 0, "discount": 2999}
                },
                "promotions": {
                    "promotionalOffers": [
                        {
                            "promotionalOffers": [
                                {
                                    "startDate": "2024-01-10T00:00:00.000Z",
                                    "endDate": "2024-01-20T00:00:00.000Z"
                                }
                            ]
                        }
                    ],
                    "upcomingPromotionalOffers": [
                        {
                            "promotionalOffers": [
                                {
                                    "startDate": "2024-02-01T00:00:00.000Z",
                                    "endDate": "2024-02-08T00:00:00.000Z"
                                }
                            ]
                        }
                    ]
                },
                "productSlug": "both-game"
            }
            """;
        var doc = JsonDocument.Parse(json);
        var entry = doc.RootElement;

        var deal = EpicGamesProvider.ParseEpicElement(entry, Now);

        Assert.NotNull(deal);
        Assert.True(deal!.IsCurrentlyFree);
        Assert.False(deal.IsUpcoming);
        // Should use active offer dates, not upcoming
        Assert.Equal(new DateTimeOffset(2024, 1, 10, 0, 0, 0, TimeSpan.Zero), deal.StartsAt);
        Assert.Equal(new DateTimeOffset(2024, 1, 20, 0, 0, 0, TimeSpan.Zero), deal.EndsAt);
    }

    [Fact]
    public void ParseEpicElement_NoProductSlug_FallsBackToCatalogNsMappings()
    {
        var json = """
            {
                "title": "Fallback Game",
                "id": "fallback1",
                "offerType": "BASEGAME",
                "price": {"totalPrice": {"originalPrice": 0, "discountPrice": 0}},
                "catalogNs": {
                    "mappings": [
                        {"pageSlug": "fallback-slug", "pageType": "productHome"}
                    ]
                }
            }
            """;
        var doc = JsonDocument.Parse(json);
        var entry = doc.RootElement;

        var deal = EpicGamesProvider.ParseEpicElement(entry, Now);

        Assert.NotNull(deal);
        Assert.Equal("https://www.epicgames.com/store/en-US/p/fallback-slug", deal!.StoreUrl);
    }

    [Fact]
    public void ParseEpicElement_NoSlugOrMappings_FallsBackToOfferId()
    {
        var json = """
            {
                "title": "No URL Game",
                "id": "nourl1",
                "offerType": "BASEGAME",
                "price": {"totalPrice": {"originalPrice": 0, "discountPrice": 0}}
            }
            """;
        var doc = JsonDocument.Parse(json);
        var entry = doc.RootElement;

        var deal = EpicGamesProvider.ParseEpicElement(entry, Now);

        Assert.NotNull(deal);
        Assert.Equal("https://www.epicgames.com/store/en-US/p/nourl1", deal!.StoreUrl);
    }

    [Fact]
    public void ParseEpicElement_AlwaysFreeGame_NoPromotions_ReturnsAsFree()
    {
        var json = """
            {
                "title": "Always Free",
                "id": "free1",
                "offerType": "BASEGAME",
                "price": {"totalPrice": {"originalPrice": 0, "discountPrice": 0}}
            }
            """;
        var doc = JsonDocument.Parse(json);
        var entry = doc.RootElement;

        var deal = EpicGamesProvider.ParseEpicElement(entry, Now);

        Assert.NotNull(deal);
        Assert.True(deal!.IsCurrentlyFree);
        Assert.False(deal.IsUpcoming);
    }
}
