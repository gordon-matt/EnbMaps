namespace EnbMaps.Models;

public sealed record StarSystem
{
    public required string Name { get; init; }

    public required IReadOnlyList<string> SectorIds { get; init; }
}
