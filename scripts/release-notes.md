# 0.4.2

Match NYXIA radar behavior: 50 ms linear player/projectile movement, stable marker identities and resets at source, session, map, round and floor boundaries. A fresh HLAE player frame is authoritative as a whole; missing HLAE pawns no longer jump to older GSI positions. GSI is used only when that frame is absent or expires at 750 ms. The WASM renderer also refreshes its clock on receipt, preventing new HLAE frames from being incorrectly rejected against an old timestamp.

Map series now show the explicit pick team, or DECIDER, in Live and presentation views. Pick ownership follows veto MapId and TeamId, independently of current CT/T sides. Missing or conflicting decisions remain unresolved.

Import 0.4.2 and reopen the output. Existing releases, panel values, all eight views, original Boltobserv assets, GPL terms and public MIT SDK 0.2.0-preview.4 are preserved. Browser movement checks do not replace native CS2/OBS acceptance.
# 0.4.1

Corrects absent weapon handling in the live rosters, observed-player card and detail inventory. Dead players no longer display stale weapon icons, names or ammunition. Alive players with no active/reloading weapon leave the current weapon area empty; holstered inventory is never substituted for a held weapon. A genuinely supplied active weapon with an unknown identity still uses its distinct fallback. Actual reloading weapons and source ammunition remain visible; unknown health is not treated as death. Historical killfeed weapons are unchanged.

Install `sou-basic-wasm-0.4.1.souhud`, select the new version and reopen outputs. The 0.4.0 composition, original Boltobserv radar, all views/panels, GPL license and MIT public SDK `0.2.0-preview.4` remain unchanged. Previous release archives are preserved.

## 0.4.0

Replaces the live header's technical metadata strip with one optional caption, central source round/state label and correctly side-aligned series-win marks. Actual timer and round scores remain dominant. Compact ordered map-series cells below the radar show complete stored scores and exact current-map identity; an unrelated source map never claims a current configured slot. No repeated phase, raw map ID or partial `13:—` result is broadcast.

Numbered recent round results appear only during actual freeze/pause/team timeout/intermission. Live combat, bomb/defuse and round-over remove the area entirely. The statistics scene retains complete supplied history. Time-expired, hostage and unknown results no longer become elimination skulls.

Adds the original Boltobserv radar maps, format-4 coordinates, transparent terrain/logo/buyzone layers, player marker shapes and utility images. Every object uses the original Nuke/Vertigo split offsets on the composite images. Buyzones follow source freeze/warmup; observed ring, observer digits, directions, bomb carrier, flashes/deaths, projectile icons, source smoke/flash effects and individual flames remain connected to actual public fields. Valid local custom maps and separate Valve fallbacks remain supported. Missing maps show a compact unavailable indicator, with no replacement grid or permanent diagnostics.

This release is GPL-3.0-only following the original Boltobserv integration. Original source, map-specific license texts, provenance/hashes and separate Valve notices ship with the package. Earlier MIT releases and the MIT public SDK remain unchanged. See `LICENSING.md`.

Install `sou-basic-wasm-0.4.0.souhud`, select the new version and reopen outputs. All eight views, saved compatible caption/Hide controls, transparent canvas and public SDK `0.2.0-preview.4` remain supported. Native CS2/OBS capture is a separate practical acceptance step.
# 0.3.0

Rebuilds the live composition around the game: compact two-line bottom-corner rosters, small header/series/history, compact observer card and sponsor rail, with economy in roster footers. The central game area remains clear during normal output. Missing live portraits and logos no longer create replacement boxes; presentation scenes retain neutral fallbacks.

Adds recognizable local weapon/utility silhouettes and equipment/status/kill flags, actual firearm ammunition and known utility counts. Source GSI/HLAE names decode one entity layer and remain normal escaped text. All eight views, detailed inventories/statistics, optional metadata, panel defaults and lifecycle guards remain available.

Known maps now automatically use original Valve radar images with their real fixed calibration, including Dust II. Nuke/Train/Vertigo use actual observed height for genuine floor layers, with unknown floors identified. Fresh HLAE/GSI positions, direction, observer digits, deaths, bomb, utility and flames stay connected. Custom exact-map calibration remains an override; unavailable maps/images use an honest compact fallback.

Own HUD code stays MIT. Included original Valve game artwork is separately attributed in `LICENSES/VALVE-ASSETS.md` and `LICENSES/valve-assets.json`; no MIT relicensing or unrestricted artwork reuse is claimed. These notices and source hashes ship in the HUD archive.

Install `sou-basic-wasm-0.3.0.souhud`, select version `0.3.0` and reopen outputs. Compatible saved captions/Hide choices remain supported. SDK packages stay at `0.2.0-preview.4`; previous releases and active outputs are unchanged. Browser verification is separate from native CS2/OBS capture.

## 0.2.1

Fixes image-load error handling: unavailable local player portraits, team/competition logos, branding and sponsor images now fall back to their neutral monograms. An unavailable calibrated overview returns the radar to its clearly labeled relative world-position plot. The existing centered spotlight fallback and transparent output are retained.

Install `sou-basic-wasm-0.2.1.souhud`, select version `0.2.1` and reopen outputs. Saved compatible panel settings are inherited. Public SDK dependencies stay at `0.2.0-preview.4`; earlier release archives and already running outputs remain unchanged.

## 0.2.0

Expands the public HUD into a complete broadcast reference project: main live output plus seven real subviews for prematch, lineups, veto, break, results, statistics and spotlight. Adds inventories/utility/statuses, complete-or-unknown team economy, round history, explicit round-win banners, real HLAE killfeed, own MIT radar renderer, verified optional local lineup/portrait metadata, competition/series/results, public reference spotlights, sponsors/branding/ticker and deduplicated lower-third production actions.

The common panel covers all nine public field types. Every Hide checkbox remains opt-out and defaults to enabled content; existing caption and Hide preferences remain compatible. Explicit subviews keep their own scene independently of the main-output selector. Missing source data is never fabricated. The default radar is clearly labeled as a relative world-position plot, with optional exact-map/local-image calibration.

Package ID stays `sou-basic-wasm`. Install `sou-basic-wasm-0.2.0.souhud`, select the new version and reopen outputs. Earlier releases and active outputs remain unchanged. Public SDK packages stay at `0.2.0-preview.4`; the new authored components use no GPL HUD assets. Native CS2/OBS capture remains a separate practical check.

## 0.1.2

Team rosters and the observed-player section are now enabled by default, including after the first panel update or a caption-only save. The panel uses **Hide team rosters** and **Hide observed player**: only a checked Hide field disables its section. The observed-player card still requires an observed player in the source data.

Install `sou-basic-wasm-0.1.2.souhud`, select version `0.1.2` and reopen the output. Your saved caption is inherited when sOU initializes the new version's panel. Visibility fields have new identifiers (`display.hideRosters` and `display.hideObservedPlayer`), so configure any sections you want hidden once again after upgrading. Existing releases, saved settings and already running outputs remain unchanged.

The transparency fix from `0.1.1` is retained. Public SDK dependencies stay at `0.2.0-preview.4`; no SDK or sOU desktop update is required.

## 0.1.1

Fixes an opaque black background when the HUD is embedded in sOU's overlay. The root document no longer forces a dark browser color scheme; the HUD's panel colors, transparent page backgrounds and layout remain the same.

Install `sou-basic-wasm-0.1.1.souhud`, select version `0.1.1` in sOU and reopen the output. Existing `0.1.0` packages and running outputs are unchanged. SDK dependencies remain at `0.2.0-preview.4`.

Source/build checks and isolated browser checks are separate from a native CS2/OBS capture test.
