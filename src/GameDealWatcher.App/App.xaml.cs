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

    protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs e)
    {
        try
        {
            // Bootstrap the Windows App SDK for unpackaged, self-contained execution.
            // This MUST be called before any WinUI types are used.
            try
            {
                Microsoft.Windows.ApplicationModel.WindowsAppRuntime.Bootstrap.Initialize();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Bootstrap failed: {ex}");
                // Bootstrap failure is fatal — the app cannot run without the Windows App SDK runtime.
                throw;
            }

            base.OnLaunched(e);
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();

            var connectionString = GetConnectionString();
            try
            {
                await InitializeAsync(connectionString);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Database initialization failed: {ex}");
                var errorWindow = _serviceProvider.GetRequiredService<MainWindow>();
                errorWindow.Activate();
                var dialog = new Microsoft.UI.Xaml.Controls.ContentDialog
                {
                    XamlRoot = errorWindow.Content.XamlRoot,
                    Title = "Initialization Failed",
                    Content = $"The application could not initialize its database: {ex.Message}",
                    CloseButtonText = "Exit"
                };
                await dialog.ShowAsync();
                return;
            }

            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.Activate();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Fatal startup error: {ex}");
            // Write a crash log to a known location so the user can report the issue
            try
            {
                var crashDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "GameDealWatcher", "Logs");
                Directory.CreateDirectory(crashDir);
                var crashFile = Path.Combine(crashDir, "crash.log");
                File.AppendAllText(crashFile,
                    $"{DateTimeOffset.Now:o} FATAL STARTUP ERROR{Environment.NewLine}{ex}{Environment.NewLine}{new string('=', 60)}{Environment.NewLine}");
            }
            catch { }
            Environment.Exit(1);
        }
    }

    private async Task InitializeAsync(string connectionString)
    {
        await DatabaseInitializer.InitializeAsync(connectionString, CancellationToken.None);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddLogging(builder =>
        {
            builder.AddConsole().SetMinimumLevel(LogLevel.Debug);
            builder.AddDebug();
            // File logging for diagnostics
            var logDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "GameDealWatcher", "Logs");
            builder.AddProvider(new FileLoggerProvider(logDir));
        });

        var connectionString = GetConnectionString();
        services.AddSingleton<IGameDealRepository>(sp => new SqliteGameDealRepository(connectionString, sp.GetService<ILogger<SqliteGameDealRepository>>()));
        services.AddSingleton<ISettingsRepository>(_ => new JsonSettingsRepository(GetSettingsPath()));

        services.AddHttpClients();

        services.AddSingleton<IGameDealProvider, SteamProvider>();
        services.AddSingleton<IGameDealProvider, EpicGamesProvider>();

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