using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using GameDealWatcher.Infrastructure.Http;

namespace GameDealWatcher.Infrastructure.Images;

public interface IImageCacheService
{
    Task<string?> GetOrDownloadImageAsync(string? imageUrl, CancellationToken ct);
    Task ClearCacheAsync();
}

public sealed class ImageCacheService : IImageCacheService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _cacheDirectory;

    public ImageCacheService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        _cacheDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "GameDealWatcher", "Cache", "Images");
        Directory.CreateDirectory(_cacheDirectory);
    }

    public async Task<string?> GetOrDownloadImageAsync(string? imageUrl, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(imageUrl)) return null;

        var fileName = HashString(imageUrl);
        // Extract extension from URL for proper file type recognition
        var ext = ".jpg";
        if (Uri.TryCreate(imageUrl, UriKind.Absolute, out var uri))
        {
            var urlExt = Path.GetExtension(uri.AbsolutePath);
            if (!string.IsNullOrEmpty(urlExt)) ext = urlExt;
        }
        fileName += ext;
        var filePath = Path.Combine(_cacheDirectory, fileName);

        if (File.Exists(filePath)) return filePath;

        var tmpPath = filePath + ".tmp";
        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientConfiguration.ImageCacheHandlerName);
            var response = await client.GetAsync(imageUrl, ct);
            if (!response.IsSuccessStatusCode) return null;

            // Download to a temp file first, then atomically rename to avoid
            // serving a corrupt partial file if the process crashes mid-download.
            await using var fs = new FileStream(tmpPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await response.Content.CopyToAsync(fs, ct);
            await fs.FlushAsync(ct);
            fs.Close();
            File.Move(tmpPath, filePath, overwrite: true);
            return filePath;
        }
        catch (OperationCanceledException) { throw; }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to download image {imageUrl}: {ex.Message}");
            // Clean up partial temp file if download failed
            try { if (File.Exists(tmpPath)) File.Delete(tmpPath); } catch { }
            return null;
        }
    }

    public async Task ClearCacheAsync()
    {
        await Task.Run(() =>
        {
            if (Directory.Exists(_cacheDirectory))
                Directory.Delete(_cacheDirectory, true);
            Directory.CreateDirectory(_cacheDirectory);
        });
    }

    private static string HashString(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}