using GameDealWatcher.Infrastructure.Startup;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Win32;
using Xunit;

namespace GameDealWatcher.Tests.Infrastructure.Startup;

public class WindowsStartupServiceTests
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "GameDealWatcher";

    [Fact]
    public void SetStartWithWindows_True_WritesRunKeyValue()
    {
        var service = new WindowsStartupService(NullLogger<WindowsStartupService>.Instance);
        try
        {
            service.SetStartWithWindows(true);

            using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
            var value = key?.GetValue(ValueName) as string;

            Assert.NotNull(value);
            Assert.Contains(Environment.ProcessPath!, value);
        }
        finally
        {
            service.SetStartWithWindows(false);
        }
    }

    [Fact]
    public void SetStartWithWindows_False_RemovesRunKeyValue()
    {
        var service = new WindowsStartupService(NullLogger<WindowsStartupService>.Instance);
        service.SetStartWithWindows(true);

        service.SetStartWithWindows(false);

        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath);
        Assert.Null(key?.GetValue(ValueName));
    }
}
