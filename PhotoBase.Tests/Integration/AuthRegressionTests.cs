using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using PhotoBase.Shared.Dtos;
using PhotoBase.Shared.Responses;

namespace PhotoBase.Tests.Integration;

/// <summary>
/// Focused auth regression tests — verifies the full endpoint access matrix
/// in a single class so regressions are immediately visible.
///
/// Access matrix:
///   POST /api/auth/login          ? Anonymous ?
///   GET  /api/assets          ? Anonymous ?
///   GET  /api/assets/{id}         ? Anonymous ?
///   GET  /api/assets/{id}/thumb   ? Anonymous ?
///   GET  /api/assets/{id}/download? Internal ?  Admin ?  Anonymous ?(401)
///   POST /api/assets ? Admin ?     Internal ?(403)  Anonymous ?(401)
///   POST /api/import/plant-records? Admin ?     Internal ?(403)  Anonymous ?(401)
/// </summary>
public class AuthRegressionTests : IClassFixture<PhotoBaseTestFactory>
{
    private readonly PhotoBaseTestFactory _factory;

    public AuthRegressionTests(PhotoBaseTestFactory factory)
    {
        _factory = factory;
    }

    // ??? Token acquisition ??????????????????????????????

    [Fact]
    public async Task Login_ReturnsValidJwt_ThatAuthorizesSubsequentRequests()
    {
        var client = _factory.CreateClient();

        // Login
        var loginResp = await client.PostAsJsonAsync("/api/auth/login",
 new { email = "admin@photobase.dev", password = "Admin123!" });
        loginResp.EnsureSuccessStatusCode();
        var login = await loginResp.Content.ReadFromJsonAsync<LoginResponse>();
        Assert.NotNull(login);
        Assert.False(string.IsNullOrWhiteSpace(login.Token));

        // Use the token on a protected endpoint
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", login.Token);

        var jpeg = TestData.CreateTestJpeg(r: 250, g: 1, b: 1);
        using var form = TestData.CreateUploadForm(jpeg, title: "Token Flow Test");
        var uploadResp = await client.PostAsync("/api/assets", form);
        Assert.Equal(HttpStatusCode.Created, uploadResp.StatusCode);
    }

    [Fact]
    public async Task MalformedToken_Returns401()
    {
        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "not.a.real.jwt.token");

