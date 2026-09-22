using Microsoft.Extensions.Logging;

namespace GameDealWatcher.App;

/// <summary>
/// File logger provider with daily log rotation, persistent StreamWriter,
/// and automatic cleanup of old log files.
/// </summary>
internal sealed class FileLoggerProvider : ILoggerProvider
{
    private readonly string _logDir;
    private readonly object _lock = new();
    private StreamWriter? _writer;
    private string? _currentDate;

    public FileLoggerProvider(string logDir)
    {
        _logDir = logDir;
        Directory.CreateDirectory(_logDir);
        CleanupOldLogs(30);
    }

    public ILogger CreateLogger(string categoryName) => new FileLogger(this);

    public void Dispose()
    {
        lock (_lock)
        {
            _writer?.Dispose();
            _writer = null;
        }
    }

    private void EnsureWriter()
    {
        var today = DateTime.Now.ToString("yyyyMMdd");
        if (_currentDate == today && _writer != null) return;

        _writer?.Dispose();
        _currentDate = today;
        var logFile = Path.Combine(_logDir, $"gamedealwatcher_{today}.log");
        _writer = new StreamWriter(logFile, append: true) { AutoFlush = true };
    }

    private void CleanupOldLogs(int maxAgeDays)
    {
        var cutoff = DateTime.Now.AddDays(-maxAgeDays);
        foreach (var file in Directory.GetFiles(_logDir, "gamedealwatcher_*.log"))
        {
            if (File.GetLastWriteTime(file) < cutoff)
            {
                try { File.Delete(file); } catch { }
            }
        }
    }

    internal void WriteLog(string message)
    {
        lock (_lock)
        {
            EnsureWriter();
            _writer!.WriteLine(message);
        }
    }

    private sealed class FileLogger : ILogger
    {
        private readonly FileLoggerProvider _provider;

        public FileLogger(FileLoggerProvider provider) => _provider = provider;

        /// <summary>
        /// Scopes are not supported by this file logger.
        /// Always returns null — scoped logging is silently ignored.
        /// </summary>
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel.Information;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(logLevel)) return;
            var message = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{logLevel}] {formatter(state, exception)}";
            if (exception != null) message += Environment.NewLine + exception;
            _provider.WriteLog(message);
        }
    }
}
