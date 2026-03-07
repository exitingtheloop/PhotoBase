using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.PixelFormats;

namespace PhotoBase.Tests;

/// <summary>
/// Helpers for creating test data (images, TSV content, multipart forms).
/// </summary>
internal static class TestData
{
    /// <summary>
    /// Generate a minimal valid JPEG in memory (10×10 red pixel).
    /// Different seed colors produce different SHA-256 hashes.
    /// </summary>
    public static byte[] CreateTestJpeg(byte r = 255, byte g = 0, byte b = 0)
    {
  using var image = new Image<Rgba32>(10, 10, new Rgba32(r, g, b, 255));
        using var ms = new MemoryStream();
  image.SaveAsJpeg(ms, new JpegEncoder { Quality = 90 });
     return ms.ToArray();
    }

    /// <summary>
    /// Create a multipart form with file + metadata fields matching AssetUploadRequest.
    /// </summary>
    public static MultipartFormDataContent CreateUploadForm(
        byte[] imageBytes,
     string fileName = "test-plant.jpg",
      string title = "Test Magnolia",
        string? accessionNumber = null,
   string? keywords = null,
 string? location = null,
        string? photographer = null,
        string? categories = null,
   bool forceOverrideDuplicate = false)
    {
  var form = new MultipartFormDataContent();

      var fileContent = new ByteArrayContent(imageBytes);
      fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/jpeg");
    form.Add(fileContent, "file", fileName);

        form.Add(new StringContent(title), "Title");

if (accessionNumber is not null)
     form.Add(new StringContent(accessionNumber), "AccessionNumber");
  if (keywords is not null)
     form.Add(new StringContent(keywords), "Keywords");
        if (location is not null)
       form.Add(new StringContent(location), "Location");
        if (photographer is not null)
        form.Add(new StringContent(photographer), "Photographer");
        if (categories is not null)
    form.Add(new StringContent(categories), "Categories");
      if (forceOverrideDuplicate)
     form.Add(new StringContent("true"), "ForceOverrideDuplicate");

        return form;
    }

    /// <summary>
    /// Build a TSV file as a multipart form suitable for the import endpoint.
    /// </summary>
    public static MultipartFormDataContent CreateTsvForm(string tsvContent, string fileName = "test.tsv")
    {
  var form = new MultipartFormDataContent();
     var fileContent = new ByteArrayContent(System.Text.Encoding.UTF8.GetBytes(tsvContent));
    fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/tab-separated-values");
      form.Add(fileContent, "file", fileName);
   return form;
 }

    public const string SampleTsv =
        "AccessionNumber\tPlantName\tLocation\tCollectionTrip\n" +
      "TEST-0001\tMagnolia zenii\tGarden A - Section 3\tSpring 2024\n" +
  "TEST-0002\tQuercus alba\tGarden B - Oak Collection\tFall 2023\n" +
        "TEST-0003\tAcer palmatum\tGarden C - Japanese Maples\t\n";

    public const string SampleTsvWithErrors =
     "AccessionNumber\tPlantName\tLocation\n" +
    "TEST-0010\tValid Plant\tValid Location\n" +
        "\tMissing Accession\tSome Location\n" +
      "TEST-0012\t\tSome Location\n";
}
