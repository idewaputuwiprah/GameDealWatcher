using System.Net.Http;
using GameDealWatcher.Infrastructure.Images;
using Moq;
using Xunit;

namespace GameDealWatcher.Tests.Infrastructure.Images;

public class ImageCacheServiceTests
{
    [Fact]
    public void Constructor_PrunesFilesOlderThan30Days()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"gdw-cache-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var oldFile = Path.Combine(directory, "old.jpg");
        var freshFile = Path.Combine(directory, "fresh.jpg");
        File.WriteAllText(oldFile, "x");
        File.WriteAllText(freshFile, "x");
        File.SetLastWriteTimeUtc(oldFile, DateTime.UtcNow.AddDays(-31));

        try
        {
            _ = new ImageCacheService(new Mock<IHttpClientFactory>().Object, directory);

            Assert.False(File.Exists(oldFile));
            Assert.True(File.Exists(freshFile));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
