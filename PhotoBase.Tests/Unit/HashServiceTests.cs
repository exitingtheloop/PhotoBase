using PhotoBase.API.Services;

namespace PhotoBase.Tests.Unit;

/// <summary>
/// Unit tests for Sha256HashService.
/// </summary>
public class HashServiceTests
{
 private readonly IHashService _sut = new Sha256HashService();

    [Fact]
    public async Task ComputeSha256_ReturnsLowercaseHex64Chars()
    {
    var bytes = new byte[] { 1, 2, 3, 4, 5 };
        using var stream = new MemoryStream(bytes);

  var hash = await _sut.ComputeSha256Async(stream);

     Assert.NotNull(hash);
Assert.Equal(64, hash.Length);
Assert.Equal(hash, hash.ToLowerInvariant()); // must be lowercase
    Assert.Matches("^[0-9a-f]{64}$", hash);
 }

    [Fact]
    public async Task ComputeSha256_SameInput_SameHash()
    {
        var bytes = new byte[] { 10, 20, 30 };

    using var s1 = new MemoryStream(bytes);
using var s2 = new MemoryStream(bytes);

        var h1 = await _sut.ComputeSha256Async(s1);
   var h2 = await _sut.ComputeSha256Async(s2);

        Assert.Equal(h1, h2);
    }

    [Fact]
    public async Task ComputeSha256_DifferentInput_DifferentHash()
    {
     using var s1 = new MemoryStream(new byte[] { 1 });
  using var s2 = new MemoryStream(new byte[] { 2 });

   var h1 = await _sut.ComputeSha256Async(s1);
      var h2 = await _sut.ComputeSha256Async(s2);

Assert.NotEqual(h1, h2);
    }

    [Fact]
 public async Task ComputeSha256_ResetsStreamPosition()
    {
       var bytes = new byte[] { 1, 2, 3 };
    using var stream = new MemoryStream(bytes);
        stream.Position = 0;

     await _sut.ComputeSha256Async(stream);

     Assert.Equal(0, stream.Position);
    }
}
