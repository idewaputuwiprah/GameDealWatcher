using Microsoft.Extensions.DependencyInjection;
using Polly;
using Polly.Extensions.Http;

namespace GameDealWatcher.Infrastructure.Http;

public static class HttpClientConfiguration
{
    public const string SteamHandlerName = "SteamHandler";
    public const string EpicHandlerName = "EpicHandler";
    public const string ImageCacheHandlerName = "ImageCache";

    public static void AddHttpClients(this IServiceCollection services)
    {
        // Retry policy: 2 retries with exponential backoff (2s, 4s)
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(2, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        services.AddHttpClient(HttpClientConfiguration.SteamHandlerName, client =>
        {
            client.BaseAddress = new Uri("https://store.steampowered.com/");
            ConfigureBrowserHeaders(client);
        }).AddPolicyHandler(retryPolicy);

        services.AddHttpClient(HttpClientConfiguration.EpicHandlerName, client =>
        {
            client.BaseAddress = new Uri("https://store-site-backend-static.ak.epicgames.com/");
            ConfigureBrowserHeaders(client);
        }).AddPolicyHandler(retryPolicy);

        services.AddHttpClient(HttpClientConfiguration.ImageCacheHandlerName, client =>
        {
            ConfigureBrowserHeaders(client);
            client.Timeout = TimeSpan.FromSeconds(15);
        });
    }

    private static void ConfigureBrowserHeaders(HttpClient client)
    {
        client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        client.DefaultRequestHeaders.Add("Accept", "application/json");
        client.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9");
        client.Timeout = TimeSpan.FromSeconds(30);
    }
}