using EnbMaps.Models;
using EnbMaps.Tools;

namespace EnbMaps.Tests;

public class SiteParserTests
{
    private static readonly SiteParser.MenuSector earthMenu = new("Earth", "Earth", "Sol", null, null);

    private const string SECTOR_HTML = """
        <div id="ueberschrift">Sol System - Earth</div>
        <div class="hauptfenster" style="width:1600px; height:868px; clip: rect(0px, 1600px, 868px, 0px);">
        <div style="top:0px; left:880px;" id="null"><img src="bilder/map_achseX.png" width=1 height=868 border=0></div>
        <div style="top:380px; left:0px;" id="null"><img src="bilder/map_achseY.png" width=1600 height=1 border=0></div>
        <div style="top:-810px; left:370px;" id="grenze"><img src="bilder/map_grenze.png" width=1310 height=1 border=0></div>
        <div style="top:691px; left:370px;" id="grenze"><img src="bilder/map_grenze.png" width=1310 height=1 border=0></div>
        <div style="top:292px; left:392px;" id="nav"><img src="bilder/iconklein11.png" width=16 height=16 border=0 onmouseover='an("11","blau","Hidden Nav-Point","Asteroid ED5611","","-108.24","17.96","0.00");return true' onmouseout='aus();return true'></div><div style="top:294px; right:1211px; text-align:right;" id="blau">Asteroid ED5611</div>
        <div style="top:334px; left:702px;" id="nav"><img src="bilder/iconklein32.png" width=16 height=16 border=0 onmouseover='an("32","orange","Wormhole Exit","Hagoth's Weft","","-28.43","43.60","0.00");return true' onmouseout='aus();return true'></div><div style="bottom:534px; left:713px; text-align:left;" id="orange">Hagoth's Weft</div>
        <div style="top:436px; left:306px;" id="bild"><img src="bilder/earth Earth.png" width=799 height=799 border=0></div>
        <div style="top:197px; left:1096px;" id="nav"><a href='/index.php?map=Equatorial&zeige=n&breite=1600&hoehe=900' id=orange onmouseover='an("41","orange","Class Specific Gate","Accelerator to Equatorial Earth","Only for Terran Scout (TS)","50.40","39.31","-2.00");return true' onmouseout='aus();return true'><img src="bilder/iconklein41.png" width=16 height=16 border=0></a></div><div style="top:199px; left:1115px; text-align:left;" id="orange"><a href='/index.php?map=Equatorial&zeige=n&breite=1600&hoehe=900' id=orange>Accelerator to Equatorial Earth</a></div>
        <div class=station style="left:1410px;">
        <table cellspacing=0>
        <tr><td id="header50">Earth Station<hr></td></tr>
        </table>
        </div>
        </div>
        """;

    [Fact]
    public void ParseSector_ReadsOriginalTooltipValues()
    {
        var sector = SiteParser.ParseSector(SECTOR_HTML, earthMenu);

        Assert.Equal("Sol System - Earth", sector.Title);
        Assert.Equal(3, sector.Objects.Count);

        var nav = sector.Objects[0];
        Assert.Equal("Hidden Nav-Point", nav.Type);
        Assert.Equal("Asteroid ED5611", nav.Name);
        Assert.Equal("blau", nav.Style);
        Assert.Equal("11", nav.TooltipIcon);
        Assert.Equal("iconklein11.png", nav.Icon);
        Assert.Equal(-108.24, nav.X);
        Assert.Equal(17.96, nav.Y);
        Assert.Equal(0, nav.Z);
        Assert.Null(nav.TargetSectorId);
    }

    [Fact]
    public void ParseSector_HandlesApostropheInsideSingleQuotedAttribute()
    {
        var sector = SiteParser.ParseSector(SECTOR_HTML, earthMenu);

        Assert.Equal("Hagoth's Weft", sector.Objects[1].Name);
        Assert.Equal("Wormhole Exit", sector.Objects[1].Type);
    }

