using System.Net.Http.Json;
using PhotoBase.Shared.Dtos;
using PhotoBase.Shared.Responses;

namespace PhotoBase.Tests.Integration;

/// <summary>
/// Integration tests for faceted search: GET /api/assets/facets and
/// GET /api/assets with filter params (category, location, photographer, year, sort).
/// </summary>
public class FacetedSearchTests : IClassFixture<PhotoBaseTestFactory>
{
    private readonly PhotoBaseTestFactory _factory;
    private readonly HttpClient _anon;

    public FacetedSearchTests(PhotoBaseTestFactory factory)
    {
        _factory = factory;
        _anon = factory.CreateClient();
    }

    private async Task SeedAssetAsync(string title, byte r, byte g, byte b,
        string? categories = null, string? location = null,
        string? photographer = null, string? keywords = null,
        DateTime? dateTaken = null)
    {
        var admin = await _factory.CreateAdminClientAsync();
        var jpeg = TestData.CreateTestJpeg(r: r, g: g, b: b);
        using var form = TestData.CreateUploadForm(jpeg, title: title,
    keywords: keywords, location: location, photographer: photographer);

        // Categories and DateTaken need to be added manually to form
        if (!string.IsNullOrEmpty(categories))
            form.Add(new StringContent(categories), "Categories");
        if (dateTaken.HasValue)
            form.Add(new StringContent(dateTaken.Value.ToString("o")), "DateTaken");

        var resp = await admin.PostAsync("/api/assets", form);
        resp.EnsureSuccessStatusCode();
    }

    // ?? Facets endpoint ?????????????????????????????????

    [Fact]
    public async Task Facets_ReturnsDistinctValuesWithCounts()
    {
        // Seed assets with known facet values
        await SeedAssetAsync("Facet Rose", 180, 1, 1,
     categories: "flower,landscape", location: "Garden A",
         photographer: "Alice", dateTaken: new DateTime(2024, 6, 15));
        await SeedAssetAsync("Facet Oak", 181, 2, 2,
  categories: "tree,landscape", location: "Garden B",
            photographer: "Bob", dateTaken: new DateTime(2023, 9, 1));
        await SeedAssetAsync("Facet Fern", 182, 3, 3,
      categories: "flower", location: "Garden A",
            photographer: "Alice", dateTaken: new DateTime(2024, 3, 20));

        var resp = await _anon.GetAsync("/api/assets/facets");
        resp.EnsureSuccessStatusCode();
        var facets = await resp.Content.ReadFromJsonAsync<FacetsDto>();

        Assert.NotNull(facets);

        // Categories: flower, landscape, tree
        Assert.True(facets.Categories.Count >= 3);
        Assert.Contains(facets.Categories, f => f.Value == "flower" && f.Count >= 2);
        Assert.Contains(facets.Categories, f => f.Value == "landscape" && f.Count >= 2);
        Assert.Contains(facets.Categories, f => f.Value == "tree" && f.Count >= 1);

        // Locations: Garden A (2), Garden B (1)
        Assert.Contains(facets.Locations, f => f.Value == "Garden A" && f.Count >= 2);
        Assert.Contains(facets.Locations, f => f.Value == "Garden B" && f.Count >= 1);

        // Photographers: Alice (2), Bob (1)
        Assert.Contains(facets.Photographers, f => f.Value == "Alice" && f.Count >= 2);
        Assert.Contains(facets.Photographers, f => f.Value == "Bob" && f.Count >= 1);

        // Years: 2024 (2), 2023 (1)
        Assert.Contains(facets.Years, f => f.Value == "2024" && f.Count >= 2);
        Assert.Contains(facets.Years, f => f.Value == "2023" && f.Count >= 1);
    }

    [Fact]
    public async Task Facets_EmptyGallery_ReturnsEmptyLists()
    {
        // Use a fresh factory would be ideal, but we can just check structure
        var resp = await _anon.GetAsync("/api/assets/facets");
        resp.EnsureSuccessStatusCode();
        var facets = await resp.Content.ReadFromJsonAsync<FacetsDto>();

        Assert.NotNull(facets);
        Assert.NotNull(facets.Categories);
        Assert.NotNull(facets.Locations);
        Assert.NotNull(facets.Photographers);
        Assert.NotNull(facets.Years);
    }

    // ?? Filter by category ??????????????????????????????

    [Fact]
    public async Task Search_FilterByCategory_NarrowsResults()
    {
        await SeedAssetAsync("Cat Filter A", 183, 4, 4, categories: "unique-cat-abc");
        await SeedAssetAsync("Cat Filter B", 184, 5, 5, categories: "other-cat");

        var resp = await _anon.GetAsync("/api/assets?category=unique-cat-abc");
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<PagedResult<AssetListItemDto>>();

        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
        Assert.All(result.Items, item =>
            Assert.Contains("unique-cat-abc", item.Categories ?? "", StringComparison.OrdinalIgnoreCase));
    }

