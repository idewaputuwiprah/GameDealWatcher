using GameDealWatcher.Domain.Formatting;
using Xunit;

namespace GameDealWatcher.Tests.Domain.Formatting;

public class PriceFormatterTests
{
    [Theory]
    [InlineData(14.99, "USD", "$14.99")]
    [InlineData(10.99, "GBP", "£10.99")]
    [InlineData(14.99, "EUR", "14,99€")]
    [InlineData(1299.50, "EUR", "1.299,50€")]
    [InlineData(29.99, "BRL", "R$ 29,99")]
    [InlineData(199, "MXN", "MX$ 199.00")]
    [InlineData(14.95, "AUD", "A$ 14.95")]
    [InlineData(14.99, "CAD", "CDN$ 14.99")]
    [InlineData(1980, "JPY", "¥ 1,980")]
    [InlineData(12000, "KRW", "₩ 12,000")]
    [InlineData(58, "CNY", "¥ 58.00")]
    [InlineData(499, "INR", "₹ 499")]
    [InlineData(549, "RUB", "549 ₽")]
    [InlineData(438000, "IDR", "Rp 438.000")]
    [InlineData(10, "XYZ", "XYZ 10.00")]
    public void Format_UsesSteamStyleForCurrency(decimal price, string currency, string expected)
    {
        Assert.Equal(expected, PriceFormatter.Format(price, currency));
    }

    [Fact]
    public void Format_ZeroPrice_IsFree()
    {
        Assert.Equal("Free", PriceFormatter.Format(0m, "IDR"));
    }

    [Fact]
    public void Format_MissingCurrency_DefaultsToUsd()
    {
        Assert.Equal("$14.99", PriceFormatter.Format(14.99m, null));
    }

    [Theory]
    [InlineData("$59.99", "USD", 59.99)]
    [InlineData("14,99\u20AC", "EUR", 14.99)]
    [InlineData("1.299,50\u20AC", "EUR", 1299.50)]
    [InlineData("\u00A5 1,980", "JPY", 1980)]
    [InlineData("R$ 29,99", "BRL", 29.99)]
    [InlineData("Rp 438.000", "IDR", 438000)]
    public void TryParse_ReadsSteamPriceText(string text, string currency, decimal expected)
    {
        Assert.True(PriceFormatter.TryParse(text, currency, out var price));
        Assert.Equal(expected, price);
    }
}
