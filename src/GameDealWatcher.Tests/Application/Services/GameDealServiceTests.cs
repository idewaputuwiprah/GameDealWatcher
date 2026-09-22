using GameDealWatcher.Application.Services;
using GameDealWatcher.Domain.Entities;
using GameDealWatcher.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace GameDealWatcher.Tests.Application.Services;

public class GameDealServiceTests
{
    private static GameDeal CreateDeal(string id, string providerGameId, string providerName,
        string title, decimal originalPrice, decimal currentPrice, int discountPct,
        bool isCurrentlyFree = false, bool isUpcoming = false) =>
        new(id, providerGameId, providerName, title, null, null, null,
            originalPrice, currentPrice, discountPct, "USD",
            $"https://store.example.com/app/{providerGameId}/",
            null, null, null, null, isCurrentlyFree, isUpcoming, null, null, null, null);

    private static GameDealService CreateService(
        Mock<IGameDealRepository> repoMock,
        List<IGameDealProvider> providers,
        Mock<ISettingsRepository> settingsMock,
        Mock<INotificationService> notificationMock,
        ILogger<GameDealService>? logger = null)
    {
        settingsMock.Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppSettings());
        repoMock.Setup(r => r.GetAllDealsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GameDeal>());
        return new GameDealService(
            repoMock.Object,
            providers,
            settingsMock.Object,
            notificationMock.Object,
            logger ?? NullLogger<GameDealService>.Instance);
    }

    [Fact]
    public async Task RefreshAllDealsAsync_WithNewEpicFreeGame_SendsNotification()
    {
        var repoMock = new Mock<IGameDealRepository>();
        var settingsMock = new Mock<ISettingsRepository>();
        var notifMock = new Mock<INotificationService>();
        var providerMock = new Mock<IGameDealProvider>();
        providerMock.SetupGet(p => p.ProviderName).Returns(ProviderNames.Epic);
        var epicDeal = CreateDeal("epic_1", "epic1", ProviderNames.Epic, "Free Game", 39.99m, 0m, 100, isCurrentlyFree: true);
        providerMock.Setup(p => p.GetDealsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GameDeal> { epicDeal });

        var service = CreateService(repoMock, new List<IGameDealProvider> { providerMock.Object }, settingsMock, notifMock);

        await service.RefreshAllDealsAsync(CancellationToken.None);

        notifMock.Verify(n => n.ShowDealNotification(It.Is<string>(t => t == "Free Game!"), It.Is<string>(m => m.Contains("Free Game"))), Times.Once);
    }

    [Fact]
    public async Task RefreshAllDealsAsync_WithEpicNotificationsDisabled_DoesNotSendNotification()
    {
        var repoMock = new Mock<IGameDealRepository>();
        repoMock.Setup(r => r.GetAllDealsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GameDeal>());
        var settingsMock = new Mock<ISettingsRepository>();
        settingsMock.Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppSettings { EpicNotifications = false });
        var notifMock = new Mock<INotificationService>();
        var providerMock = new Mock<IGameDealProvider>();
        providerMock.SetupGet(p => p.ProviderName).Returns(ProviderNames.Epic);
        var epicDeal = CreateDeal("epic_1", "epic1", ProviderNames.Epic, "Free Game", 39.99m, 0m, 100, isCurrentlyFree: true);
        providerMock.Setup(p => p.GetDealsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GameDeal> { epicDeal });

        var service = new GameDealService(
            repoMock.Object,
            new List<IGameDealProvider> { providerMock.Object },
            settingsMock.Object,
            notifMock.Object,
            NullLogger<GameDealService>.Instance);

        await service.RefreshAllDealsAsync(CancellationToken.None);

        notifMock.Verify(n => n.ShowDealNotification(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RefreshAllDealsAsync_WithSteamDiscountBelowMinimum_DoesNotSendNotification()
    {
        var repoMock = new Mock<IGameDealRepository>();
        repoMock.Setup(r => r.GetAllDealsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GameDeal>());
        var settingsMock = new Mock<ISettingsRepository>();
        settingsMock.Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppSettings { MinimumSteamDiscount = 50 });
        var notifMock = new Mock<INotificationService>();
        var providerMock = new Mock<IGameDealProvider>();
        providerMock.SetupGet(p => p.ProviderName).Returns(ProviderNames.Steam);
        var steamDeal = CreateDeal("steam_1", "1", ProviderNames.Steam, "Cheap Game", 19.99m, 14.99m, 25);
        providerMock.Setup(p => p.GetDealsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GameDeal> { steamDeal });

        var service = new GameDealService(
            repoMock.Object,
            new List<IGameDealProvider> { providerMock.Object },
            settingsMock.Object,
            notifMock.Object,
            NullLogger<GameDealService>.Instance);

        await service.RefreshAllDealsAsync(CancellationToken.None);

        notifMock.Verify(n => n.ShowDealNotification(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task RefreshAllDealsAsync_WithSteamDiscountAboveMinimum_SendsNotification()
    {
        var repoMock = new Mock<IGameDealRepository>();
        repoMock.Setup(r => r.GetAllDealsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GameDeal>());
        var settingsMock = new Mock<ISettingsRepository>();
        settingsMock.Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppSettings { MinimumSteamDiscount = 50 });
        var notifMock = new Mock<INotificationService>();
        var providerMock = new Mock<IGameDealProvider>();
        providerMock.SetupGet(p => p.ProviderName).Returns(ProviderNames.Steam);
        var steamDeal = CreateDeal("steam_1", "1", ProviderNames.Steam, "Discounted Game", 59.99m, 29.99m, 50);
        providerMock.Setup(p => p.GetDealsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GameDeal> { steamDeal });

        var service = new GameDealService(
            repoMock.Object,
            new List<IGameDealProvider> { providerMock.Object },
            settingsMock.Object,
            notifMock.Object,
            NullLogger<GameDealService>.Instance);

        await service.RefreshAllDealsAsync(CancellationToken.None);

        notifMock.Verify(n => n.ShowDealNotification(It.Is<string>(t => t == "Steam Deal!"), It.Is<string>(m => m.Contains("Discounted Game"))), Times.Once);
    }

    [Fact]
    public async Task RefreshAllDealsAsync_WithProviderFailure_ContinuesWithOtherProvider()
    {
        var repoMock = new Mock<IGameDealRepository>();
        repoMock.Setup(r => r.GetAllDealsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GameDeal>());
        var settingsMock = new Mock<ISettingsRepository>();
        settingsMock.Setup(s => s.GetSettingsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new AppSettings());
        var notifMock = new Mock<INotificationService>();

        var failingProvider = new Mock<IGameDealProvider>();
        failingProvider.SetupGet(p => p.ProviderName).Returns(ProviderNames.Epic);
        failingProvider.Setup(p => p.GetDealsAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("API down"));

        var successProvider = new Mock<IGameDealProvider>();
        successProvider.SetupGet(p => p.ProviderName).Returns(ProviderNames.Steam);
        var steamDeal = CreateDeal("steam_1", "1", ProviderNames.Steam, "Working Game", 59.99m, 29.99m, 50);
        successProvider.Setup(p => p.GetDealsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GameDeal> { steamDeal });

        var service = new GameDealService(
            repoMock.Object,
            new List<IGameDealProvider> { failingProvider.Object, successProvider.Object },
            settingsMock.Object,
            notifMock.Object,
            NullLogger<GameDealService>.Instance);

        await service.RefreshAllDealsAsync(CancellationToken.None);

        repoMock.Verify(r => r.InsertDealsAsync(It.Is<IReadOnlyList<GameDeal>>(l => l.Count == 1), It.IsAny<CancellationToken>()), Times.Once);
        repoMock.Verify(r => r.RecordRefreshAsync(ProviderNames.Epic, false, 0, It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
        repoMock.Verify(r => r.RecordRefreshAsync(ProviderNames.Steam, true, 1, null, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshAllDealsAsync_WithNoDeals_DoesNotUpdateRefreshTimestamp()
    {
        var repoMock = new Mock<IGameDealRepository>();
        var settingsMock = new Mock<ISettingsRepository>();
        var notifMock = new Mock<INotificationService>();
        var providerMock = new Mock<IGameDealProvider>();
        providerMock.SetupGet(p => p.ProviderName).Returns(ProviderNames.Steam);
        providerMock.Setup(p => p.GetDealsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GameDeal>());

        var service = CreateService(repoMock, new List<IGameDealProvider> { providerMock.Object }, settingsMock, notifMock);

        await service.RefreshAllDealsAsync(CancellationToken.None);

        repoMock.Verify(r => r.SaveLastRefreshAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
        repoMock.Verify(r => r.DeleteStaleDealsAsync(It.IsAny<TimeSpan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task RefreshAllDealsAsync_WithPriceChange_RecordsPriceChange()
    {
        var existingDeal = CreateDeal("steam_1", "1", ProviderNames.Steam, "Price Change Game", 59.99m, 29.99m, 50);
        var repoMock = new Mock<IGameDealRepository>();

        var settingsMock = new Mock<ISettingsRepository>();
        var notifMock = new Mock<INotificationService>();
        var providerMock = new Mock<IGameDealProvider>();
        providerMock.SetupGet(p => p.ProviderName).Returns(ProviderNames.Steam);
        var changedDeal = CreateDeal("steam_1", "1", ProviderNames.Steam, "Price Change Game", 59.99m, 19.99m, 67);
        providerMock.Setup(p => p.GetDealsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GameDeal> { changedDeal });

        var service = CreateService(repoMock, new List<IGameDealProvider> { providerMock.Object }, settingsMock, notifMock);

        // Override the default empty list set by CreateService with the existing deal
        repoMock.Setup(r => r.GetAllDealsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GameDeal> { existingDeal });

        await service.RefreshAllDealsAsync(CancellationToken.None);

        repoMock.Verify(r => r.RecordPriceChangesAsync(It.Is<IReadOnlyList<GameDeal>>(l => l.Count == 1), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RefreshAllDealsAsync_WithDeals_DeletesUnseenDeals()
    {
        var repoMock = new Mock<IGameDealRepository>();
        var settingsMock = new Mock<ISettingsRepository>();
        var notifMock = new Mock<INotificationService>();
        var providerMock = new Mock<IGameDealProvider>();
        providerMock.SetupGet(p => p.ProviderName).Returns(ProviderNames.Steam);
        var deal = CreateDeal("steam_1", "1", ProviderNames.Steam, "Game", 59.99m, 29.99m, 50);
        providerMock.Setup(p => p.GetDealsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GameDeal> { deal });

        var service = CreateService(repoMock, new List<IGameDealProvider> { providerMock.Object }, settingsMock, notifMock);

        await service.RefreshAllDealsAsync(CancellationToken.None);

        repoMock.Verify(r => r.DeleteDealsNotSeenAsync(It.Is<IReadOnlyList<string>>(l => l.Contains("steam_1")), It.IsAny<CancellationToken>()), Times.Once);
    }
}
