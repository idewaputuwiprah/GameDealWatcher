using Microsoft.Extensions.DependencyInjection;

namespace GameDealWatcher.Infrastructure.Http;

public static class HttpClientConfiguration
{
    public const string SteamHandlerName = "SteamHandler";
    public const string EpicHandlerName = "EpicHandler";

    public static void AddHttpClients(this IServiceCollection services)
    {
        services.AddHttpClient(HttpClientConfiguration.SteamHandlerName, client =>
        {
            client.BaseAddress = new Uri("https://store.steampowered.com/");
            client.DefaultRequestHeaders.Add("User-Agent", "GameDealWatcher/1.0");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
        services.AddHttpClient(HttpClientConfiguration.EpicHandlerName, client =>
        {
            client.BaseAddress = new Uri("https://store-site-backend-static.ak.epicgames.com/");
            client.DefaultRequestHeaders.Add("User-Agent", "GameDealWatcher/1.0");
            client.Timeout = TimeSpan.FromSeconds(30);
        });
    }
}