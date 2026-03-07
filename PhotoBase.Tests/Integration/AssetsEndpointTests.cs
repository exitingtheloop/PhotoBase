using System.Net;
using System.Net.Http.Json;
using PhotoBase.Shared.Dtos;
using PhotoBase.Shared.Responses;

namespace PhotoBase.Tests.Integration;

/// <summary>
/// Integration tests for /api/assets endpoints.
/// Tests run in order within a single factory instance using IClassFixture.
/// Each test is independent — uploads its own data so ordering doesn't matter.
/// </summary>
public class AssetsEndpointTests : IClassFixture<PhotoBaseTestFactory>
{
    private readonly HttpClient _client;

    public AssetsEndpointTests(PhotoBaseTestFactory factory)
    {
  _client = factory.CreateClient();
 }

    // ??? Upload ?????????????????????????????????????????????

    [Fact]
    public async Task Upload_ValidImage_Returns201WithDetail()
    {
    // Arrange
    var jpeg = TestData.CreateTestJpeg(r: 100, g: 50, b: 25);
    using var form = TestData.CreateUploadForm(jpeg,
   title: "Upload Test Plant",
            keywords: "test,upload",
     location: "Lab A");

    // Act
    var response = await _client.PostAsync("/api/assets", form);

        // Assert
Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var detail = await response.Content.ReadFromJsonAsync<AssetDetailDto>();
 Assert.NotNull(detail);
Assert.Equal("Upload Test Plant", detail.Title);
Assert.Equal("Lab A", detail.Location);
Assert.NotEqual(Guid.Empty, detail.Id);
        Assert.Contains("/api/assets/", detail.ThumbUrl);

   // Location header should point to the detail endpoint
     Assert.NotNull(response.Headers.Location);
        Assert.Contains(detail.Id.ToString(), response.Headers.Location.ToString());
    }

    [Fact]
    public async Task Upload_WithAccessionNumber_AutoFillsFromPlantRecord()
    {
      // Arrange — first import a plant record
       const string tsv = "AccessionNumber\tPlantName\tLocation\tCollectionTrip\n" +
      "AUTOFILL-001\tRosa canina\tGarden Z - Rose Walk\tSpring 2024\n";
        using var importForm = TestData.CreateTsvForm(tsv);
    await _client.PostAsync("/api/import/plant-records", importForm);

        // Upload image with that accession, but no location specified
     var jpeg = TestData.CreateTestJpeg(r: 10, g: 20, b: 30);
      using var form = TestData.CreateUploadForm(jpeg,
   title: "Auto-fill Test",
     accessionNumber: "AUTOFILL-001");

    // Act
   var response = await _client.PostAsync("/api/assets", form);

  // Assert
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<AssetDetailDto>();
     Assert.NotNull(detail);
  Assert.Equal("AUTOFILL-001", detail.AccessionNumber);
     Assert.Equal("Rosa canina", detail.PlantName);
  // Location should be auto-filled from PlantRecord
   Assert.Equal("Garden Z - Rose Walk", detail.Location);
    }

