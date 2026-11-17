# Panel controls

Open the installed HUD's **Panel** in sOU. Edit the desired fields and use **Speichern und anwenden**. Actions run immediately; ordinary text, references and images are replayable saved state. No selection in a reference/select field is allowed and has the documented fallback below.

## Display

| Field | Behavior |
| --- | --- |
| `display.caption` | Fallback for the left live heading when competition data is absent. Normally the left field shows tournament and season, the center shows the fixed white sOU wordmark, and the right field shows the competition section path and round. |
| `display.scene` | Main output scene: `live`, `prematch`, `lineups`, `veto`, `break`, `postmatch`, `statistics`, `spotlight`. Blank means live; explicit subviews keep their scene. |
| `display.agentsOnly` | **Agents only**: when checked, ignore supplied player photos and show only the local CT/T agent for the actual observer digit. Applies to rosters, observed player, lineups and spotlight, including selected reference photos. Unchecked/missing means real photos take priority and agents remain the fallback for missing/failed photos. Team logos and sponsors are unaffected. Unknown side/slot retains the existing final fallback. |
| `display.hideRosters` | Hide both live team rosters. |
| `display.horizontalPlayercards` | **Playercards horizontal**: arrange each team's five cards in a horizontal row along the bottom, with the observer card in the center. Both teams use the same internal content order (name/stats left-aligned, weapon left, HP right, utility left, equipment right); observer digits and side strips sit on the left for both teams. Remaining HP fills the full card background from the bottom; damage clears it from top to bottom. Dead cards are shorter and keep portrait/name/statistics without the large death icon or lower equipment row. Unchecked/missing keeps the original vertical rosters. Applies only to the live scene; switching layouts preserves player data, portrait effects and observed-player highlighting. |
| `display.hideObservedPlayer` | Hide the live observed-player card. |
| `display.hideRadar` | Hide the live radar, including its unavailable indicator. |
| `display.hideTrades` | Hide possible-trade icons and teammate names within killfeed rows. Unchecked or missing keeps them visible. Requires an SDK-provided trade mapping; never infers one from visible kills. |
| `display.hideKillfeed` | Hide the live killfeed. |
| `display.hideRoundHistory` | Hide recent numbered round results during freeze/pause/timeout/intermission. History is already absent during active/bomb/defuse rounds; the statistics view keeps full history. |
| `display.hideSeries` | Hide the compact map-series cells below the radar and the team-aligned series-win marks in the header. |
| `display.hideSponsors` | Hide sponsor images in every scene. |

Every Hide checkbox is an opt-out: **only true hides**. False, missing and the host's initial unchecked defaults leave the component enabled. A caption-only save does not unexpectedly hide components. Source-dependent elements still need the corresponding real data.

## Matchbar bomb status

Bomb actions use icons without visible captions. Plant and defuse rings fill from the SDK's action countdown, interpolated on the existing synchronized render clock. Standard CS2 action lengths are 3.2 seconds for planting and 5/10 seconds for defusing with/without a kit. The kit belongs to the bomb state's `playerSteamId`; another player's kit is never substituted. Missing kit/timing, missing clock anchors or out-of-range action durations omit the progress ring rather than starting an artificial spinner. Modified game modes with different action durations are not fully described by the published SDK contract.

Only planting/defusing with known progress have a ring. Afterplant and terminal states show the icon alone, without a ring, track or filled circular background. The existing 8% double-pulse motif accelerates with `100 + 900 * clamp(remaining / 40, 0, 1)` milliseconds per cycle. Remaining time comes from the SDK and its synchronized render clock; 40 seconds is an explicit visual reference, not a supplied total duration. Custom bomb lengths therefore remain an approximation. Native animation playback-rate changes preserve pulse phase without an extra timer, data subscription or HTTP request. Color changes transition over 220 ms; existing SVG shapes crossfade over 180 ms rather than morphing incompatible paths. The bomb turns red at 10 seconds. At five seconds the pulse stops completely. The entire final-five-second stage uses one 300 ms double-shake cycle: 200 ms of movement followed by 100 ms at rest. Only amplitude grows toward zero (4 to 10 degrees); speed and pauses stay constant, including at three seconds. No pulse or residual scale transition overlaps these final stages. Animation stages stay stable between thresholds instead of restarting on every countdown update. Transitions use at most half the remaining action time, and terminal symbols appear immediately. At an estimated zero the critical shake continues until the SDK supplies explosion, defuse or another real state change; a zero/missing countdown must not freeze it early. No explosion/defuse event is inferred. Paused/stale data freezes timing and holds the shake; reduced-motion preferences disable shaking. A finite bomb countdown at or below zero remains critical until a terminal state arrives. Missing timing retains only the last known warning for the same session/match/map/round, including a temporarily interrupted defuse; a valid new timer value is accepted. Terminal states, freeze and context changes clear retained warnings. Reaching zero does not synthesize a planted, defused or exploded event. Abort, terminal and context changes use the newly supplied SDK state.

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

