using GameDealWatcher.Domain.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace GameDealWatcher.Infrastructure.Startup;

public sealed class WindowsStartupService : IStartupService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "GameDealWatcher";

    private readonly ILogger<WindowsStartupService> _logger;

    public WindowsStartupService(ILogger<WindowsStartupService> logger)
    {
        _logger = logger;
    }

    public void SetStartWithWindows(bool enabled)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true)
                ?? Registry.CurrentUser.CreateSubKey(RunKeyPath);

            if (enabled)
            {
                var exePath = Environment.ProcessPath;
                if (string.IsNullOrEmpty(exePath))
                {
                    _logger.LogWarning("Could not determine executable path — skipping startup registration");
                    return;
                }
                key?.SetValue(ValueName, $"\"{exePath}\"");
            }
            else
            {
                key?.DeleteValue(ValueName, throwOnMissingValue: false);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update Windows startup registration");
        }
    }
}
