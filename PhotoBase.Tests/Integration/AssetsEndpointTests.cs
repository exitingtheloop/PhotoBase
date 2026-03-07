using System.Net;
using System.Net.Http.Json;
using PhotoBase.Shared.Dtos;
using PhotoBase.Shared.Responses;

namespace PhotoBase.Tests.Integration;

/// <summary>
/// Integration tests for /api/assets endpoints.
/// Upload/import are admin-only; download is auth-required; search/detail/thumb are public.
/// </summary>
public class AssetsEndpointTests : IClassFixture<PhotoBaseTestFactory>
{
    private readonly PhotoBaseTestFactory _factory;
    private readonly HttpClient _anonClient;

    public AssetsEndpointTests(PhotoBaseTestFactory factory)
    {
        _factory = factory;
        _anonClient = factory.CreateClient();
    }

    // Helper: upload via admin client, return detail
    private async Task<(HttpClient admin, AssetDetailDto detail)> UploadAsAdminAsync(
      byte[]? jpeg = null, string title = "Test", string? accessionNumber = null,
     string? keywords = null, string? location = null, string? photographer = null,
  bool forceOverride = false)
    {
        var client = await _factory.CreateAdminClientAsync();
        jpeg ??= TestData.CreateTestJpeg();
        using var form = TestData.CreateUploadForm(jpeg, title: title,
     accessionNumber: accessionNumber, keywords: keywords,
     location: location, photographer: photographer,
  forceOverrideDuplicate: forceOverride);
        var resp = await client.PostAsync("/api/assets", form);
        resp.EnsureSuccessStatusCode();
        var detail = await resp.Content.ReadFromJsonAsync<AssetDetailDto>();
        return (client, detail!);
    }

    // ??? Upload (Admin) ?????????????????????????????????

    [Fact]
    public async Task Upload_ValidImage_Returns201WithDetail()
    {
        var jpeg = TestData.CreateTestJpeg(r: 100, g: 50, b: 25);
        var (_, detail) = await UploadAsAdminAsync(jpeg, title: "Upload Test Plant",
       keywords: "test,upload", location: "Lab A");

        Assert.Equal("Upload Test Plant", detail.Title);
        Assert.Equal("Lab A", detail.Location);
        Assert.NotEqual(Guid.Empty, detail.Id);
        Assert.Contains("/api/assets/", detail.ThumbUrl);
    }

    [Fact]
    public async Task Upload_WithAccessionNumber_AutoFillsFromPlantRecord()
    {
        var admin = await _factory.CreateAdminClientAsync();
        const string tsv = "AccessionNumber\tPlantName\tLocation\tCollectionTrip\n" +
  "AUTOFILL-001\tRosa canina\tGarden Z - Rose Walk\tSpring 2024\n";
        using var importForm = TestData.CreateTsvForm(tsv);
        await admin.PostAsync("/api/import/plant-records", importForm);

        var jpeg = TestData.CreateTestJpeg(r: 10, g: 20, b: 30);
        using var form = TestData.CreateUploadForm(jpeg,
   title: "Auto-fill Test", accessionNumber: "AUTOFILL-001");
        var response = await admin.PostAsync("/api/assets", form);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<AssetDetailDto>();
        Assert.NotNull(detail);
        Assert.Equal("AUTOFILL-001", detail.AccessionNumber);
        Assert.Equal("Rosa canina", detail.PlantName);
        Assert.Equal("Garden Z - Rose Walk", detail.Location);
    }