        var response = await client.PostAsync("/api/assets",
                   TestData.CreateUploadForm(TestData.CreateTestJpeg(), title: "Bad Token"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ExpiredToken_Returns401()
    {
        // Generate a token that expired 1 hour ago using a custom TokenService
        var expiredToken = TokenHelper.GenerateExpiredToken(PhotoBaseTestFactory.TestJwtSecret);

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
   new AuthenticationHeaderValue("Bearer", expiredToken);

        var response = await client.PostAsync("/api/assets",
            TestData.CreateUploadForm(TestData.CreateTestJpeg(r: 240, g: 2, b: 2), title: "Expired"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task TokenSignedWithWrongKey_Returns401()
    {
        var wrongKeyToken = TokenHelper.GenerateToken(
    "Completely-Wrong-Key-That-Server-Does-Not-Know-About!!", "Admin");

        var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
      new AuthenticationHeaderValue("Bearer", wrongKeyToken);

        var response = await client.PostAsync("/api/assets",
     TestData.CreateUploadForm(TestData.CreateTestJpeg(r: 230, g: 3, b: 3), title: "Wrong Key"));
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ??? Public endpoints work without any token ????????

    [Fact]
    public async Task PublicEndpoints_WorkAnonymously()
    {
        // Seed an asset via admin for the other endpoints to find
        var admin = await _factory.CreateAdminClientAsync();
        var jpeg = TestData.CreateTestJpeg(r: 111, g: 22, b: 33);
        using var form = TestData.CreateUploadForm(jpeg, title: "Public Access Test");
        var uploadResp = await admin.PostAsync("/api/assets", form);
        var asset = await uploadResp.Content.ReadFromJsonAsync<AssetDetailDto>();
        Assert.NotNull(asset);

        // Now test all public endpoints with a completely anonymous client
        var anon = _factory.CreateClient();

        // Search
        var searchResp = await anon.GetAsync("/api/assets?page=1&pageSize=5");
        Assert.Equal(HttpStatusCode.OK, searchResp.StatusCode);

        // Detail
        var detailResp = await anon.GetAsync($"/api/assets/{asset.Id}");
        Assert.Equal(HttpStatusCode.OK, detailResp.StatusCode);

        // Thumbnail
        var thumbResp = await anon.GetAsync($"/api/assets/{asset.Id}/thumb");
        Assert.Equal(HttpStatusCode.OK, thumbResp.StatusCode);
    }

    // ??? Download requires auth ?????????????????????????

    [Fact]
    public async Task Download_Anonymous_Returns401()
    {
        var (_, asset) = await SeedAssetAsync();
        var anon = _factory.CreateClient();

        var resp = await anon.GetAsync($"/api/assets/{asset.Id}/download");
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Download_Internal_Returns200()
    {
        var (_, asset) = await SeedAssetAsync();
        var client = await _factory.CreateInternalClientAsync();

        var resp = await client.GetAsync($"/api/assets/{asset.Id}/download");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [Fact]
    public async Task Download_Admin_Returns200()
    {
        var (admin, asset) = await SeedAssetAsync();

        var resp = await admin.GetAsync($"/api/assets/{asset.Id}/download");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    // ??? Upload requires Admin ??????????????????????????

    [Fact]
    public async Task Upload_Anonymous_Returns401()
    {
        var anon = _factory.CreateClient();
        using var form = TestData.CreateUploadForm(TestData.CreateTestJpeg(r: 170, g: 4, b: 4), title: "Anon");

        var resp = await anon.PostAsync("/api/assets", form);
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Upload_Internal_Returns403()
    {
        var client = await _factory.CreateInternalClientAsync();
        using var form = TestData.CreateUploadForm(TestData.CreateTestJpeg(r: 160, g: 5, b: 5), title: "Internal");

        var resp = await client.PostAsync("/api/assets", form);
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task Upload_Admin_Returns201()
    {
        var client = await _factory.CreateAdminClientAsync();
        using var form = TestData.CreateUploadForm(TestData.CreateTestJpeg(r: 145, g: 6, b: 6), title: "Admin");

        var resp = await client.PostAsync("/api/assets", form);
        Assert.Equal(HttpStatusCode.Created, resp.StatusCode);
    }

    // ??? Import requires Admin ??????????????????????????

    [Fact]
    public async Task Import_Anonymous_Returns401()
    {
        var anon = _factory.CreateClient();
        using var form = TestData.CreateTsvForm(TestData.SampleTsv);

        var resp = await anon.PostAsync("/api/import/plant-records", form);
        Assert.Equal(HttpStatusCode.Unauthorized, resp.StatusCode);
    }

    [Fact]
    public async Task Import_Internal_Returns403()
    {
        var client = await _factory.CreateInternalClientAsync();
        using var form = TestData.CreateTsvForm(TestData.SampleTsv);

        var resp = await client.PostAsync("/api/import/plant-records", form);
        Assert.Equal(HttpStatusCode.Forbidden, resp.StatusCode);
    }

    [Fact]
    public async Task Import_Admin_Returns200()
    {
        var client = await _factory.CreateAdminClientAsync();
        using var form = TestData.CreateTsvForm(TestData.SampleTsv);

        var resp = await client.PostAsync("/api/import/plant-records", form);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    // ??? Helpers ????????????????????????????????????????

    private async Task<(HttpClient admin, AssetDetailDto asset)> SeedAssetAsync()
    {
        var admin = await _factory.CreateAdminClientAsync();
        var jpeg = TestData.CreateTestJpeg(r: (byte)Random.Shared.Next(1, 255),
      g: (byte)Random.Shared.Next(1, 255), b: (byte)Random.Shared.Next(1, 255));
        using var form = TestData.CreateUploadForm(jpeg, title: "Auth Test Asset");
        var resp = await admin.PostAsync("/api/assets", form);
        resp.EnsureSuccessStatusCode();
        var asset = await resp.Content.ReadFromJsonAsync<AssetDetailDto>();
        return (admin, asset!);
    }
}
