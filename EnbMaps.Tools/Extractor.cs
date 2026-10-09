using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using EnbMaps.Models;

namespace EnbMaps.Tools;

/// <summary>
/// Converts the archived original pages into the normalized dataset and publishes it, together with the
/// original images, into the web application's wwwroot.
/// </summary>
public sealed class Extractor(string originalDirectory, string repositoryRoot)
{
    public const string DATASET_VERSION = "1.0.0";

    public static readonly JsonSerializerOptions JsonOptions = new(EnbMapsJsonContext.Default.Options)
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public void Run()
    {
        string htmlDirectory = Path.Combine(originalDirectory, "html");
        string overviewHtml = Read(Path.Combine(htmlDirectory, "overview.html"));

        var menu = SiteParser.ParseMenu(overviewHtml);
        var sectors = new List<Sector>();
        foreach (var entry in menu)
        {
            string path = Path.Combine(htmlDirectory, $"map_{entry.Id}.html");
            if (!File.Exists(path))
            {
                Console.WriteLine($"WARNING: no archived page for sector {entry.Id}");
                continue;
            }

            sectors.Add(SiteParser.ParseSector(Read(path), entry));
        }

        var dataset = new EnbMapsDataset
        {
            DatasetVersion = DATASET_VERSION,
            SiteVersion = SiteParser.ParseSiteVersion(overviewHtml),
            SourceUrl = Scraper.DEFAULT_BASE_URL,
            RetrievedUtc = File.GetLastWriteTimeUtc(Path.Combine(htmlDirectory, "overview.html")),
            Overview = SiteParser.ParseOverview(overviewHtml, "gatewayline2012.png"),
            Systems = menu.GroupBy(x => x.SystemName).Select(g => new StarSystem { Name = g.Key, SectorIds = g.Select(x => x.Id).ToList() }).ToList(),
            Sectors = sectors,
            StationList = SiteParser.ParseStationList(Read(Path.Combine(htmlDirectory, "stationlist.html")))
        };

        Validate(dataset);

        string json = JsonSerializer.Serialize(dataset, JsonOptions);
        string normalizedDirectory = Path.Combine(repositoryRoot, "data", "normalized");
        Directory.CreateDirectory(normalizedDirectory);
        File.WriteAllText(Path.Combine(normalizedDirectory, "enbmaps.json"), json);

        string wwwroot = Path.Combine(repositoryRoot, "EnbMaps", "wwwroot");
        Directory.CreateDirectory(Path.Combine(wwwroot, "data"));
        File.WriteAllText(Path.Combine(wwwroot, "data", "enbmaps.json"), JsonSerializer.Serialize(dataset, new JsonSerializerOptions(JsonOptions) { WriteIndented = false }));

        string imageTarget = Path.Combine(wwwroot, "bilder");
        Directory.CreateDirectory(imageTarget);
        foreach (string file in Directory.GetFiles(Path.Combine(originalDirectory, "bilder")))
        {
            File.Copy(file, Path.Combine(imageTarget, Path.GetFileName(file)), overwrite: true);
        }
        File.Copy(Path.Combine(originalDirectory, "gatewayline2012.png"), Path.Combine(imageTarget, "gatewayline2012.png"), overwrite: true);

        Console.WriteLine($"Extracted {sectors.Count} sectors, {sectors.Sum(x => x.Objects.Count)} objects, " +
            $"{sectors.Sum(x => x.Stations.Count)} station panels, {dataset.StationList.Entries.Count} station list entries.");
    }

    private void Validate(EnbMapsDataset dataset)
    {
        var sectorIds = dataset.Sectors.Select(x => x.Id).ToHashSet();
        string imageDirectory = Path.Combine(originalDirectory, "bilder");

        foreach (var sector in dataset.Sectors)
        {
            if (sector.Objects.Count == 0)
            {
                Console.WriteLine($"WARNING: {sector.Id} has no objects");
            }

            foreach (var mapObject in sector.Objects.Where(x => x.TargetSectorId is not null && !sectorIds.Contains(x.TargetSectorId)))
            {
                Console.WriteLine($"WARNING: {mapObject.Id} '{mapObject.Name}' links to unknown sector {mapObject.TargetSectorId}");
            }

            foreach (var image in sector.Images.Select(x => x.Src).Concat(sector.Objects.Select(x => x.Icon)).Distinct())
            {
                if (!File.Exists(Path.Combine(imageDirectory, image)))
                {
                    Console.WriteLine($"WARNING: {sector.Id} references missing image {image}");
                }
            }

            foreach (var station in sector.Objects.Where(x => x.TooltipIcon == "50" && sector.Stations.All(s => s.Name != x.Name)))
            {
                Console.WriteLine($"NOTE: {sector.Id} station '{station.Name}' has no information panel");
            }
        }

        foreach (string missing in dataset.Overview.Sectors.Select(x => x.SectorId).Where(x => !sectorIds.Contains(x)))
        {
            Console.WriteLine($"WARNING: overview references unknown sector {missing}");
        }
    }

    private static string Read(string path) => File.ReadAllText(path, SourceEncoding.Instance);
}
