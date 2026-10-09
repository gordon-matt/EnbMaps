using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using EnbMaps.Models;

namespace EnbMaps.Tools;

/// <summary>
/// Parses the server-rendered HTML of the original enbmaps.de pages. The markup is machine generated and very
/// regular, but not always well-formed (e.g. apostrophes inside single-quoted attributes), so regular expressions
/// over the raw text are more reliable here than a DOM parser.
/// </summary>
public static partial class SiteParser
{
    private const int ICON_SIZE = 16;
    private const int LABEL_LINE_HEIGHT = 14;

    public sealed record MenuSector(string Id, string Name, string SystemName, string? Info, string? Style);

    public static string ParseSiteVersion(string html) => VersionRegex().Match(html).Groups["v"].Value;

    public static IReadOnlyList<MenuSector> ParseMenu(string html)
    {
        var sectors = new List<MenuSector>();
        string? system = null;
        foreach (Match match in MenuEntryRegex().Matches(html))
        {
            if (match.Groups["system"].Success)
            {
                system = Text(match.Groups["system"].Value);
                continue;
            }

            sectors.Add(new MenuSector(
                match.Groups["id"].Value,
                Text(match.Groups["name"].Value),
                system ?? "Unknown",
                match.Groups["info"].Success ? Text(match.Groups["info"].Value) : null,
                match.Groups["style"].Success ? match.Groups["style"].Value : null));
        }

        return sectors;
    }

    public static Overview ParseOverview(string html, string gatewayImage)
    {
        var (width, height) = ParseMainWindowSize(html);
        var systems = new List<OverviewSystem>();
        var sectors = new List<(string Id, List<PlacedImage> Images, string? Label, int LabelLeft, int LabelTop)>();

        foreach (Match match in OverviewDivRegex().Matches(html))
        {
            var style = ParseStyle(match.Groups["style"].Value);
            string body = match.Groups["body"].Value;
            string kind = match.Groups["id"].Value;

            switch (kind)
            {
                case "system":
                {
                    var img = ImageRegex().Match(body);
                    string src = img.Groups["src"].Value;
                    systems.Add(new OverviewSystem
                    {
                        Name = Path.GetFileNameWithoutExtension(src)["overview-bg-".Length..],
                        Image = new PlacedImage(src, style.Left ?? 0, style.Top ?? 0, Int(img.Groups["w"].Value), Int(img.Groups["h"].Value))
                    });
                    break;
                }
                case "sektor":
                {
                    string id = MapIdRegex().Match(body).Groups["id"].Value;
                    var img = ImageRegex().Match(body);
                    var image = new PlacedImage(img.Groups["src"].Value, style.Left ?? 0, style.Top ?? 0, Int(img.Groups["w"].Value), Int(img.Groups["h"].Value));
                    int index = sectors.FindIndex(x => x.Id == id);
                    if (index < 0)
                    {
                        sectors.Add((id, [image], null, 0, 0));
                    }
                    else
                    {
                        sectors[index].Images.Add(image);
                    }
                    break;
                }
                case "sektorname":
                {
                    string id = MapIdRegex().Match(body).Groups["id"].Value;
                    int index = sectors.FindIndex(x => x.Id == id);
                    if (index >= 0)
                    {
                        sectors[index] = sectors[index] with { Label = Text(body), LabelLeft = style.Left ?? 0, LabelTop = style.Top ?? 0 };
                    }
                    break;
                }
            }
        }

        return new Overview
        {
            Width = width,
            Height = height,
            GatewayImage = gatewayImage,
            Systems = systems,
            Sectors = sectors.Select(x => new OverviewSector
            {
                SectorId = x.Id,
                Label = x.Label ?? x.Id,
                LabelLeft = x.LabelLeft,
                LabelTop = x.LabelTop,
                Images = x.Images
            }).ToList()
        };
    }

