using System.Globalization;

namespace GameDealWatcher.Domain.Formatting;

/// <summary>
/// Formats prices the way the Steam store displays them per currency:
/// local symbol, symbol position and decimal/group separators.
/// </summary>
public static class PriceFormatter
{
    public static string Format(decimal price, string? currency)
    {
        if (price <= 0) return "Free";

        var code = string.IsNullOrWhiteSpace(currency) ? "USD" : currency.Trim().ToUpperInvariant();
        return price.ToString("C", FormatFor(code));
    }

    public static bool TryParse(string? text, string? currency, out decimal price)
    {
        price = 0m;
        if (string.IsNullOrWhiteSpace(text)) return false;

        var code = string.IsNullOrWhiteSpace(currency) ? "USD" : currency.Trim().ToUpperInvariant();
        var numeric = new string(text
            .Replace('\u00A0', ' ')
            .Where(c => char.IsDigit(c) || c is '.' or ',' or '-' or ' ')
            .ToArray());
        var format = FormatFor(code);
        format.NumberDecimalSeparator = format.CurrencyDecimalSeparator;
        format.NumberGroupSeparator = format.CurrencyGroupSeparator;
        return decimal.TryParse(numeric, NumberStyles.Number, format, out price);
    }

    private static NumberFormatInfo FormatFor(string currency)
    {
        var format = (NumberFormatInfo)CultureInfo.InvariantCulture.NumberFormat.Clone();
        switch (currency)
        {
            // symbol, positive pattern (0="$n", 1="n$", 2="$ n", 3="n $"), decimals, group, decimal
            case "USD": Set(format, "$", 0, 2, ",", "."); break;
            case "GBP": Set(format, "£", 0, 2, ",", "."); break;
            case "EUR": Set(format, "€", 1, 2, ".", ","); break;
            case "BRL": Set(format, "R$", 2, 2, ".", ","); break;
            case "MXN": Set(format, "MX$", 2, 2, ",", "."); break;
            case "AUD": Set(format, "A$", 2, 2, ",", "."); break;
            case "CAD": Set(format, "CDN$", 2, 2, ",", "."); break;
            case "JPY": Set(format, "¥", 2, 0, ",", "."); break;
            case "KRW": Set(format, "₩", 2, 0, ",", "."); break;
            case "CNY": Set(format, "¥", 2, 2, ",", "."); break;
            case "INR": Set(format, "₹", 2, 0, ",", "."); break;
            case "RUB": Set(format, "₽", 3, 0, " ", ","); break;
            case "IDR": Set(format, "Rp", 2, 0, ".", ","); break;
            default: Set(format, currency + " ", 0, 2, ",", "."); break;
        }
        return format;
    }

    private static void Set(NumberFormatInfo format, string symbol, int positivePattern,
        int decimals, string groupSeparator, string decimalSeparator)
    {
        format.CurrencySymbol = symbol;
        format.CurrencyPositivePattern = positivePattern;
        format.CurrencyDecimalDigits = decimals;
        format.CurrencyGroupSeparator = groupSeparator;
        format.CurrencyDecimalSeparator = decimalSeparator;
    }
}
