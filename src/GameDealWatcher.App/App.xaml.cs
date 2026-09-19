using GameDealWatcher.App.ViewModels;
using GameDealWatcher.App.Views;
using GameDealWatcher.Application.Services;
using GameDealWatcher.Domain.Interfaces;
using GameDealWatcher.Infrastructure.Database;
using GameDealWatcher.Infrastructure.Http;
using GameDealWatcher.Infrastructure.Images;
using GameDealWatcher.Infrastructure.Notifications;
using GameDealWatcher.Infrastructure.Providers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace GameDealWatcher.App;

public partial class App : Microsoft.UI.Xaml.Application
{
    private IServiceProvider? _serviceProvider;

    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs e)
    {
        base.OnLaunched(e);
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        var connectionString = GetConnectionString();
        _ = InitializeAsync(connectionString);

        var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
        mainWindow.Activate();
    }

    private async Task InitializeAsync(string connectionString)
    {
        await DatabaseInitializer.InitializeAsync(connectionString, CancellationToken.None);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder => builder.AddConsole().SetMinimumLevel(LogLevel.Debug));

        var connectionString = GetConnectionString();
        services.AddSingleton<IGameDealRepository>(_ => new SqliteGameDealRepository(connectionString));
        services.AddSingleton<ISettingsRepository>(_ => new JsonSettingsRepository(GetSettingsPath()));

        services.AddHttpClients();

        services.AddSingleton<SteamProvider>();
        services.AddSingleton<EpicGamesProvider>();

        services.AddSingleton<IImageCacheService, ImageCacheService>();
        services.AddSingleton<INotificationService>(_ => new WindowsNotificationService(_.GetRequiredService<ILogger<WindowsNotificationService>>()));
        services.AddSingleton<IGameDealService, GameDealService>();

        services.AddSingleton<DashboardViewModel>();
        services.AddSingleton<EpicViewModel>();
        services.AddSingleton<SteamViewModel>();
        services.AddSingleton<SettingsViewModel>();
        services.AddSingleton<MainViewModel>();

        services.AddSingleton<MainWindow>();
        services.AddSingleton<DashboardView>();
        services.AddSingleton<EpicView>();
        services.AddSingleton<SteamView>();
        services.AddSingleton<SettingsView>();
    }

    private static string GetConnectionString()
    {
        var dbDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GameDealWatcher", "Database");
        Directory.CreateDirectory(dbDir);
        var dbPath = Path.Combine(dbDir, "game_deals.db");
        return $"Data Source={dbPath}";
    }

    private static string GetSettingsPath()
    {
        var settingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GameDealWatcher");
        Directory.CreateDirectory(settingsDir);
        return Path.Combine(settingsDir, "settings.json");
    }
}