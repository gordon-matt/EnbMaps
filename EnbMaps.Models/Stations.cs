namespace EnbMaps.Models;

/// <summary>Station information panel shown beside a sector map.</summary>
public sealed record StationPanel
{
    public required string Name { get; init; }

    /// <summary>Original panel markup (faction, terminals, vendors, NPCs). References images under bilder/.</summary>
    public required string Html { get; init; }
}

/// <summary>The original "Station List" page.</summary>
public sealed record StationList
{
    /// <summary>Header icons for the terminal/vendor columns.</summary>
    public required IReadOnlyList<string> ColumnIcons { get; init; }

    public required IReadOnlyList<StationListEntry> Entries { get; init; }
}

public sealed record StationListEntry
{
    public required int Number { get; init; }

    public required string Name { get; init; }

    public required string SectorId { get; init; }

    public required string SectorName { get; init; }

    public required string SectorIcon { get; init; }

    public PlacedImage? FactionIcon { get; init; }

    public required string FactionName { get; init; }

    /// <summary>Original cell markup, one entry per column in <see cref="StationList.ColumnIcons"/>.</summary>
    public required IReadOnlyList<string> Cells { get; init; }
}
