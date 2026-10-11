using System;
using System.Diagnostics;
using System.IO;

namespace GameDealWatcher.App.Utilities;

internal static class CrashLogger
{
    public static void WriteCrashLog(string tag, Exception ex)
    {
        var msg = $"{DateTimeOffset.Now:o} {tag}{Environment.NewLine}{ex}{Environment.NewLine}{new string('=', 60)}{Environment.NewLine}";
        
        var exeDir = Path.GetDirectoryName(Environment.ProcessPath);
        var exeLogs = string.IsNullOrEmpty(exeDir) ? null : Path.Combine(exeDir, "Logs");
        var appDataLogs = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GameDealWatcher", "Logs");
        
        var written = false;
        foreach (var crashDir in new[] { exeLogs, appDataLogs })
        {
            if (crashDir == null) continue;
            
            try
            {
                Directory.CreateDirectory(crashDir);
                var crashFile = Path.Combine(crashDir, "crash.log");
                File.AppendAllText(crashFile, msg);
                written = true;
                break;
            }
            catch (UnauthorizedAccessException uae)
            {
                Debug.WriteLine($"CrashLogger: UnauthorizedAccessException for {crashDir}: {uae.Message}");
            }
            catch (IOException ioe)
            {
                Debug.WriteLine($"CrashLogger: IOException for {crashDir}: {ioe.Message}");
            }
            catch (Exception writeEx)
            {
                Debug.WriteLine($"CrashLogger: Unexpected error for {crashDir}: {writeEx.Message}");
            }
        }
        
        if (!written)
            Debug.WriteLine("CrashLogger: Could not write crash log to any location");
    }
}