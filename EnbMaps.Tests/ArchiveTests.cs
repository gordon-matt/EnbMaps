using EnbMaps.Models;
using EnbMaps.Tools;

namespace EnbMaps.Tests;

/// <summary>
/// Runs the parser over the complete archived copy of the original site in data/original.
/// </summary>
public class ArchiveTests
{
    private static readonly string originalDirectory = Path.Combine(FindRepositoryRoot(), "data", "original");

    private static string ReadPage(string name) =>
        File.ReadAllText(Path.Combine(originalDirectory, "html", name), SourceEncoding.Instance);

    [Fact]
    public void AllSectorsParse_WithObjectsAndValidGateTargets()
    {
        string overview = ReadPage("overview.html");
        var menu = SiteParser.ParseMenu(overview);
        var sectors = menu.Select(m => SiteParser.ParseSector(ReadPage($"map_{m.Id}.html"), m)).ToList();
        var ids = sectors.Select(x => x.Id).ToHashSet();

        Assert.Equal(84, sectors.Count);
        Assert.All(sectors, s => Assert.NotEmpty(s.Objects));
        Assert.All(sectors, s => Assert.True(s.ReferenceScale > 0, $"{s.Id} has an invalid scale"));
        Assert.All(sectors.SelectMany(s => s.Objects), o => Assert.True(o.TargetSectorId is null || ids.Contains(o.TargetSectorId), $"{o.Id} -> {o.TargetSectorId}"));
        Assert.Equal(sectors.Sum(s => s.Objects.Count), sectors.SelectMany(s => s.Objects).Select(o => o.Id).Distinct().Count());
        Assert.DoesNotContain(sectors.SelectMany(s => s.Objects), o => o.Name.Contains('\uFFFD'));
    }

    [Fact]
    public void OverviewAndStationListParse()
    {
        string overview = ReadPage("overview.html");
        var parsed = SiteParser.ParseOverview(overview, "gatewayline2012.png");
        var stations = SiteParser.ParseStationList(ReadPage("stationlist.html"));

        Assert.Equal(20, parsed.Systems.Count);
        Assert.Equal(84, parsed.Sectors.Count);
        Assert.Equal(54, stations.Entries.Count);
        Assert.Equal(Enumerable.Range(1, 54), stations.Entries.Select(x => x.Number));
        Assert.All(stations.Entries, e => Assert.Equal(stations.ColumnIcons.Count, e.Cells.Count));
    }

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "EnbMaps.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found.");
    }
}
