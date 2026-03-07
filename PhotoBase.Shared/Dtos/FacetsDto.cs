namespace PhotoBase.Shared.Dtos;

/// <summary>
/// A single facet value with its count of matching assets.
/// </summary>
public class FacetValueDto
{
    public string Value { get; set; } = string.Empty;
    public int Count { get; set; }
}

/// <summary>
/// Aggregated facet values for the gallery filter sidebar.
/// Each list contains distinct values + counts from the current asset collection.
/// </summary>
public class FacetsDto
{
    public List<FacetValueDto> Categories { get; set; } = new();
    public List<FacetValueDto> Locations { get; set; } = new();
    public List<FacetValueDto> Photographers { get; set; } = new();
    public List<FacetValueDto> Years { get; set; } = new();
}