    [Fact]
    public async Task Upload_DuplicateHash_Returns409()
    {
     // Arrange — upload same file twice with SAME bytes
        var jpeg = TestData.CreateTestJpeg(r: 200, g: 200, b: 200);

        using var form1 = TestData.CreateUploadForm(jpeg, title: "First Upload");
        var first = await _client.PostAsync("/api/assets", form1);
    first.EnsureSuccessStatusCode();

    // Act — same bytes, different metadata
   using var form2 = TestData.CreateUploadForm(jpeg, title: "Duplicate Upload");
  var second = await _client.PostAsync("/api/assets", form2);

    // Assert
    Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Upload_DuplicateWithForceFlag_Returns201()
 {
   // Arrange
var jpeg = TestData.CreateTestJpeg(r: 150, g: 150, b: 150);

  using var form1 = TestData.CreateUploadForm(jpeg, title: "Original");
 var first = await _client.PostAsync("/api/assets", form1);
        first.EnsureSuccessStatusCode();

     // Act — force override
    using var form2 = TestData.CreateUploadForm(jpeg, title: "Force Override", forceOverrideDuplicate: true);
        var second = await _client.PostAsync("/api/assets", form2);

       // Assert
     Assert.Equal(HttpStatusCode.Created, second.StatusCode);
    }

    [Fact]
    public async Task Upload_InvalidExtension_Returns400()
    {
  // Arrange
     var form = new MultipartFormDataContent();
  var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
   fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        form.Add(fileContent, "file", "malware.exe");
   form.Add(new StringContent("Bad File"), "Title");

   // Act
    var response = await _client.PostAsync("/api/assets", form);

    // Assert
   Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ??? Search ?????????????????????????????????????????????

    [Fact]
    public async Task Search_NoQuery_ReturnsPagedResult()
    {
     // Arrange — upload something so there's data
    var jpeg = TestData.CreateTestJpeg(r: 1, g: 2, b: 3);
     using var form = TestData.CreateUploadForm(jpeg, title: "Searchable Plant", keywords: "magnolia,search-test");
      await _client.PostAsync("/api/assets", form);

     // Act
      var response = await _client.GetAsync("/api/assets?page=1&pageSize=10");

     // Assert
     response.EnsureSuccessStatusCode();
    var result = await response.Content.ReadFromJsonAsync<PagedResult<AssetListItemDto>>();
     Assert.NotNull(result);
      Assert.True(result.TotalCount >= 1);
        Assert.NotEmpty(result.Items);
     Assert.Equal(1, result.Page);
    }

    [Fact]
    public async Task Search_ByKeyword_FiltersResults()
    {
        // Arrange
       var jpeg = TestData.CreateTestJpeg(r: 5, g: 10, b: 15);
     using var form = TestData.CreateUploadForm(jpeg,
            title: "Keyword Search Subject",
       keywords: "unique-keyword-xyz");
      await _client.PostAsync("/api/assets", form);

        // Act
        var response = await _client.GetAsync("/api/assets?query=unique-keyword-xyz");

        // Assert
response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<AssetListItemDto>>();
    Assert.NotNull(result);
     Assert.True(result.TotalCount >= 1);
        Assert.All(result.Items, item =>
            Assert.Contains("unique-keyword-xyz", item.Keywords ?? "", StringComparison.OrdinalIgnoreCase));
  }

    [Fact]
    public async Task Search_ByPlantName_FindsViaJoin()
    {
  // Arrange — import a plant record, then upload an image linked to it
        const string tsv = "AccessionNumber\tPlantName\tLocation\n" +
          "SEARCH-001\tBetula pendula\tGarden X\n";
        using var importForm = TestData.CreateTsvForm(tsv);
await _client.PostAsync("/api/import/plant-records", importForm);

        var jpeg = TestData.CreateTestJpeg(r: 20, g: 40, b: 60);
 using var form = TestData.CreateUploadForm(jpeg,
    title: "Birch Photo",
       accessionNumber: "SEARCH-001");
     await _client.PostAsync("/api/assets", form);

  // Act — search by plant name
      var response = await _client.GetAsync("/api/assets?query=Betula");

   // Assert
  response.EnsureSuccessStatusCode();
     var result = await response.Content.ReadFromJsonAsync<PagedResult<AssetListItemDto>>();
  Assert.NotNull(result);
  Assert.True(result.TotalCount >= 1);
        Assert.Contains(result.Items, item => item.PlantName?.Contains("Betula") == true);
    }

    // ??? Detail ?????????????????????????????????????????????

    [Fact]
    public async Task GetDetail_ExistingAsset_ReturnsFullDto()
    {
    // Arrange
        var jpeg = TestData.CreateTestJpeg(r: 30, g: 60, b: 90);
     using var form = TestData.CreateUploadForm(jpeg, title: "Detail Test", photographer: "Jane Doe");
    var uploadResponse = await _client.PostAsync("/api/assets", form);
        var uploaded = await uploadResponse.Content.ReadFromJsonAsync<AssetDetailDto>();

        // Act
    var response = await _client.GetAsync($"/api/assets/{uploaded!.Id}");

        // Assert
  response.EnsureSuccessStatusCode();
        var detail = await response.Content.ReadFromJsonAsync<AssetDetailDto>();
     Assert.NotNull(detail);
     Assert.Equal(uploaded.Id, detail.Id);
Assert.Equal("Detail Test", detail.Title);
    Assert.Equal("Jane Doe", detail.Photographer);
        Assert.Equal("test-plant.jpg", detail.OriginalFileName);
  }

    [Fact]
    public async Task GetDetail_NonExistent_Returns404()
    {
   var response = await _client.GetAsync($"/api/assets/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ??? Thumbnail ??????????????????????????????????????????

  [Fact]
  public async Task GetThumbnail_ReturnsJpegImage()
    {
        // Arrange
        var jpeg = TestData.CreateTestJpeg(r: 40, g: 80, b: 120);
     using var form = TestData.CreateUploadForm(jpeg, title: "Thumb Test");
        var uploadResponse = await _client.PostAsync("/api/assets", form);
     var uploaded = await uploadResponse.Content.ReadFromJsonAsync<AssetDetailDto>();

        // Act
     var response = await _client.GetAsync($"/api/assets/{uploaded!.Id}/thumb");

     // Assert
     response.EnsureSuccessStatusCode();
   Assert.Equal("image/jpeg", response.Content.Headers.ContentType?.MediaType);
      var bytes = await response.Content.ReadAsByteArrayAsync();
      Assert.True(bytes.Length > 0);
       // JPEG magic bytes: FF D8
     Assert.Equal(0xFF, bytes[0]);
   Assert.Equal(0xD8, bytes[1]);
    }

    [Fact]
    public async Task GetThumbnail_NonExistent_Returns404()
  {
     var response = await _client.GetAsync($"/api/assets/{Guid.NewGuid()}/thumb");
 Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ??? Download ???????????????????????????????????????????

  [Fact]
  public async Task Download_ReturnsOriginalFile()
    {
    // Arrange
  var jpeg = TestData.CreateTestJpeg(r: 50, g: 100, b: 150);
    using var form = TestData.CreateUploadForm(jpeg, title: "Download Test");
     var uploadResponse = await _client.PostAsync("/api/assets", form);
     var uploaded = await uploadResponse.Content.ReadFromJsonAsync<AssetDetailDto>();

        // Act
    var response = await _client.GetAsync($"/api/assets/{uploaded!.Id}/download");

  // Assert
     response.EnsureSuccessStatusCode();
   Assert.Equal("image/jpeg", response.Content.Headers.ContentType?.MediaType);
   var bytes = await response.Content.ReadAsByteArrayAsync();
   Assert.True(bytes.Length > 0);
     // Should have Content-Disposition header for download
      Assert.NotNull(response.Content.Headers.ContentDisposition);
      Assert.Equal("attachment", response.Content.Headers.ContentDisposition.DispositionType);
    }

    [Fact]
    public async Task Download_NonExistent_Returns404()
    {
 var response = await _client.GetAsync($"/api/assets/{Guid.NewGuid()}/download");
    Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