    [Fact]
    public async Task Upload_DuplicateHash_Returns409()
    {
        var admin = await _factory.CreateAdminClientAsync();
        var jpeg = TestData.CreateTestJpeg(r: 200, g: 200, b: 200);

        using var form1 = TestData.CreateUploadForm(jpeg, title: "First Upload");
        await admin.PostAsync("/api/assets", form1);

        using var form2 = TestData.CreateUploadForm(jpeg, title: "Duplicate Upload");
        var second = await admin.PostAsync("/api/assets", form2);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Upload_DuplicateWithForceFlag_Returns201()
    {
        var admin = await _factory.CreateAdminClientAsync();
        var jpeg = TestData.CreateTestJpeg(r: 150, g: 150, b: 150);

        using var form1 = TestData.CreateUploadForm(jpeg, title: "Original");
        await admin.PostAsync("/api/assets", form1);

        using var form2 = TestData.CreateUploadForm(jpeg, title: "Force Override", forceOverrideDuplicate: true);
        var second = await admin.PostAsync("/api/assets", form2);
        Assert.Equal(HttpStatusCode.Created, second.StatusCode);
    }

    [Fact]
    public async Task Upload_InvalidExtension_Returns400()
    {
        var admin = await _factory.CreateAdminClientAsync();
        var form = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[] { 1, 2, 3, 4 });
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");
        form.Add(fileContent, "file", "malware.exe");
        form.Add(new StringContent("Bad File"), "Title");

        var response = await admin.PostAsync("/api/assets", form);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Upload_Anonymous_Returns401()
    {
        var jpeg = TestData.CreateTestJpeg(r: 99, g: 99, b: 99);
        using var form = TestData.CreateUploadForm(jpeg, title: "Anon Upload");
        var response = await _anonClient.PostAsync("/api/assets", form);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Upload_InternalUser_Returns403()
    {
        var client = await _factory.CreateInternalClientAsync();
        var jpeg = TestData.CreateTestJpeg(r: 88, g: 88, b: 88);
        using var form = TestData.CreateUploadForm(jpeg, title: "Internal Upload");
        var response = await client.PostAsync("/api/assets", form);
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ??? Search (Public) ????????????????????????????????

    [Fact]
    public async Task Search_NoQuery_ReturnsPagedResult()
    {
        await UploadAsAdminAsync(TestData.CreateTestJpeg(r: 1, g: 2, b: 3),
            title: "Searchable Plant", keywords: "magnolia,search-test");

        var response = await _anonClient.GetAsync("/api/assets?page=1&pageSize=10");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<AssetListItemDto>>();
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
        Assert.NotEmpty(result.Items);
    }

    [Fact]
    public async Task Search_ByKeyword_FiltersResults()
    {
        await UploadAsAdminAsync(TestData.CreateTestJpeg(r: 5, g: 10, b: 15),
         title: "Keyword Search Subject", keywords: "unique-keyword-xyz");

        var response = await _anonClient.GetAsync("/api/assets?query=unique-keyword-xyz");
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
        var admin = await _factory.CreateAdminClientAsync();
        const string tsv = "AccessionNumber\tPlantName\tLocation\n" +
    "SEARCH-001\tBetula pendula\tGarden X\n";
        using var importForm = TestData.CreateTsvForm(tsv);
        await admin.PostAsync("/api/import/plant-records", importForm);

        var jpeg = TestData.CreateTestJpeg(r: 20, g: 40, b: 60);
        using var form = TestData.CreateUploadForm(jpeg,
          title: "Birch Photo", accessionNumber: "SEARCH-001");
        await admin.PostAsync("/api/assets", form);

        var response = await _anonClient.GetAsync("/api/assets?query=Betula");
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<PagedResult<AssetListItemDto>>();
        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
        Assert.Contains(result.Items, item => item.PlantName?.Contains("Betula") == true);
    }

    // ??? Detail (Public) ????????????????????????????????

    [Fact]
    public async Task GetDetail_ExistingAsset_ReturnsFullDto()
    {
        var (_, uploaded) = await UploadAsAdminAsync(
                TestData.CreateTestJpeg(r: 30, g: 60, b: 90),
           title: "Detail Test", photographer: "Jane Doe");

        var response = await _anonClient.GetAsync($"/api/assets/{uploaded.Id}");
        response.EnsureSuccessStatusCode();
        var detail = await response.Content.ReadFromJsonAsync<AssetDetailDto>();
        Assert.NotNull(detail);
        Assert.Equal(uploaded.Id, detail.Id);
        Assert.Equal("Detail Test", detail.Title);
        Assert.Equal("Jane Doe", detail.Photographer);
    }

    [Fact]
    public async Task GetDetail_NonExistent_Returns404()
    {
        var response = await _anonClient.GetAsync($"/api/assets/{Guid.NewGuid()}");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ??? Thumbnail (Public) ?????????????????????????????

    [Fact]
    public async Task GetThumbnail_ReturnsJpegImage()
    {
        var (_, uploaded) = await UploadAsAdminAsync(
       TestData.CreateTestJpeg(r: 40, g: 80, b: 120), title: "Thumb Test");

        var response = await _anonClient.GetAsync($"/api/assets/{uploaded.Id}/thumb");
        response.EnsureSuccessStatusCode();
        Assert.Equal("image/jpeg", response.Content.Headers.ContentType?.MediaType);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        Assert.Equal(0xFF, bytes[0]);
        Assert.Equal(0xD8, bytes[1]);
    }

    [Fact]
    public async Task GetThumbnail_NonExistent_Returns404()
    {
        var response = await _anonClient.GetAsync($"/api/assets/{Guid.NewGuid()}/thumb");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ??? Download (Auth required) ???????????????????????

    [Fact]
    public async Task Download_AsInternal_ReturnsOriginalFile()
    {
        // Upload as admin
        var (_, uploaded) = await UploadAsAdminAsync(
       TestData.CreateTestJpeg(r: 50, g: 100, b: 150), title: "Download Test");

        // Download as internal user
        var internal_ = await _factory.CreateInternalClientAsync();
        var response = await internal_.GetAsync($"/api/assets/{uploaded.Id}/download");

        response.EnsureSuccessStatusCode();
        Assert.Equal("image/jpeg", response.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(response.Content.Headers.ContentDisposition);
        Assert.Equal("attachment", response.Content.Headers.ContentDisposition.DispositionType);
    }

    [Fact]
    public async Task Download_AsAdmin_ReturnsOriginalFile()
    {
        var (admin, uploaded) = await UploadAsAdminAsync(
        TestData.CreateTestJpeg(r: 55, g: 105, b: 155), title: "Admin Download Test");

        var response = await admin.GetAsync($"/api/assets/{uploaded.Id}/download");
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Download_Anonymous_Returns401()
    {
        var (_, uploaded) = await UploadAsAdminAsync(
          TestData.CreateTestJpeg(r: 60, g: 110, b: 160), title: "Anon Download Test");

        var response = await _anonClient.GetAsync($"/api/assets/{uploaded.Id}/download");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Download_NonExistent_Returns404()
    {
        var client = await _factory.CreateInternalClientAsync();
        var response = await client.GetAsync($"/api/assets/{Guid.NewGuid()}/download");
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
