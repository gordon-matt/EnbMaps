# ENBMaps dataset

`data/normalized/enbmaps.json` is generated from the archived pages in `data/original/html` by
`dotnet run --project EnbMaps.Tools -- extract`. The C# records live in `EnbMaps.Models`.

## Structure

- `DatasetVersion`, `SiteVersion` (original site version, e.g. `v3.0.4`), `SourceUrl`, `RetrievedUtc`
- `Overview`: the galaxy overview in its original pixel layout (`Width` x `Height` reference window):
  system background images, sector icons and labels, and the gateway lines image.
- `Systems`: star systems in original menu order, each with its sector ids.
- `Sectors`: one entry per sector map:
  - `Id`: the original `map=` parameter (e.g. `Earth`, `PlanetZweihander`).
  - `Name`, `SystemName`, `Title`, `MenuInfo`/`MenuStyle` (starting sectors, e.g. "Terran Scout").
  - `Objects`: navs with the original tooltip values: `Type`, `Name`, `Info`, `Style` (colour), `TooltipIcon`,
    `Icon`, `X`/`Y`/`Z`, plus `TargetSectorId` for gates and landable planets, and `Label` (the original label
    position relative to the icon, in reference pixels).
  - `Images`: planet/station pictures and gravity wells, in game units (top-left corner, width, height).
  - `Bounds`: the red sector boundary, in game units.
  - `Stations`: station information panels (original markup).
  - `ReferenceScale`: pixels per game unit in the original reference layout.
- `StationList`: the original station list: column icons, and per station the sector, faction and the original
  cell markup (terminal availability, vendor levels).

## Coordinates

Game coordinates use X to the right and Y up, exactly as shown in the original tooltips.

Values marked *original* below are copied verbatim. *Derived* values were computed from the original pixel
layout, which was rendered at a 1600x900 browser window (868px map area):

| Value | Provenance |
| --- | --- |
| Nav type, name, info, X/Y/Z, gate targets | original |
| Label placement | original (pixel offsets) |
| Image and boundary positions/sizes | derived: `world = (pixel - origin) / scale`, with origin and scale least-squares fitted to the navs' icon centres and original coordinates |
| Overview layout | original (pixels) |

## Encoding

The original pages declare ISO-8859-1 but are almost entirely UTF-8. A few PHP string literals are Latin-1
(e.g. `NPC´s`). The extractor decodes UTF-8 and maps any invalid byte to Latin-1 (`SourceEncoding`).
