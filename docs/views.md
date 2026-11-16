# Outputs and scenes

Select the installed HUD and the desired **Ansicht** in sOU, then use **OBS-Adresse kopieren**, **Über CS2 anzeigen** or **Im Browser öffnen**. Use the address supplied by sOU; do not guess the host port. Each output is an independent view of the same HUD version and panel state.

| Output | Relative route | Contents |
| --- | --- | --- |
| Main live | package root | One compact scoreboard with central round/state and side-aligned series wins, two-line five-player corner rosters with weapon/utility icons and economy footers, compact observed player, numbered round history only during freeze/pause/timeout/intermission, real killfeed, original automatically calibrated Boltobserv radar and compact map-series cells below it. The central game area stays clear during normal output. |
| Prematch | `views/prematch` | Managed teams/logos, series, competition and explicit planned-start countdown before GSI. |
| Lineups | `views/lineups` | Actual metadata-bound team rosters and local portraits, with matching live values where available. |
| Map veto | `views/veto` | Explicit ordered picks/bans/decider, acting team only when assigned, stored map order/status/results. |
| Break | `views/break` | Operator scene title/subtitle, teams/series and explicit planned start. |
| Results | `views/postmatch` | Managed series score, verified winner where supportable, stored map results and available player statistics. |
| Statistics | `views/statistics` | Full player table with K/D/A/MVP/score, round damage/kills, full team economy summaries, recorded ADR/HS and honest coverage, plus all supplied numbered round results regardless of the live phase. |
| Spotlight | `views/spotlight` | Selected public player/team/match references, portrait and available matching live statistics; operator headline/subline. |

The main output's **Main output scene** select can choose any scene; no selection means live. Explicit `views/*` URLs keep their own scene even when that select changes. This lets OBS keep separate prematch, live and statistics sources without switching every source together.

All thirteen subviews are declared in `hud.json`. sOU serves the same index and rewrites the base to the shared package root, so framework files, CSS, SDK session and local media continue to work. No duplicate HTML pages or external router is needed.

Use a **1920 × 1080** OBS Browser Source with a transparent background. All views share a uniformly scaling 16:9 canvas measured against their containing output. Embedded previews, 4K, ultrawide, portrait and height-limited windows retain the same composition with transparent unused space. All thirteen presentation views share their frame, header, map label, typography, palette and spacing. Text, icons and utility indicators scale together, without fixed-pixel icon floors or scrollbars. Long text wraps or truncates within its assigned area. Lower-third actions reserve space consistently in both original and recording views. Reduced-motion preference is respected.

Live positions use the same proportional composition at every size. At 1920×1080 the normal output leaves x360–1560/y160–930 free, with 312×318 corner rosters, a 288px map radar and a 520×82 observer card below that area. An explicit **Show now** lower-third production action can temporarily cross the game area. Missing live portraits/team logos are omitted; full presentation scenes keep neutral monogram fallbacks.

Panel changes apply to every output of that HUD/version. Installing an update leaves already running outputs on their existing package. Select the new version and reload/reopen each output deliberately.

Six additional recording scenes are documented in [Recording and economy showcase](recording-showcase.md). They use passive 16:9 broadcast compositions that scale with the existing HUD canvas. Their dedicated host panel sections control rounds, phases, metrics, player focus and visual pages; the output has no controls or scrolling.

Layout verification: `node tests/browser/layout.mjs <published-package-folder>` exercises all fourteen views with ten live players, populated vetoes, eight output sizes, an embedded 960 x 540 output and lower-third space. It uses the isolated SDK simulator; it is not native CS2/OBS acceptance.