    [Fact]
    public void ParseSector_ReadsGateTargetAndInfo()
    {
        var gate = SiteParser.ParseSector(SECTOR_HTML, earthMenu).Objects[2];

        Assert.Equal("Equatorial", gate.TargetSectorId);
        Assert.Equal("Only for Terran Scout (TS)", gate.Info);
    }

    [Fact]
    public void ParseSector_ComputesLabelPlacementRelativeToIcon()
    {
        var sector = SiteParser.ParseSector(SECTOR_HTML, earthMenu);

        // right:1211px in a 1600px window => right edge at x=389, icon at left=392
        Assert.Equal(new LabelPlacement(LabelAlign.Right, -3, 2), sector.Objects[0].Label);

        // bottom:534px in an 868px window with a 14px line => top at 320, icon at top=334
        Assert.Equal(new LabelPlacement(LabelAlign.Left, 11, -14), sector.Objects[1].Label);
    }

    [Fact]
    public void ParseSector_ConvertsImagesAndBoundsToGameUnits()
    {
        var sector = SiteParser.ParseSector(SECTOR_HTML, earthMenu);

        Assert.InRange(sector.ReferenceScale, 4.3, 4.6);

        var image = Assert.Single(sector.Images);
        Assert.Equal("earth Earth.png", image.Src);
        Assert.Equal(MapImageLayer.Picture, image.Layer);
        Assert.InRange(image.Width, 170, 190);

        Assert.NotNull(sector.Bounds);
        Assert.True(sector.Bounds.MaxY > sector.Bounds.MinY);
        Assert.True(sector.Bounds.MaxX > sector.Bounds.MinX);
    }

    [Fact]
    public void ParseSector_ReadsStationPanel()
    {
        var station = Assert.Single(SiteParser.ParseSector(SECTOR_HTML, earthMenu).Stations);

        Assert.Equal("Earth Station", station.Name);
        Assert.Contains("header50", station.Html);
    }

    [Fact]
    public void FitTransform_RecoversScaleAndOrigin()
    {
        static (MapObject, int, int) Nav(double x, double y) => (
            new MapObject { Id = "t", Type = "t", Name = "t", TooltipIcon = "30", Icon = "i", Style = "grau", X = x, Y = y, Z = 0 },
            (int)Math.Round(500 + (x * 4)) - 8,
            (int)Math.Round(300 - (y * 4)) - 8);

        var (scale, originX, originY) = SiteParser.FitTransform([Nav(-50, 20), Nav(30, -10), Nav(75, 60)], null, null);

        Assert.Equal(4, scale, 3);
        Assert.Equal(500, originX, 1);
        Assert.Equal(300, originY, 1);
    }

    [Fact]
    public void ParseMenu_ReadsSystemsSectorsAndStartingInfo()
    {
        const string html = """
            <li><a href="" id="system">Sol</a><ul>
                <li><a href="/index.php?map=AkeronsGate&zeige=n&breite=1600&hoehe=900">Akerons Gate</a></li>
                <li><a href="/index.php?map=Equatorial&zeige=n&breite=1600&hoehe=900" id="terran">Equatorial Earth <span id="info">(Terran&nbsp;Scout)</span></a></li>
            </ul></li>
            """;

        var menu = SiteParser.ParseMenu(html);

        Assert.Equal(2, menu.Count);
        Assert.Equal(new SiteParser.MenuSector("AkeronsGate", "Akerons Gate", "Sol", null, null), menu[0]);
        Assert.Equal(new SiteParser.MenuSector("Equatorial", "Equatorial Earth", "Sol", "Terran Scout", "terran"), menu[1]);
    }

    [Theory]
    [InlineData("Ceres /&nbsp;Thule", "Ceres / Thule")]
    [InlineData("Jobs<br>(Lvl 105)", "Jobs (Lvl 105)")]
    [InlineData("<b>Bold</b>  text", "Bold text")]
    public void Text_StripsTagsAndDecodesEntities(string html, string expected) =>
        Assert.Equal(expected, SiteParser.Text(html));
}
