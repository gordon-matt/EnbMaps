using System.Net.Http.Json;
using EnbMaps.Models;

namespace EnbMaps.Services;

/// <summary>
/// Loads the static ENBMaps dataset once and provides lookups and search over it.
/// </summary>
public sealed class MapDataService(HttpClient httpClient)
{
    private Task<EnbMapsDataset>? loadTask;
    private Dictionary<string, Sector> sectorsById = [];

    public EnbMapsDataset? Dataset { get; private set; }

    public Task<EnbMapsDataset> LoadAsync() => loadTask ??= LoadCoreAsync();

    public Sector? FindSector(string? id) =>
        id is not null && sectorsById.TryGetValue(id, out var sector) ? sector : null;

    public string SectorName(string? id) => FindSector(id)?.Name ?? id ?? string.Empty;

    public string SectorIcon(string sectorId) =>
        Dataset?.Overview.Sectors.FirstOrDefault(x => x.SectorId == sectorId)?.Images.FirstOrDefault()?.Src ?? "overview-sektor.png";

    public SearchResults Search(string? query)
    {
        if (Dataset is null || string.IsNullOrWhiteSpace(query))
        {
            return SearchResults.Empty;
        }

        string term = query.Trim();
        bool Matches(string? value) => value?.Contains(term, StringComparison.OrdinalIgnoreCase) == true;

        var sectors = Dataset.Sectors
            .Where(x => Matches(x.Name) || Matches(x.Id))
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var objects = Dataset.Sectors
            .SelectMany(sector => sector.Objects.Select((mapObject, index) => new ObjectSearchResult(sector, mapObject, index)))
            .Where(x => Matches(x.Object.Name))
            .OrderBy(x => x.Object.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Sector.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new SearchResults(sectors, objects);
    }

    private async Task<EnbMapsDataset> LoadCoreAsync()
    {
        var dataset = await httpClient.GetFromJsonAsync("data/enbmaps.json", EnbMapsJsonContext.Default.EnbMapsDataset)
            ?? throw new InvalidOperationException("The ENBMaps dataset could not be loaded.");

        sectorsById = dataset.Sectors.ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);
        Dataset = dataset;
        return dataset;
    }
}

public sealed record ObjectSearchResult(Sector Sector, MapObject Object, int Index);

public sealed record SearchResults(IReadOnlyList<Sector> Sectors, IReadOnlyList<ObjectSearchResult> Objects)
{
    public static readonly SearchResults Empty = new([], []);

    public int Count => Sectors.Count + Objects.Count;
}