    public static Sector ParseSector(string html, MenuSector menu)
    {
        var (windowWidth, windowHeight) = ParseMainWindowSize(html);
        int mainStart = html.IndexOf("class=\"hauptfenster\"", StringComparison.Ordinal);
        string main = mainStart >= 0 ? html[mainStart..] : string.Empty;

        var navs = new List<(MapObject Object, int Left, int Top)>();
        var images = new List<(string Src, MapImageLayer Layer, int Left, int Top, int Width, int Height)>();
        var boundaries = new List<(int Left, int Top, int Width, int Height)>();
        var stations = new List<StationPanel>();
        int? axisX = null, axisY = null;

        foreach (Match match in MapDivRegex().Matches(main))
        {
            string attributes = match.Groups["attrs"].Value;
            string body = match.Groups["body"].Value;
            var style = ParseStyle(StyleAttributeRegex().Match(attributes).Groups["style"].Value);
            string id = IdAttributeRegex().Match(attributes).Groups["id"].Value;

            if (attributes.Contains("class=station", StringComparison.Ordinal))
            {
                string name = Text(StationHeaderRegex().Match(body).Groups["name"].Value);
                stations.Add(new StationPanel { Name = name, Html = body.Trim() });
                continue;
            }

            var img = ImageRegex().Match(body);
            switch (id)
            {
                case "null" when img.Success:
                    if (img.Groups["src"].Value.EndsWith("achseX.png", StringComparison.Ordinal))
                    {
                        axisX = style.Left;
                    }
                    else
                    {
                        axisY = style.Top;
                    }
                    break;

                case "grenze" when img.Success:
                    boundaries.Add((style.Left ?? 0, style.Top ?? 0, Int(img.Groups["w"].Value), Int(img.Groups["h"].Value)));
                    break;

                case "bild" or "gravwell" when img.Success:
                    images.Add((
                        img.Groups["src"].Value,
                        id == "gravwell" ? MapImageLayer.GravityWell : MapImageLayer.Picture,
                        style.Left ?? 0, style.Top ?? 0, Int(img.Groups["w"].Value), Int(img.Groups["h"].Value)));
                    break;

                case "nav":
                {
                    var call = TooltipCallRegex().Match(body);
                    if (!call.Success)
                    {
                        break;
                    }

                    var args = TooltipArgumentRegex().Matches(call.Groups["args"].Value).Select(x => x.Groups["v"].Value).ToList();
                    if (args.Count < 8)
                    {
                        break;
                    }

                    var target = MapIdRegex().Match(body);
                    var mapObject = new MapObject
                    {
                        Id = $"{menu.Id}/{navs.Count}",
                        TooltipIcon = args[0],
                        Style = args[1],
                        Type = Text(args[2]),
                        Name = Text(args[3]),
                        Info = string.IsNullOrWhiteSpace(args[4]) ? null : args[4],
                        X = Double(args[5]),
                        Y = Double(args[6]),
                        Z = Double(args[7]),
                        Icon = img.Groups["src"].Value,
                        TargetSectorId = target.Success ? target.Groups["id"].Value : null
                    };
                    navs.Add((mapObject, style.Left ?? 0, style.Top ?? 0));
                    break;
                }

                // Any other id is a colour style (blau, grau, ...) used for the label of the preceding nav.
                default:
                    if (navs.Count > 0 && navs[^1].Object.Label is null && style.HasPosition)
                    {
                        var (nav, left, top) = navs[^1];
                        navs[^1] = (nav with { Label = ComputeLabel(style, left, top, windowWidth, windowHeight) }, left, top);
                    }
                    break;
            }
        }

        var (scale, originX, originY) = FitTransform(navs, axisX, axisY);
        double ToWorldX(double px) => (px - originX) / scale;
        double ToWorldY(double py) => (originY - py) / scale;

        WorldRect? bounds = null;
        if (boundaries.Count > 0)
        {
            bounds = new WorldRect(
                Round(ToWorldX(boundaries.Min(b => b.Left))),
                Round(ToWorldY(boundaries.Max(b => b.Top + b.Height))),
                Round(ToWorldX(boundaries.Max(b => b.Left + b.Width))),
                Round(ToWorldY(boundaries.Min(b => b.Top))));
        }

        return new Sector
        {
            Id = menu.Id,
            Name = menu.Name,
            SystemName = menu.SystemName,
            Title = Text(TitleRegex().Match(html).Groups["title"].Value),
            MenuInfo = menu.Info,
            MenuStyle = menu.Style,
            ReferenceScale = Math.Round(scale, 4),
            Bounds = bounds,
            Images = images.Select(i => new MapImage(
                i.Src, i.Layer,
                Round(ToWorldX(i.Left)), Round(ToWorldY(i.Top)),
                Round(i.Width / scale), Round(i.Height / scale))).ToList(),
            Objects = navs.Select(n => n.Object).ToList(),
            Stations = stations
        };
    }

