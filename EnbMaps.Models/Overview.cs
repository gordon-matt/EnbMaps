namespace EnbMaps.Models;

/// <summary>
/// The galaxy overview page. Positions are the original pixel positions at the reference viewport size.
/// </summary>
public sealed record Overview
{
    public required int Width { get; init; }

    public required int Height { get; init; }

    /// <summary>Server-generated image containing the gateway lines between sectors.</summary>
    public required string GatewayImage { get; init; }

    public required IReadOnlyList<OverviewSystem> Systems { get; init; }

    public required IReadOnlyList<OverviewSector> Sectors { get; init; }
}

public sealed record OverviewSystem
{
    public required string Name { get; init; }

    public required PlacedImage Image { get; init; }
}

public sealed record OverviewSector
{
    public required string SectorId { get; init; }

    /// <summary>Label as shown on the overview (can differ from the menu name, e.g. "AstroBelt Alpha").</summary>
    public required string Label { get; init; }

    public required int LabelLeft { get; init; }

    public required int LabelTop { get; init; }

    public required IReadOnlyList<PlacedImage> Images { get; init; }
}

public sealed record PlacedImage(string Src, int Left, int Top, int Width, int Height);
