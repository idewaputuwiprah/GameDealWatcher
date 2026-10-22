using GameDealWatcher.Domain.Entities;
using Xunit;

namespace GameDealWatcher.Tests.Domain.Entities;

public class GameDealTests
{
    private static readonly DateTimeOffset Now = new(2024, 1, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void IsFreeAt_ActiveGiveaway_ReturnsTrue()
    {
        var deal = CreateDeal(isCurrentlyFree: true, endsAt: Now.AddDays(1));

        Assert.True(deal.IsFreeAt(Now));
    }

    [Fact]
    public void IsFreeAt_ExpiredGiveaway_ReturnsFalse()
    {
        var deal = CreateDeal(isCurrentlyFree: true, endsAt: Now.AddMinutes(-1));

        Assert.False(deal.IsFreeAt(Now));
    }

    [Fact]
    public void IsUpcomingAt_BeforeStart_ReturnsTrue()
    {
        var deal = CreateDeal(isUpcoming: true, startsAt: Now.AddDays(1));

        Assert.True(deal.IsUpcomingAt(Now));
    }

    [Fact]
    public void IsUpcomingAt_AfterStart_ReturnsFalse()
    {
        var deal = CreateDeal(isUpcoming: true, startsAt: Now.AddMinutes(-1));

        Assert.False(deal.IsUpcomingAt(Now));
    }

    private static GameDeal CreateDeal(
        bool isCurrentlyFree = false,
        bool isUpcoming = false,
        DateTimeOffset? startsAt = null,
        DateTimeOffset? endsAt = null) =>
        new("epic_1", "1", ProviderNames.Epic, "Test", null, null, null,
            0m, 0m, 100, "USD", "https://example.com", null, null,
            startsAt, endsAt, isCurrentlyFree, isUpcoming, null, null, null, null);
}