    public static StationList ParseStationList(string html)
    {
        var columnIcons = new List<string>();
        int headerStart = html.IndexOf("<td colspan=2></td>", StringComparison.Ordinal);
        if (headerStart >= 0)
        {
            string header = html[headerStart..html.IndexOf("</tr>", headerStart, StringComparison.Ordinal)];
            columnIcons.AddRange(ImageRegex().Matches(header).Select(x => x.Groups["src"].Value));
        }

        var entries = new List<StationListEntry>();
        foreach (Match match in StationRowRegex().Matches(html))
        {
            var cells = CellSplitRegex().Split(match.Groups["cells"].Value)
                .Skip(1)
                .Select(x => CellEndRegex().Replace(x, string.Empty).Trim())
                .ToList();

            var info = InfoBoxRegex().Matches(match.Groups["info"].Value);
            if (info.Count < 2)
            {
                continue;
            }

            entries.Add(new StationListEntry
            {
                Number = Int(match.Groups["number"].Value),
                Name = Text(match.Groups["name"].Value),
                SectorId = match.Groups["sector"].Value,
                SectorIcon = info[0].Groups["src"].Value,
                SectorName = Text(info[0].Groups["text"].Value),
                FactionIcon = info[1].Groups["src"].Success
                    ? new PlacedImage(info[1].Groups["src"].Value, 0, 0, Int(info[1].Groups["w"].Value), Int(info[1].Groups["h"].Value))
                    : null,
                FactionName = Text(info[1].Groups["text"].Value),
                Cells = cells
            });
        }

        return new StationList { ColumnIcons = columnIcons, Entries = entries };
    }

    /// <summary>
    /// Fits the linear transform pixel = origin + scale * world, shared by both axes (Y inverted), using the
    /// icon centres of all navs and their original game coordinates. Falls back to the drawn axes for the origin.
    /// </summary>
    public static (double Scale, double OriginX, double OriginY) FitTransform(
        IReadOnlyList<(MapObject Object, int Left, int Top)> navs, int? axisX, int? axisY)
    {
        if (navs.Count < 2)
        {
            return (1, axisX ?? 0, axisY ?? 0);
        }

        double half = ICON_SIZE / 2.0;
        double meanX = navs.Average(n => n.Object.X), meanY = navs.Average(n => n.Object.Y);
        double meanPx = navs.Average(n => n.Left + half), meanPy = navs.Average(n => n.Top + half);

        double numerator = navs.Sum(n => ((n.Object.X - meanX) * (n.Left + half - meanPx)) - ((n.Object.Y - meanY) * (n.Top + half - meanPy)));
        double denominator = navs.Sum(n => Math.Pow(n.Object.X - meanX, 2) + Math.Pow(n.Object.Y - meanY, 2));
        double scale = denominator > 0 ? numerator / denominator : 1;

        return (scale, meanPx - (scale * meanX), meanPy + (scale * meanY));
    }

    private static LabelPlacement ComputeLabel(CssPosition style, int iconLeft, int iconTop, int windowWidth, int windowHeight)
    {
        int top = style.Top ?? (windowHeight - (style.Bottom ?? 0) - LABEL_LINE_HEIGHT);
        if (style.Width is int width && style.Left is int centeredLeft)
        {
            return new LabelPlacement(LabelAlign.Center, centeredLeft + (width / 2) - iconLeft, top - iconTop);
        }

        return style.Left is int left
            ? new LabelPlacement(LabelAlign.Left, left - iconLeft, top - iconTop)
            : new LabelPlacement(LabelAlign.Right, windowWidth - (style.Right ?? 0) - iconLeft, top - iconTop);
    }

    private static (int Width, int Height) ParseMainWindowSize(string html)
    {
        var match = MainWindowRegex().Match(html);
        return match.Success ? (Int(match.Groups["w"].Value), Int(match.Groups["h"].Value)) : (1600, 868);
    }

    private sealed record CssPosition(int? Top, int? Left, int? Right, int? Bottom, int? Width)
    {
        public bool HasPosition => (Top ?? Bottom) is not null && (Left ?? Right) is not null;
    }

