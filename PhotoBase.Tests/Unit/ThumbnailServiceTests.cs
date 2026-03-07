using PhotoBase.API.Services;

namespace PhotoBase.Tests.Unit;

/// <summary>
/// Unit tests for ImageSharpThumbnailService.
/// </summary>
public class ThumbnailServiceTests
{
private readonly IThumbnailService _sut = new ImageSharpThumbnailService();

 [Fact]
    public async Task GenerateThumbnail_ReturnsValidJpeg()
    {
     // Arrange — create a small test image
   var sourceBytes = TestData.CreateTestJpeg();
using var source = new MemoryStream(sourceBytes);

        // Act
        using var thumb = await _sut.GenerateThumbnailAsync(source);

        // Assert
Assert.NotNull(thumb);
     Assert.True(thumb.Length > 0);

    // Read first 2 bytes — JPEG magic
      var buffer = new byte[2];
   await thumb.ReadAsync(buffer);
     Assert.Equal(0xFF, buffer[0]);
    Assert.Equal(0xD8, buffer[1]);
    }

    [Fact]
    public async Task GenerateThumbnail_ResetsSourcePosition()
    {
    var sourceBytes = TestData.CreateTestJpeg();
    using var source = new MemoryStream(sourceBytes);

 using var _ = await _sut.GenerateThumbnailAsync(source);

    Assert.Equal(0, source.Position);
    }
}