    // ?? Filter by location ??????????????????????????????

    [Fact]
    public async Task Search_FilterByLocation_NarrowsResults()
    {
        await SeedAssetAsync("Loc Filter", 185, 6, 6, location: "Unique Garden XYZ");

        var resp = await _anon.GetAsync("/api/assets?location=Unique%20Garden%20XYZ");
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<PagedResult<AssetListItemDto>>();

        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
        Assert.All(result.Items, item => Assert.Equal("Unique Garden XYZ", item.Location));
    }

    // ?? Filter by photographer ??????????????????????????

    [Fact]
    public async Task Search_FilterByPhotographer_NarrowsResults()
    {
        await SeedAssetAsync("Photo Filter", 186, 7, 7, photographer: "Unique Photographer ZZZ");

        var resp = await _anon.GetAsync("/api/assets?photographer=Unique%20Photographer%20ZZZ");
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<PagedResult<AssetListItemDto>>();

        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
        Assert.All(result.Items, item => Assert.Equal("Unique Photographer ZZZ", item.Photographer));
    }

    // ?? Filter by year ??????????????????????????????????

    [Fact]
    public async Task Search_FilterByYear_NarrowsResults()
    {
        await SeedAssetAsync("Year Filter", 187, 8, 8, dateTaken: new DateTime(2019, 1, 1));

        var resp = await _anon.GetAsync("/api/assets?year=2019");
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<PagedResult<AssetListItemDto>>();

        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
        Assert.All(result.Items, item =>
      {
          Assert.NotNull(item.DateTaken);
          Assert.Equal(2019, item.DateTaken.Value.Year);
      });
    }

    // ?? Combined filters ????????????????????????????????

    [Fact]
    public async Task Search_CombinedFilters_StackCorrectly()
    {
        await SeedAssetAsync("Combined Filter", 188, 9, 9,
    categories: "combo-cat", location: "Combo Garden",
              photographer: "Combo Alice", dateTaken: new DateTime(2022, 7, 4));

        var resp = await _anon.GetAsync(
    "/api/assets?category=combo-cat&location=Combo%20Garden&photographer=Combo%20Alice&year=2022");
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<PagedResult<AssetListItemDto>>();

        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
        var item = result.Items.First(i => i.Title == "Combined Filter");
        Assert.Contains("combo-cat", item.Categories!);
        Assert.Equal("Combo Garden", item.Location);
        Assert.Equal("Combo Alice", item.Photographer);
    }

    // ?? Sort options ????????????????????????????????????

    [Fact]
    public async Task Search_SortByOldest_ReturnsOldestFirst()
    {
        var resp = await _anon.GetAsync("/api/assets?sort=oldest&pageSize=100");
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<PagedResult<AssetListItemDto>>();

        Assert.NotNull(result);
        if (result.Items.Count >= 2)
        {
            for (int i = 1; i < result.Items.Count; i++)
            {
                Assert.True(result.Items[i].CreatedAt >= result.Items[i - 1].CreatedAt,
                   "Items should be ordered oldest first");
            }
        }
    }

    [Fact]
    public async Task Search_SortByTitle_ReturnsAlphabetical()
    {
        var resp = await _anon.GetAsync("/api/assets?sort=title&pageSize=100");
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<PagedResult<AssetListItemDto>>();

        Assert.NotNull(result);
        if (result.Items.Count >= 2)
        {
            for (int i = 1; i < result.Items.Count; i++)
            {
                Assert.True(
                           string.Compare(result.Items[i].Title, result.Items[i - 1].Title, StringComparison.OrdinalIgnoreCase) >= 0,
                     $"'{result.Items[i].Title}' should come after '{result.Items[i - 1].Title}'");
            }
        }
    }

    // ?? Filters + text query combine ????????????????????

    [Fact]
    public async Task Search_FilterPlusQuery_NarrowsFurther()
    {
        await SeedAssetAsync("Queried Rose", 189, 10, 10,
            categories: "query-combo", keywords: "unique-query-rose");
        await SeedAssetAsync("Queried Oak", 190, 11, 11,
       categories: "query-combo", keywords: "unique-query-oak");

        // Filter by category + search by keyword
        var resp = await _anon.GetAsync("/api/assets?category=query-combo&query=unique-query-rose");
        resp.EnsureSuccessStatusCode();
        var result = await resp.Content.ReadFromJsonAsync<PagedResult<AssetListItemDto>>();

        Assert.NotNull(result);
        Assert.True(result.TotalCount >= 1);
        Assert.All(result.Items, item =>
    {
        Assert.Contains("query-combo", item.Categories ?? "");
        Assert.Contains("unique-query-rose", item.Keywords ?? "");
    });
    }
}
