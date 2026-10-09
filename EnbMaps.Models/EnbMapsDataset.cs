namespace EnbMaps.Models;

/// <summary>
/// The complete normalized ENBMaps dataset, extracted from the archived pages of the original site.
/// </summary>
public sealed record EnbMapsDataset
{
    public required string DatasetVersion { get; init; }

    /// <summary>Version string shown in the original site header (e.g. "v3.0.4").</summary>
    public required string SiteVersion { get; init; }

    public required string SourceUrl { get; init; }

    public required DateTimeOffset RetrievedUtc { get; init; }

    public required Overview Overview { get; init; }

    /// <summary>Star systems in the order of the original sector menu.</summary>
    public required IReadOnlyList<StarSystem> Systems { get; init; }

    public required IReadOnlyList<Sector> Sectors { get; init; }

    public required StationList StationList { get; init; }
}
