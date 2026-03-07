using System.Net;
using System.Net.Http.Json;
using PhotoBase.Shared.Responses;

namespace PhotoBase.Tests.Integration;

/// <summary>
/// Integration tests for POST /api/import/plant-records.
/// Import is now Admin-only, so tests authenticate as admin.
/// </summary>
public class ImportEndpointTests : IClassFixture<PhotoBaseTestFactory>
{
    private readonly PhotoBaseTestFactory _factory;

    public ImportEndpointTests(PhotoBaseTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Import_ValidTsv_ReturnsExpectedCounts()
    {
        var client = await _factory.CreateAdminClientAsync();
        const string tsv =
        "AccessionNumber\tPlantName\tLocation\tCollectionTrip\n" +
            "VALID-001\tMagnolia zenii\tGarden A\tSpring 2024\n" +
          "VALID-002\tQuercus alba\tGarden B\tFall 2023\n" +
    "VALID-003\tAcer palmatum\tGarden C\t\n";
        using var form = TestData.CreateTsvForm(tsv);

        var response = await client.PostAsync("/api/import/plant-records", form);

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
        var client = await _factory.CreateAdminClientAsync();
        const string tsv =
         "AccessionNumber\tPlantName\tLocation\tCollectionTrip\n" +
    "UPSERT-001\tPlant A\tLocation A\tTrip A\n" +
            "UPSERT-002\tPlant B\tLocation B\tTrip B\n";

        using var form1 = TestData.CreateTsvForm(tsv);
        await client.PostAsync("/api/import/plant-records", form1);

        using var form2 = TestData.CreateTsvForm(tsv);
        var response = await client.PostAsync("/api/import/plant-records", form2);

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
        var client = await _factory.CreateAdminClientAsync();
        using var form = TestData.CreateTsvForm(TestData.SampleTsvWithErrors);

        var response = await client.PostAsync("/api/import/plant-records", form);

        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<ImportSummaryDto>();
        Assert.NotNull(summary);
        Assert.Equal(1, summary.Imported);
        Assert.Equal(2, summary.Errors);
        Assert.Equal(2, summary.ErrorDetails.Count);
        Assert.Contains(summary.ErrorDetails, e => e.Contains("Line 3"));
        Assert.Contains(summary.ErrorDetails, e => e.Contains("Line 4"));
    }

    [Fact]
    public async Task Import_EmptyFile_Returns400()
    {
        var client = await _factory.CreateAdminClientAsync();
        using var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(Array.Empty<byte>());
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/tab-separated-values");
        form.Add(fileContent, "file", "empty.tsv");

        var response = await client.PostAsync("/api/import/plant-records", form);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Import_WrongExtension_Returns400()
    {
        var client = await _factory.CreateAdminClientAsync();
        using var form = TestData.CreateTsvForm(TestData.SampleTsv, fileName: "data.csv");

        var response = await client.PostAsync("/api/import/plant-records", form);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Import_MissingRequiredColumn_ReturnsErrorInSummary()
    {
        var client = await _factory.CreateAdminClientAsync();
        const string badTsv = "AccessionNumber\tPlantName\n" +
   "TEST-X1\tSome Plant\n";
        using var form = TestData.CreateTsvForm(badTsv);

        var response = await client.PostAsync("/api/import/plant-records", form);
        response.EnsureSuccessStatusCode();
        var summary = await response.Content.ReadFromJsonAsync<ImportSummaryDto>();
        Assert.NotNull(summary);
        Assert.True(summary.Errors > 0);
        Assert.Contains(summary.ErrorDetails, e => e.Contains("Location", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Import_Anonymous_Returns401()
    {
        var client = _factory.CreateClient();
        using var form = TestData.CreateTsvForm(TestData.SampleTsv);

        var response = await client.PostAsync("/api/import/plant-records", form);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Import_InternalUser_Returns403()
    {
        var client = await _factory.CreateInternalClientAsync();
        using var form = TestData.CreateTsvForm(TestData.SampleTsv);

        var response = await client.PostAsync("/api/import/plant-records", form);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}
