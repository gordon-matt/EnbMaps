# ENBMaps

An archived and modernized copy of [enbmaps.de](http://enbmaps.de) — the interactive star maps for
*Earth & Beyond* created by Stefan Schleusener (aka Dodger).

The new site is a static Blazor WebAssembly app (installable PWA, works offline) that needs no server, so it can
be hosted on GitHub Pages.

## What the original site was

Recovered from the live site (v3.0.4) in October 2026:

- A PHP application that renders every page on the server. The only JavaScript (`scripts2012.js`) reloads the
  page with `?breite=<width>&hoehe=<height>` (German for width/height) so the server can lay the map out
  for the browser window, and shows the mouse-over boxes. The server also requires a PHP session cookie.
  That is why generic crawlers/archivers (Zimit, Kiwix, ...) fail on it.
- Pages: galaxy overview (`index.php`), one map per sector (`index.php?map=Earth`), station list
  (`index.php?stationlist`), search (`POST index.php?suche`). `zeige=n` shows names and `zeige=kn` shows names plus
  coordinates.
- Maps are absolutely positioned `<div>`s. Each nav carries its data in an inline call
  `an(icon, style, type, name, info, x, y, z)`, which holds the original game coordinates. Gates link to their target
  sector. Planet/station pictures, gravity wells, axes and sector boundaries are positioned images. The overview's
  gateway lines are a server-generated image (`gatewayline2012.inc.php`).

## Repository layout

| Path | Contents |
| --- | --- |
| `data/original/` | Untouched archive of the original site: rendered HTML pages, CSS, JS and all images (`bilder/`). |
| `data/normalized/enbmaps.json` | The normalized dataset extracted from the archive (see [DATA.md](DATA.md)). |
| `EnbMaps.Models` | Dataset records, shared by the tool and the web app. |
| `EnbMaps.Tools` | Console tool that scrapes the original site and extracts the dataset. |
| `EnbMaps` | The Blazor WebAssembly app. |
| `EnbMaps.Tests` | xUnit tests for the parser, including a run over the full archive. |

## Usage

```powershell
# Re-download the original site into data/original (only needed while enbmaps.de is still online)
dotnet run --project EnbMaps.Tools -- scrape

# Parse data/original into data/normalized/enbmaps.json and copy it plus the images into EnbMaps/wwwroot
dotnet run --project EnbMaps.Tools -- extract

# Run the app locally
dotnet run --project EnbMaps
```

`extract` must run at least once before starting the app. Its output in `EnbMaps/wwwroot/bilder` and
`EnbMaps/wwwroot/data` is git-ignored, so the images are stored only once (in `data/original`).

## Features

- Galaxy overview, sector menu, sector maps, station list and search, styled with the original artwork and CSS.
- Pan (drag), zoom (mouse wheel, pinch, double-click, buttons) and a reset button.
- Original mouse-over boxes, plus click-to-select for touch devices.
- "Name" and "XYZ" toggles as on the original, plus layer toggles (nav types, pictures, gravity wells).
- Search results open the sector and centre on and highlight the nav.
- Links using the old URL scheme (`index.php?map=Earth`, `index.php?stationlist`) redirect to the new routes.
- Offline support through the Blazor PWA service worker.

## Deployment (GitHub Pages)

`.github/workflows/deploy.yml` runs the tests, extracts the dataset, publishes the app, sets `<base href>` to
`/<repository name>/` and pushes the result to the `gh-pages` branch. In the repository settings, set
**Pages → Source** to the `gh-pages` branch. `404.html` handles deep links such as `/EnbMaps/map/Earth`.

## Known limitations

- Three stations (Pax Remar, Chavez Capital Ship, Yasuragi) have no information panel on the original site either.
- `bilder/icon10.gif` (the tooltip icon for "Hidden Object") could not be downloaded, so the small map icon is used
  instead.
- Picture/boundary positions are derived from the original pixel layout using a scale fitted to the navs'
  game coordinates, so they carry the original's whole-pixel rounding. Nav coordinates are the original values.
- The original search was a server-side POST. The new search runs client-side over sectors and navs. For "Earth"
  it returns the same 40 results.

## Copyright

All map data, artwork and the original styles belong to their respective authors (enbmaps.de / Stefan
Schleusener, and Earth & Beyond © Westwood Studios / Electronic Arts). They are preserved here for archival
purposes only.
