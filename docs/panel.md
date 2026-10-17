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
| `display.hideEconomy` | Hide the live team economy strip. |
| `display.hideRoundHistory` | Hide the live round-history strip. |
| `display.hideSeries` | Hide the compact live map-series strip. |
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

`branding.logo` is an optional local broadcast image. `branding.sponsors` accepts additional local images, combined with competition sponsors supplied by sOU. Only permitted local images are loaded; image failures use a neutral fallback. Sponsor rendering is limited to 24 distinct local images.

## Radar

The default plot uses real source positions with dynamic relative bounds and is labeled **Relative world positions · no map**. It is not an accurate CS2 minimap. It includes real player directions/observer digits/death markers, bomb and grenade positions where supplied.

To use a local overview image, configure all six fields:

1. `radar.overview`: local image selected through the image field.
2. `radar.mapName`: the exact source `Game.Map` string, including case (for example `de_example` for your own calibrated example).
3. `radar.minX`: world X at the image's left edge.
4. `radar.maxX`: world X at the right edge; must exceed minX.
5. `radar.minY`: world Y at the image's bottom edge.
6. `radar.maxY`: world Y at the top edge; must exceed minY.

Use a decimal point for numeric values. All boundaries must be finite and within the renderer's safety range (absolute value below 100 million). Calibration assumes image right = positive X and image up = positive Y; no unconfigured rotation or coordinate transformation is guessed. Use an image for which you know these bounds and have distribution/use rights. A wrong/missing map name, invalid bounds or invalid image path keeps the honest relative plot. Markers outside calibrated bounds are omitted instead of clamped into fake map corners.

HLAE positions are preferred only for a fresh frame no older than 750ms and a valid timeline; otherwise actual GSI positions are used. Fresh HLAE does not change GSI data freshness. Last known live death positions are retained within the round, with session/map/round/freezetime/timeline resets. No unknown historical death location is reconstructed.
