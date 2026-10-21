# Panel controls

Open the installed HUD's **Panel** in sOU. Edit the desired fields and use **Speichern und anwenden**. Actions run immediately; ordinary text, references and images are replayable saved state. No selection in a reference/select field is allowed and has the documented fallback below.

## Display

| Field | Behavior |
| --- | --- |
| `display.caption` | The only live header caption. Explicit text has priority; blank uses the current competition name, or omits the caption when none is available. Existing saved captions remain compatible. |
| `display.scene` | Main output scene: `live`, `prematch`, `lineups`, `veto`, `break`, `postmatch`, `statistics`, `spotlight`. Blank means live; explicit subviews keep their scene. |
| `display.agentsOnly` | **Nur Agenten anzeigen**: when checked, ignore supplied player photos and show only the local CT/T agent for the actual observer digit. Applies to rosters, observed player, lineups and spotlight, including selected reference photos. Unchecked/missing means real photos take priority and agents remain the fallback for missing/failed photos. Team logos and sponsors are unaffected. Unknown side/slot retains the existing final fallback. |
| `display.hideRosters` | Hide both live team rosters. |
| `display.hideObservedPlayer` | Hide the live observed-player card. |
| `display.hideRadar` | Hide the live radar, including its unavailable indicator. |
| `display.hideKillfeed` | Hide the live killfeed. |
| `display.hideEconomy` | Hide the compact live economy footer beneath each roster. Full statistics remain available. |
| `display.hideRoundHistory` | Hide recent numbered round results during freeze/pause/timeout/intermission. History is already absent during active/bomb/defuse rounds; the statistics view keeps full history. |
| `display.hideSeries` | Hide the compact map-series cells below the radar and the team-aligned series-win marks in the header. |
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

`branding.logo` is an optional local broadcast image in the compact live caption and presentation header. `branding.sponsors` accepts additional local images, combined with competition sponsors supplied by sOU. Only permitted local images are loaded; image failures use a neutral fallback in presentation scenes. Failed live team logos are omitted. Missing or failed player portraits use a local CS2 agent for the actual side and observer digit, when known. Sponsor rendering is limited to 24 distinct local images, clipped to the small live sponsor rail.

## Radar

The default is the original [Boltobserv](https://github.com/boltgolt/boltobserv) radar at revision `0320883198e438cd6ae9fd490fa310027003727e`: Ancient, Anubis, Cache, Dust II, Inferno, Mirage, Nuke, Overpass, Train and Vertigo. Original transparent terrain, logo and buyzone images are bundled without modifications. Radar maps are not fetched from the Internet. Exact source map names are used; only the documented `workshop/<numeric-id>/<exact-map-name>` form is normalized. Similar names or arbitrary paths do not select another map.

The original format-4 metadata uses `resolution` (world units per 1024px image pixel) and bottom-origin X/Y `offset`. SVG projection is `x=(worldX+offset.x)/resolution/1024*100`, `y=100-(worldY+offset.y)/resolution/1024*100`. Original Nuke and Vertigo images show both levels together: every object applies its own original strict Z split and X/Y percentage offset. Observing another floor never swaps the complete image. The exact metadata, original source archive and provenance hashes ship with the HUD.

Buyzones are shown only during actual freeze/warmup states. Player direction drops, observer digits, observed-player ring, orange bomb carrier, flashed/dead markers and original projectile icons use supplied player/grenade data. Smoke with positive source effect time and flashes at the original 1.4-second source-lifetime threshold and individual flames use the original Bolt world sizes. Carried or exploded bombs are not displayed as dropped bombs. Each flash ID triggers the original 100/1200/2200ms presentation pulse once; repeated snapshots do not extend it. Source session/map/round resets clear the bounded (128 IDs per round) pulse state. No guessed buy duration, synchronized event, camera action or fabricated explosion is added.

A custom valid overview has priority. Otherwise a matching Bolt map is preferred, with the separately documented original Valve overview/calibration as fallback for additional supported maps or an unavailable Bolt image. Valve multi-image maps retain their actual observed-height selection. If no valid overview can load, a compact **Radar unavailable** indicator replaces the map; no empty grid or player-scaled pretend map is broadcast. The two local catalogs load once with bounded bodies, five-second deadlines and source-generated JSON parsing. Catalog/image failures leave the other HUD elements running.

To override the automatic map with your own local overview, configure all six fields:

1. `radar.overview`: local image selected through the image field.
2. `radar.mapName`: the exact source `Game.Map` string, including case (for example `de_example` for your own calibrated example).
3. `radar.minX`: world X at the image's left edge.
4. `radar.maxX`: world X at the right edge; must exceed minX.
5. `radar.minY`: world Y at the image's bottom edge.
6. `radar.maxY`: world Y at the top edge; must exceed minY.

Use a decimal point for numeric values. All boundaries must be finite and within the renderer's safety range (absolute value below 100 million). Calibration assumes image right = positive X and image up = positive Y; no unconfigured rotation or coordinate transformation is guessed. Use an image for which you know these bounds and have distribution/use rights. A wrong/missing map name, invalid bounds or failed image falls back to the matching bundled overview when available, otherwise to the compact unavailable indicator. Markers outside calibrated bounds are omitted instead of clamped into fake map corners.

HLAE positions are preferred only for a fresh frame no older than 750ms and a valid timeline; otherwise actual GSI positions are used. Fresh HLAE does not change GSI data freshness. Last known live death positions are retained within the round, with session/map/round/freezetime/timeline resets. No unknown historical death location is reconstructed.
