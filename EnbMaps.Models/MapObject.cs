namespace EnbMaps.Models;

/// <summary>
/// A nav/object on a sector map, taken from the original tooltip call:
/// <c>an(icon, style, type, name, info, x, y, z)</c>.
/// </summary>
public sealed record MapObject
{
    /// <summary>Stable identifier within the dataset: "{SectorId}/{index}".</summary>
    public required string Id { get; init; }

    /// <summary>Original object type text, e.g. "Nav-Point", "Class Specific Gate".</summary>
    public required string Type { get; init; }

    public required string Name { get; init; }

    /// <summary>Additional tooltip text, e.g. "Only for Terran Scout (TS)". May contain simple HTML.</summary>
    public string? Info { get; init; }

    /// <summary>Tooltip icon id (bilder/icon{id}.gif).</summary>
    public required string TooltipIcon { get; init; }

    /// <summary>Map icon image (bilder/iconklein{n}.png).</summary>
    public required string Icon { get; init; }

    /// <summary>Colour style: blau, grau, gruen, gelb, orange, rot.</summary>
    public required string Style { get; init; }

    public required double X { get; init; }

    public required double Y { get; init; }

    public required double Z { get; init; }

    /// <summary>Destination sector for gates and landable planets.</summary>
    public string? TargetSectorId { get; init; }

    public LabelPlacement? Label { get; init; }
}

/// <summary>
/// Label position relative to the top-left of the 16px icon, in reference pixels, as laid out by the original site.
/// </summary>
public sealed record LabelPlacement(LabelAlign Align, int Dx, int Dy);

public enum LabelAlign
{
    Left,
    Right,
    Center
}
