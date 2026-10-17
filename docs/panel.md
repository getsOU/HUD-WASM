# Panel controls

Open the installed HUD's **Panel** in sOU. Edit the desired fields and use **Speichern und anwenden**. Actions run immediately; ordinary text, references and images are replayable saved state. No selection in a reference/select field is allowed and has the documented fallback below.

## Display

| Field | Behavior |
| --- | --- |
| `display.caption` | Scoreboard caption. Blank uses `sOU BASIC`. The existing saved caption remains compatible. |
| `display.scene` | Main output scene: `live`, `prematch`, `lineups`, `veto`, `break`, `postmatch`, `statistics`, `spotlight`. Blank means live; explicit subviews keep their scene. |
| `display.hideRosters` | Hide both live team rosters. |
| `display.hideObservedPlayer` | Hide the live observed-player card. |
| `display.hideRadar` | Hide the live radar/position plot. |
| `display.hideKillfeed` | Hide the live killfeed. |
| `display.hideEconomy` | Hide the compact live economy footer beneath each roster. Full statistics remain available. |
| `display.hideRoundHistory` | Hide the live round-history strip. |
| `display.hideSeries` | Hide series-map detail in the compact live header line. |
| `display.hideSponsors` | Hide sponsor images in every scene. |

Every Hide checkbox is an opt-out: **only true hides**. False, missing and the host's initial unchecked defaults leave the component enabled. A caption-only save does not unexpectedly hide components. Source-dependent elements still need the corresponding real data.

## Production

| Field | Behavior |
| --- | --- |
| `production.title` / `subtitle` | Presentation scene heading/subheading. Blank title uses the scene's own heading. |
| `production.ticker` | Static bottom text line; long text truncates rather than animating continuously. |
| `production.replayLabel` | Operator-entered replay/status text, explicitly marked **MANUAL**. It does not detect replay state or control a camera. |
| `production.lowerTitle` / `lowerSubtitle` | Text for the lower-third overlay. Saving these fields does not trigger it. |
| `production.lowerThird` | Action values `show` / `hide`: **Show now** displays current text, **Hide now** removes it. Duplicate action IDs do not replay it. Reconnection receives saved text without replaying old actions. |

The lower third is transient; a new source session clears it. It reserves space below presentation scenes rather than hiding their selected reference text.

## Spotlight

`feature.player`, `feature.team`, `feature.match` use the public player/team/match selectors. Their names and available local images come from the public reference response. Deleted/missing references are identified. A chosen player receives live stats only when the source identity matches; otherwise the real selection stays visible with unknown stats. Without a player selection, the actual observed player is used. The selected team/match cards show matching current context only when IDs agree.

`feature.headline` and `feature.subline` are operator text in the spotlight scene. These fields do not automatically designate someone as MVP or infer an award.

## Branding

`branding.logo` is an optional local broadcast image in the compact live caption and presentation header. `branding.sponsors` accepts additional local images, combined with competition sponsors supplied by sOU. Only permitted local images are loaded; image failures use a neutral fallback in presentation scenes. Live portraits/team logos omit failed boxes. Sponsor rendering is limited to 24 distinct local images, clipped to the small live sponsor rail.

## Radar

Known exact source map names automatically select an included original Valve radar image and fixed real world calibration. No setup is required for Dust II, Mirage, Inferno, Ancient, Anubis, Nuke, Overpass, Train, Vertigo, Italy, Office or Cache; additional explicit legacy/night IDs are listed in `assets/radar/catalog.json`. No map-name aliases are guessed. The catalog loads once from the installed package with a five-second deadline, bounded body and generated JSON metadata; failure leaves other HUD data running.

Each catalog layer defines original image dimensions, `posX`, `posY` and world units per pixel (`scale`). Projection is `x=(worldX-posX)/(width*scale)` and `y=(posY-worldY)/(height*scale)`. Players moving across the map do not resize these bounds. Source-defined A/B sites are labeled when supplied by the local catalog.

Nuke, Train and Vertigo include genuine vertical layers. The actual observed player's supplied Z coordinate selects a layer (`minZ` inclusive, `maxZ` exclusive); fresh matching HLAE Z has priority over GSI. A player on another level is dimmed and marked higher/lower. With no observed height, the main layer is shown with **floor unknown** rather than claiming the viewed floor. Flat maps do not need Z for layer selection.

Player directions/observer digits/death markers, bomb, grenades and inferno flame points remain connected to actual source data. An unknown map, missing catalog or failed bundled image uses a compact plot labeled **Relative positions · map unavailable**. Its relative bounds follow the supplied positions; it does not claim to be an accurate minimap.

To override the automatic map with your own local overview, configure all six fields:

1. `radar.overview`: local image selected through the image field.
2. `radar.mapName`: the exact source `Game.Map` string, including case (for example `de_example` for your own calibrated example).
3. `radar.minX`: world X at the image's left edge.
4. `radar.maxX`: world X at the right edge; must exceed minX.
5. `radar.minY`: world Y at the image's bottom edge.
6. `radar.maxY`: world Y at the top edge; must exceed minY.

Use a decimal point for numeric values. All boundaries must be finite and within the renderer's safety range (absolute value below 100 million). Calibration assumes image right = positive X and image up = positive Y; no unconfigured rotation or coordinate transformation is guessed. Use an image for which you know these bounds and have distribution/use rights. A wrong/missing map name, invalid bounds or failed image falls back to the matching bundled overview when available, otherwise to the labeled relative plot. Markers outside calibrated bounds are omitted instead of clamped into fake map corners.

HLAE positions are preferred only for a fresh frame no older than 750ms and a valid timeline; otherwise actual GSI positions are used. Fresh HLAE does not change GSI data freshness. Last known live death positions are retained within the round, with session/map/round/freezetime/timeline resets. No unknown historical death location is reconstructed.
