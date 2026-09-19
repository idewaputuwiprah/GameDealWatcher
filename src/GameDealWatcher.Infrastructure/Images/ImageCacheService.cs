using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;

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
        var filePath = Path.Combine(_cacheDirectory, fileName);

        if (File.Exists(filePath)) return filePath;

        try
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync(imageUrl, ct);
            if (!response.IsSuccessStatusCode) return null;

            await using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None);
            await response.Content.CopyToAsync(fs, ct);
            return filePath;
        }
        catch (Exception)
        {
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
        using var sha256 = SHA256.Create();
        var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(bytes);
    }
}