    private static CssPosition ParseStyle(string style)
    {
        var values = CssPropertyRegex().Matches(style).ToDictionary(x => x.Groups["k"].Value, x => (int?)Int(x.Groups["v"].Value));
        return new CssPosition(
            values.GetValueOrDefault("top"),
            values.GetValueOrDefault("left"),
            values.GetValueOrDefault("right"),
            values.GetValueOrDefault("bottom"),
            values.GetValueOrDefault("width"));
    }

    /// <summary>Strips tags, decodes entities and normalizes whitespace.</summary>
    public static string Text(string html) =>
        WhitespaceRegex().Replace(WebUtility.HtmlDecode(TagRegex().Replace(html.Replace("<br>", " "), string.Empty)), " ").Trim();

    private static int Int(string value) => int.Parse(value, CultureInfo.InvariantCulture);

    private static double Double(string value) =>
        double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double result) ? result : 0;

    private static double Round(double value) => Math.Round(value, 2);

    [GeneratedRegex(@"<div id=""version"">(?<v>[^<]*)</div>")]
    private static partial Regex VersionRegex();

    [GeneratedRegex(@"<a href="""" id=""system"">(?<system>[^<]*)</a>|<li><a href=""/index\.php\?map=(?<id>\w+)&[^""]*""(?: id=""(?<style>\w+)"")?>(?<name>[^<]*?)\s*(?:<span id=""info"">\((?<info>[^<]*)\)</span>)?</a></li>")]
    private static partial Regex MenuEntryRegex();

    [GeneratedRegex(@"<div id=""(?<id>system|sektor|sektorname)"" style=""(?<style>[^""]*)"">(?<body>.*?)</div>")]
    private static partial Regex OverviewDivRegex();

    [GeneratedRegex(@"<div (?<attrs>[^>]*)>(?<body>.*?)</div>", RegexOptions.Singleline)]
    private static partial Regex MapDivRegex();

    [GeneratedRegex(@"style=""(?<style>[^""]*)""")]
    private static partial Regex StyleAttributeRegex();

    [GeneratedRegex(@"id=""?(?<id>\w+)")]
    private static partial Regex IdAttributeRegex();

    [GeneratedRegex(@"(?<k>top|left|right|bottom|width):\s*(?<v>-?\d+)px")]
    private static partial Regex CssPropertyRegex();

    [GeneratedRegex(@"<img src=[""']?(?:bilder/)?(?<src>[^""'>]+?)[""']? width=(?<w>\d+) height=(?<h>\d+)")]
    private static partial Regex ImageRegex();

    [GeneratedRegex(@"map=(?<id>\w+)&")]
    private static partial Regex MapIdRegex();

    [GeneratedRegex(@"an\((?<args>.*?)\);return true")]
    private static partial Regex TooltipCallRegex();

    [GeneratedRegex(@"""(?<v>[^""]*)""")]
    private static partial Regex TooltipArgumentRegex();

    [GeneratedRegex(@"<td id=""header\d+"">(?<name>.*?)<hr>")]
    private static partial Regex StationHeaderRegex();

    [GeneratedRegex(@"<div id=""ueberschrift"">(?<title>.*?)</div>")]
    private static partial Regex TitleRegex();

    [GeneratedRegex(@"class=""hauptfenster"" style=""width:(?<w>\d+)px; height:(?<h>\d+)px")]
    private static partial Regex MainWindowRegex();

    [GeneratedRegex(@"<tr><td rowspan=2 id=""nummer\w+"">#(?<number>\d+)</td><td id=""stationsname\w+""><a href=""/index\.php\?map=(?<sector>\w+)&[^""]*"">(?<name>.*?)</a></td>\s*(?<cells>.*?)</tr>\s*<tr><td id=""infozeile\w+"">(?<info>.*?</table></td></tr></table>)</td></tr>", RegexOptions.Singleline)]
    private static partial Regex StationRowRegex();

    [GeneratedRegex(@"<td rowspan=2 id=""[^""]+"">")]
    private static partial Regex CellSplitRegex();

    [GeneratedRegex(@"</td>\s*$")]
    private static partial Regex CellEndRegex();

    [GeneratedRegex(@"(?:<td id=""sektorbild""><img src=""bilder/(?<src>[^""]+)"" width=(?<w>\d+) height=(?<h>\d+)[^>]*></td>)?<td id=""sektorname"">(?<text>.*?)</td>")]
    private static partial Regex InfoBoxRegex();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex TagRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();
}
