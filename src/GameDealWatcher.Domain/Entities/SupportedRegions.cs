namespace GameDealWatcher.Domain.Entities;

public sealed record SupportedRegion(
    string Code,
    string DisplayName,
    string CurrencyCode,
    string SteamLocale,
    string EpicLocale);

public static class RegionCatalog
{
    private static readonly SupportedRegion[] Regions =
    [
        new("US", "United States", "USD", "english", "en-US"),
        new("GB", "United Kingdom", "GBP", "english", "en-GB"),
        new("DE", "Germany", "EUR", "german", "de-DE"),
        new("FR", "France", "EUR", "french", "fr-FR"),
        new("ES", "Spain", "EUR", "spanish", "es-ES"),
        new("IT", "Italy", "EUR", "italian", "it-IT"),
        new("BR", "Brazil", "BRL", "brazilian", "pt-BR"),
        new("MX", "Mexico", "MXN", "latamspanish", "es-MX"),
        new("JP", "Japan", "JPY", "japanese", "ja-JP"),
        new("KR", "South Korea", "KRW", "koreana", "ko-KR"),
        new("AU", "Australia", "AUD", "english", "en-AU"),
        new("CA", "Canada", "CAD", "english", "en-CA"),
        new("RU", "Russia", "RUB", "russian", "ru-RU"),
        new("CN", "China (Simplified)", "CNY", "schinese", "zh-CN"),
        new("IN", "India", "INR", "english", "en-IN")
    ];

    public static IReadOnlyList<SupportedRegion> All => Regions;

    public static SupportedRegion? ByCode(string code) =>
        Regions.FirstOrDefault(r => r.Code == code);

    public static SupportedRegion Default => Regions[0];
}