`branding.logo` is an optional local broadcast image in presentation headers. The live scoreboard uses the fixed local sOU wordmark in white. `branding.sponsors` accepts additional local images, combined with competition sponsors supplied by sOU. Only permitted local images are loaded; missing or failed competition and sponsor images are omitted. Failed live team logos are omitted. Missing or failed player portraits use a local CS2 agent for the actual side and observer digit, when known. Sponsor rendering is limited to 24 distinct local images. In every view, one sponsor rotates directly left of the right-aligned competition logo in the upper-right area of each view's layout. Subview logos align with the header's inner edges. Rotation duration, logo sizes and Hide sponsors apply to all views. Presentation headers retain only the optional broadcast logo. Their independent height controls preserve each image aspect ratio; widths follow the content without fixed slots.

## Radar

The default is the original [Boltobserv](https://github.com/boltgolt/boltobserv) radar at revision `0320883198e438cd6ae9fd490fa310027003727e`: Ancient, Anubis, Cache, Dust II, Inferno, Mirage, Nuke, Overpass, Train and Vertigo. Original transparent terrain, logo and buyzone images are bundled without modifications. The separate Boltobserv/Simple Radar branding overlay is not displayed. Radar maps are not fetched from the Internet. Exact source map names are used; only the documented `workshop/<numeric-id>/<exact-map-name>` form is normalized. Similar names or arbitrary paths do not select another map.

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

## Recording broadcasts

Each recording scene has its own host panel section. The output is passive and has no form controls, navigation, refresh button or scroll area.

| Section | Operator settings |
| --- | --- |
| Team Economy | Title, money/equipment, received phase, final round and round window |
| Loadout comparison | Title, two rounds and phases, player focus, inventory page |
| CT/T statistics | Title, CT/T/unassigned sample selection, player focus |
| Multikills | Title, player focus |
| Buy classes and round end | Title, round, team/unassigned group, player focus, inventory page |
| Economy evidence | Title, round, player focus, event page and carryover-weapon page |

Every section includes **Synchronize recording**. This cancels and replaces the shared push subscription with a new SDK baseline. The other views' actions have no effect. There are no recording data HTTP reads or polling. Blank round fields choose the newest saved round; comparison A chooses the preceding saved round. Explicit unavailable rounds/phases stay unavailable. All players are shown simultaneously using adaptive columns/rows. Only event and inventory pages remain. Multikills have no side selector. See [recording showcase](recording-showcase.md).

## Player image alignment

Display > **Player image alignment** defaults to **Top center**. Top/center/bottom and left/center/right combinations provide nine positions. Existing panel configurations without this field also use top center. The choice applies to live vertical/horizontal cards, observer and MVP portraits, lineup/spotlight views and recorded player images. Images keep their existing cover/contain fitting and card dimensions; cover-cropped portraits retain the chosen edge. Effect masks and agent fallbacks use the same position. Logos and other artwork retain their own alignment.

## Possible trades

The killfeed uses `KillFeedEntry.TradedKill.Victim` from SDK `0.3.0-preview.9` (sOU Desktop `0.3.42` or newer), with `killfeed.trades.v1` declared as an optional capability. Missing mappings produce no indicator. Nested chains show only the immediate teammate. Indicators share the row identity and lifetime; late mappings and alias changes update the existing row. See the [SDK reference](https://www.getsou.app/docs/hud-sdk-referenz?ui_lang=de#killfeed-trades).
