# 0.2.1

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
