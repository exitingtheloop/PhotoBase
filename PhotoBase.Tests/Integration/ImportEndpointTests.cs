using System.Net;
using System.Net.Http.Json;
using PhotoBase.Shared.Responses;

namespace PhotoBase.Tests.Integration;

/// <summary>
/// Integration tests for POST /api/import/plant-records.
/// </summary>
public class ImportEndpointTests : IClassFixture<PhotoBaseTestFactory>
{
    private readonly HttpClient _client;

    public ImportEndpointTests(PhotoBaseTestFactory factory)
    {
    _client = factory.CreateClient();
    }

    [Fact]
    public async Task Import_ValidTsv_ReturnsExpectedCounts()
    {
        // Arrange — use unique accession numbers so no prior test interferes
        const string tsv =
        "AccessionNumber\tPlantName\tLocation\tCollectionTrip\n" +
            "VALID-001\tMagnolia zenii\tGarden A\tSpring 2024\n" +
  "VALID-002\tQuercus alba\tGarden B\tFall 2023\n" +
  "VALID-003\tAcer palmatum\tGarden C\t\n";
        using var form = TestData.CreateTsvForm(tsv);

        // Act
        var response = await _client.PostAsync("/api/import/plant-records", form);

   // Assert
  response.EnsureSuccessStatusCode();
  var summary = await response.Content.ReadFromJsonAsync<ImportSummaryDto>();

        Assert.NotNull(summary);
        Assert.Equal(3, summary.Imported);
     Assert.Equal(0, summary.Updated);
     Assert.Equal(0, summary.Errors);
      Assert.Empty(summary.ErrorDetails);
    }

    [Fact]
    public async Task Import_UpsertExisting_UpdatesRecords()
    {
     // Arrange — import unique records first
        const string tsv =
        "AccessionNumber\tPlantName\tLocation\tCollectionTrip\n" +
     "UPSERT-001\tPlant A\tLocation A\tTrip A\n" +
            "UPSERT-002\tPlant B\tLocation B\tTrip B\n";
        using var form1 = TestData.CreateTsvForm(tsv);
        await _client.PostAsync("/api/import/plant-records", form1);

        // Act — import same accessions again (should upsert)
        using var form2 = TestData.CreateTsvForm(tsv);
        var response = await _client.PostAsync("/api/import/plant-records", form2);

// Assert
response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<ImportSummaryDto>();

        Assert.NotNull(summary);
Assert.Equal(0, summary.Imported);
        Assert.Equal(2, summary.Updated);
        Assert.Equal(0, summary.Errors);
    }

    [Fact]
    public async Task Import_RowsWithErrors_ReportsLineLevel()
    {
        // Arrange
  using var form = TestData.CreateTsvForm(TestData.SampleTsvWithErrors);

        // Act
     var response = await _client.PostAsync("/api/import/plant-records", form);

        // Assert
      response.EnsureSuccessStatusCode();
     var summary = await response.Content.ReadFromJsonAsync<ImportSummaryDto>();

     Assert.NotNull(summary);
    Assert.Equal(1, summary.Imported); // TEST-0010
     Assert.Equal(2, summary.Errors);   // line 3 (empty accession), line 4 (empty plantname)
    Assert.Equal(2, summary.ErrorDetails.Count);
        Assert.Contains(summary.ErrorDetails, e => e.Contains("Line 3"));
     Assert.Contains(summary.ErrorDetails, e => e.Contains("Line 4"));
  }

    [Fact]
 public async Task Import_EmptyFile_Returns400()
    {
     // Arrange — empty form with no file
        using var form = new MultipartFormDataContent();
  var fileContent = new ByteArrayContent(Array.Empty<byte>());
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/tab-separated-values");
        form.Add(fileContent, "file", "empty.tsv");

    // Act
        var response = await _client.PostAsync("/api/import/plant-records", form);

   // Assert — the controller should return a problem or the service returns error in summary
        // With zero-length file, the controller returns 400
     Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Import_WrongExtension_Returns400()
    {
        // Arrange
     using var form = TestData.CreateTsvForm(TestData.SampleTsv, fileName: "data.csv");

     // Act
      var response = await _client.PostAsync("/api/import/plant-records", form);

  // Assert
  Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
  }

    [Fact]
    public async Task Import_MissingRequiredColumn_ReturnsErrorInSummary()
    {
    // Arrange — TSV missing Location column
     const string badTsv = "AccessionNumber\tPlantName\n" +
      "TEST-X1\tSome Plant\n";
        using var form = TestData.CreateTsvForm(badTsv);

      // Act
     var response = await _client.PostAsync("/api/import/plant-records", form);

        // Assert
        response.EnsureSuccessStatusCode(); // 200 with error in body
        var summary = await response.Content.ReadFromJsonAsync<ImportSummaryDto>();

        Assert.NotNull(summary);
        Assert.True(summary.Errors > 0);
      Assert.Contains(summary.ErrorDetails, e => e.Contains("Location", StringComparison.OrdinalIgnoreCase));
    }
}
