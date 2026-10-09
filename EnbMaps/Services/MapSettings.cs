using EnbMaps.Models;

namespace EnbMaps.Services;

/// <summary>
/// User display preferences shared by the top bar and the map pages.
/// </summary>
public sealed class MapSettings
{
    private readonly HashSet<MapLayer> hiddenLayers = [];

    public event Action? Changed;

    /// <summary>Original "Name" button: show nav labels.</summary>
    public bool ShowNames { get; private set; } = true;

    /// <summary>Original "XYZ" button: append coordinates to nav labels.</summary>
    public bool ShowCoordinates { get; private set; }

    public void ToggleNames()
    {
        ShowNames = !ShowNames;
        Changed?.Invoke();
    }

    public void ToggleCoordinates()
    {
        ShowCoordinates = !ShowCoordinates;
        Changed?.Invoke();
    }

    public bool IsVisible(MapLayer layer) => !hiddenLayers.Contains(layer);

    public void ToggleLayer(MapLayer layer)
    {
        if (!hiddenLayers.Remove(layer))
        {
            hiddenLayers.Add(layer);
        }

        Changed?.Invoke();
    }
}

public enum MapLayer
{
    NavPoints,
    HiddenNavs,
    Gates,
    Planets,
    Stations,
    Other,
    Pictures,
    GravityWells
}

public static class MapLayers
{
    public static readonly IReadOnlyList<(MapLayer Layer, string Name)> All =
    [
        (MapLayer.NavPoints, "Nav-Points"),
        (MapLayer.HiddenNavs, "Hidden Navs"),
        (MapLayer.Gates, "Gates & Wormholes"),
        (MapLayer.Planets, "Planets & Moons"),
        (MapLayer.Stations, "Stations"),
        (MapLayer.Other, "Other"),
        (MapLayer.Pictures, "Pictures"),
        (MapLayer.GravityWells, "Gravity Wells")
    ];

    public static MapLayer For(MapObject mapObject) => mapObject.Type switch
    {
        "Nav-Point" or "Special Nav-Point" => MapLayer.NavPoints,
        "Hidden Nav-Point" or "Hidden Object" or "Hidden Gate" => MapLayer.HiddenNavs,
        "Gate" or "Class Specific Gate" or "Factioned Gate" or "Disabled Gate" or "Wormhole Exit" => MapLayer.Gates,
        "Planet" or "Dwarfplanet" or "Moon" or "Landable Planet" or "Landable Moon" or "Object" => MapLayer.Planets,
        "Station" or "Factioned Station" => MapLayer.Stations,
        _ => MapLayer.Other
    };

    public static MapLayer For(MapImage image) =>
        image.Layer == MapImageLayer.GravityWell ? MapLayer.GravityWells : MapLayer.Pictures;
}
