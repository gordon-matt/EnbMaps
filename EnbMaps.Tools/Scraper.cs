using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace EnbMaps.Tools;

/// <summary>
/// Downloads the original enbmaps.de site (rendered HTML pages, stylesheet, script and every referenced image)
/// into an archive folder. The site is PHP rendered and requires a session cookie plus the
/// <c>breite</c>/<c>hoehe</c> (width/height) query parameters, which is why generic crawlers fail on it.
/// </summary>
public sealed partial class Scraper(string outputDirectory, string baseUrl = Scraper.DEFAULT_BASE_URL)
{
    public const string DEFAULT_BASE_URL = "http://enbmaps.de/";
    public const int REFERENCE_WIDTH = 1600;
    public const int REFERENCE_HEIGHT = 900;

    private static readonly Encoding encoding = SourceEncoding.Instance;
    private static readonly TimeSpan politeDelay = TimeSpan.FromMilliseconds(250);

    private readonly HttpClient httpClient = new(new HttpClientHandler { CookieContainer = new CookieContainer() })
    {
        BaseAddress = new Uri(baseUrl),
        Timeout = TimeSpan.FromSeconds(60)
    };

    private string HtmlDirectory => Path.Combine(outputDirectory, "html");

    private string ImageDirectory => Path.Combine(outputDirectory, "bilder");

    public async Task RunAsync()
    {
        Directory.CreateDirectory(HtmlDirectory);
        Directory.CreateDirectory(ImageDirectory);

        string sizeQuery = $"breite={REFERENCE_WIDTH}&hoehe={REFERENCE_HEIGHT}";

        Console.WriteLine("Fetching root shell page...");
        await SaveAsync("", Path.Combine(outputDirectory, "index.html"));
        foreach (string asset in new[] { "scripts2012.js", "styles2012.css", "favicon.png" })
        {
            await SaveAsync(asset, Path.Combine(outputDirectory, asset));
        }

        Console.WriteLine("Fetching overview...");
        string overview = await SaveAsync($"index.php?zeige=n&{sizeQuery}", Path.Combine(HtmlDirectory, "overview.html"));

        // The gateway lines image is generated per session for the last requested viewport size.
        await SaveAsync("gatewayline2012.inc.php", Path.Combine(outputDirectory, "gatewayline2012.png"));

        var sectorIds = MapLinkRegex().Matches(overview).Select(x => x.Groups["id"].Value).Distinct().ToList();
        Console.WriteLine($"Found {sectorIds.Count} sectors.");

        var pages = new List<string> { overview };
        foreach (string sectorId in sectorIds)
        {
            Console.WriteLine($"  Sector: {sectorId}");
            pages.Add(await SaveAsync($"index.php?map={sectorId}&zeige=n&{sizeQuery}", Path.Combine(HtmlDirectory, $"map_{sectorId}.html")));
        }

        Console.WriteLine("Fetching station list...");
        pages.Add(await SaveAsync($"index.php?stationlist&zeige=n&{sizeQuery}", Path.Combine(HtmlDirectory, "stationlist.html")));

        var images = new SortedSet<string>(StringComparer.Ordinal);
        foreach (string page in pages)
        {
            foreach (Match match in ImageSrcRegex().Matches(page))
            {
                images.Add(match.Groups["name"].Value);
            }

            // Tooltip icons are only referenced from the inline an(...) JavaScript calls.
            foreach (Match match in TooltipIconRegex().Matches(page))
            {
                images.Add($"icon{match.Groups["icon"].Value}.gif");
            }
        }

        string css = await File.ReadAllTextAsync(Path.Combine(outputDirectory, "styles2012.css"), encoding);
        foreach (Match match in CssUrlRegex().Matches(css))
        {
            images.Add(match.Groups["name"].Value);
        }

        // Referenced by the original search results page only.
        images.Add("overview-sektor.png");
        images.Add("overview-nav.png");

        Console.WriteLine($"Downloading {images.Count} images...");
        int failed = 0;
        foreach (string image in images)
        {
            string target = Path.Combine(ImageDirectory, image);
            if (File.Exists(target))
            {
                continue;
            }

            try
            {
                byte[] bytes = await httpClient.GetByteArrayAsync("bilder/" + Uri.EscapeDataString(image));
                await File.WriteAllBytesAsync(target, bytes);
            }
            catch (HttpRequestException ex)
            {
                failed++;
                Console.WriteLine($"  MISSING bilder/{image}: {ex.StatusCode}");
            }

            await Task.Delay(politeDelay);
        }

        Console.WriteLine($"Done. {images.Count - failed} images saved, {failed} missing.");
    }

    private async Task<string> SaveAsync(string relativeUrl, string path)
    {
        byte[] bytes = await httpClient.GetByteArrayAsync(relativeUrl);
        await File.WriteAllBytesAsync(path, bytes);
        await Task.Delay(politeDelay);
        return encoding.GetString(bytes);
    }

    [GeneratedRegex(@"map=(?<id>[A-Za-z0-9_]+)&")]
    private static partial Regex MapLinkRegex();

    [GeneratedRegex(@"src=(?:""bilder/(?<name>[^""]+)""|'bilder/(?<name>[^']+)'|bilder/(?<name>[^\s>]+))")]
    private static partial Regex ImageSrcRegex();

    [GeneratedRegex(@"an\(""(?<icon>\d+)""")]
    private static partial Regex TooltipIconRegex();

    [GeneratedRegex(@"url\(""bilder/(?<name>[^""]+)""\)")]
    private static partial Regex CssUrlRegex();
}
