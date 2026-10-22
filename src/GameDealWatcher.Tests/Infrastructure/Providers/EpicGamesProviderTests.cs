using System.Net;
using System.Text;
using System.Text.Json;
using GameDealWatcher.Domain.Entities;
using GameDealWatcher.Domain.Interfaces;
using GameDealWatcher.Infrastructure.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
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
                "offerType": "BASE_GAME",
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

        var deal = EpicGamesProvider.ParseEpicElement(entry, Now, RegionCatalog.Default);

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
        Assert.Equal("https://cdn.epicgames.com/wide.jpg?w=460&resize=1", deal.ImageUrl);
        Assert.Equal("https://cdn.epicgames.com/thumb.jpg?w=460&resize=1", deal.ThumbnailUrl);
    }

    [Fact]
    public void ParseEpicElement_ImageOnLookalikeHost_IsNotResized()
    {
        var json = """
            {
                "title": "Odd Host",
                "id": "odd1",
                "offerType": "BASE_GAME",
                "price": {"totalPrice": {"originalPrice": 0, "discountPrice": 0}},
                "keyImages": [
                    {"type": "OfferImageWide", "url": "https://myepicgames.com/wide.jpg"}
                ]
            }
            """;
        var doc = JsonDocument.Parse(json);

        var deal = EpicGamesProvider.ParseEpicElement(doc.RootElement, Now, RegionCatalog.Default);

        Assert.NotNull(deal);
        Assert.Equal("https://myepicgames.com/wide.jpg", deal!.ImageUrl);
    }

    [Fact]
    public void ParseEpicElement_ImageUrlWithQuery_AppendsResizeParam()
    {
        var json = """
            {
                "title": "Always Free",
                "id": "free1",
                "offerType": "BASE_GAME",
                "price": {"totalPrice": {"originalPrice": 0, "discountPrice": 0}},
                "keyImages": [
                    {"type": "OfferImageWide", "url": "https://cdn.epicgames.com/wide.jpg?foo=bar"}
                ]
            }
            """;
        var doc = JsonDocument.Parse(json);

        var deal = EpicGamesProvider.ParseEpicElement(doc.RootElement, Now, RegionCatalog.Default);

        Assert.NotNull(deal);
        Assert.Equal("https://cdn.epicgames.com/wide.jpg?foo=bar&w=460&resize=1", deal!.ImageUrl);
    }

    [Fact]
    public void ParseEpicElement_ActivePaidDiscount_IsNotFree()
    {
        var json = """
            {
                "title": "Castlevania Anniversary Collection",
                "id": "paid1",
                "offerType": "BASE_GAME",
                "price": {
                    "totalPrice": {"originalPrice": 285000, "discountPrice": 114000, "discount": 171000}
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
                "productSlug": "castlevania"
            }
            """;
        var doc = JsonDocument.Parse(json);
        var entry = doc.RootElement;

        var deal = EpicGamesProvider.ParseEpicElement(entry, Now, RegionCatalog.Default);

        Assert.NotNull(deal);
        Assert.False(deal!.IsCurrentlyFree);
        Assert.Equal(60, deal.DiscountPercentage);
    }

    [Fact]
    public void ParseEpicElement_PaidWithoutDiscount_ReturnsNull()
    {
        var json = """
            {
                "title": "Luftrausers",
                "id": "catalog1",
                "offerType": "BASE_GAME",
                "price": {"totalPrice": {"originalPrice": 69999, "discountPrice": 69999}}
            }
            """;
        var doc = JsonDocument.Parse(json);
        var entry = doc.RootElement;

        var deal = EpicGamesProvider.ParseEpicElement(entry, Now, RegionCatalog.Default);

        Assert.Null(deal);
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

        var deal = EpicGamesProvider.ParseEpicElement(entry, Now, RegionCatalog.Default);

        Assert.Null(deal);
    }

    [Fact]
    public void ParseEpicElement_UpcomingFreeGame_ReturnsAsUpcoming()
    {
        var json = """
            {
                "title": "Future Free Game",
                "id": "future1",
                "offerType": "BASE_GAME",
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

        var deal = EpicGamesProvider.ParseEpicElement(entry, Now, RegionCatalog.Default);

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
                "offerType": "BASE_GAME",
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

        var deal = EpicGamesProvider.ParseEpicElement(entry, Now, RegionCatalog.Default);

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
                "offerType": "BASE_GAME",
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

        var deal = EpicGamesProvider.ParseEpicElement(entry, Now, RegionCatalog.Default);

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
                "offerType": "BASE_GAME",
                "price": {"totalPrice": {"originalPrice": 0, "discountPrice": 0}}
            }
            """;
        var doc = JsonDocument.Parse(json);
        var entry = doc.RootElement;

        var deal = EpicGamesProvider.ParseEpicElement(entry, Now, RegionCatalog.Default);

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
                "offerType": "BASE_GAME",
                "price": {"totalPrice": {"originalPrice": 0, "discountPrice": 0}}
            }
            """;
        var doc = JsonDocument.Parse(json);
        var entry = doc.RootElement;

        var deal = EpicGamesProvider.ParseEpicElement(entry, Now, RegionCatalog.Default);

        Assert.NotNull(deal);
        Assert.True(deal!.IsCurrentlyFree);
        Assert.False(deal.IsUpcoming);
    }

    [Fact]
    public void ParseEpicElement_ZeroDecimalCurrency_UsesCurrencyInfoDecimals()
    {
        var json = """
            {
                "title": "Yen Game",
                "id": "yen1",
                "offerType": "BASE_GAME",
                "price": {
                    "totalPrice": {
                        "originalPrice": 1980,
                        "discountPrice": 396,
                        "currencyCode": "JPY",
                        "currencyInfo": {"decimals": 0}
                    }
                },
                "promotions": {
                    "promotionalOffers": [
                        {
                            "promotionalOffers": [
                                {
                                    "startDate": "2024-01-10T00:00:00.000Z",
                                    "endDate": "2024-01-20T00:00:00.000Z",
                                    "discountSetting": {"discountType": "PERCENTAGE", "discountPercentage": 20}
                                }
                            ]
                        }
                    ]
                },
                "productSlug": "yen-game"
            }
            """;
        var doc = JsonDocument.Parse(json);
        var entry = doc.RootElement;

        var deal = EpicGamesProvider.ParseEpicElement(entry, Now, RegionCatalog.ByCode("JP")!);

        Assert.NotNull(deal);
        Assert.Equal(1980m, deal!.OriginalPrice);
        Assert.Equal(396m, deal.CurrentPrice);
        Assert.Equal(80, deal.DiscountPercentage);
        Assert.Equal("JPY", deal.Currency);
    }

    [Fact]
    public void ParseEpicElement_UpcomingPaidDiscount_ReturnsNull()
    {
        var json = """
            {
                "title": "Future Sale Game",
                "id": "futuresale1",
                "offerType": "BASE_GAME",
                "price": {
                    "totalPrice": {"originalPrice": 3999, "discountPrice": 3999}
                },
                "promotions": {
                    "upcomingPromotionalOffers": [
                        {
                            "promotionalOffers": [
                                {
                                    "startDate": "2024-02-01T00:00:00.000Z",
                                    "endDate": "2024-02-08T00:00:00.000Z",
                                    "discountSetting": {"discountType": "PERCENTAGE", "discountPercentage": 20}
                                }
                            ]
                        }
                    ]
                },
                "productSlug": "future-sale-game"
            }
            """;
        var doc = JsonDocument.Parse(json);
        var entry = doc.RootElement;

        var deal = EpicGamesProvider.ParseEpicElement(entry, Now, RegionCatalog.Default);

        Assert.Null(deal);
    }

    [Theory]
    [InlineData("""{"nope":1}""")]
    [InlineData("""{"data":{}}""")]
    [InlineData("""{"data":{"Catalog":{"searchStore":{}}}}""")]
    public async Task GetDealsAsync_MalformedResponse_Throws(string json)
    {
        await Assert.ThrowsAsync<InvalidOperationException>(() => CreateProvider(json).GetDealsAsync(CancellationToken.None));
    }

    [Fact]
    public async Task GetDealsAsync_EmptyElements_ReturnsEmpty()
    {
        var deals = await CreateProvider("""{"data":{"Catalog":{"searchStore":{"elements":[]}}}}""")
            .GetDealsAsync(CancellationToken.None);

        Assert.Empty(deals);
    }

    private static EpicGamesProvider CreateProvider(string json)
    {
        var settings = new Mock<ISettingsRepository>();
        settings.Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppSettings { SelectedRegionCode = "US" });

        var client = new HttpClient(new StubHandler(json))
        {
            BaseAddress = new Uri("https://store-site-backend-static.ak.epicgames.com/")
        };
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        return new EpicGamesProvider(factory.Object, settings.Object, NullLogger<EpicGamesProvider>.Instance);
    }

    private sealed class StubHandler(string json) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
    }
}
