namespace EnbMaps.Models;

/// <summary>
/// A single sector map. All positions are in original game units (X to the right, Y up), as shown in the
/// original tooltips. Image and boundary positions were derived from the original pixel layout.
/// </summary>
public sealed record Sector
{
    /// <summary>Original <c>map=</c> URL parameter, e.g. "Earth".</summary>
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string SystemName { get; init; }

    /// <summary>Page heading, e.g. "Sol System - Earth".</summary>
    public required string Title { get; init; }

    /// <summary>Extra menu info such as "Terran Scout" for starting sectors.</summary>
    public string? MenuInfo { get; init; }

    /// <summary>Menu colour style for starting sectors: "terran", "jenquai" or "progen".</summary>
    public string? MenuStyle { get; init; }

    /// <summary>Pixels per game unit in the original reference layout. Used to convert label offsets.</summary>
    public required double ReferenceScale { get; init; }

    public WorldRect? Bounds { get; init; }

    public required IReadOnlyList<MapImage> Images { get; init; }

    public required IReadOnlyList<MapObject> Objects { get; init; }

    public required IReadOnlyList<StationPanel> Stations { get; init; }
}

public sealed record WorldRect(double MinX, double MinY, double MaxX, double MaxY);

/// <summary>Background image (planet, station model, gravity well) in game units. X/Y is the top-left corner.</summary>
public sealed record MapImage(string Src, MapImageLayer Layer, double X, double Y, double Width, double Height);

public enum MapImageLayer
{
    GravityWell,
    Picture
}
