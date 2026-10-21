# 0.4.11

This release updates the icons, adds side-specific player portrait placeholders and corrects the live player-card proportions.

- Import 81 weapon, grenade, status and killfeed SVGs from OpenHud-React-Hud, with reviewed optical sizing and packaged source/license records. Repair Flashbang's missing SVG namespace so it loads as a standalone image.
- Add twenty local CS2 agent portraits from CSGOSKINS.GG: a different CT and T agent for each observer digit 0–9. Supplied player portraits take priority; missing or failed images use the actual side/slot placeholder. Side changes update the agent. Unknown side/slot values retain their existing final fallback.
- Match player-card proportions, portrait widths, identity insets, loadout padding and row gaps to the broadcast reference. Keep three-digit Health values fully visible on both sides, including scaled outputs and fallback fonts.
- Remove the bottom-right connection/preview status overlay in all scenes.
- Set `infoUrl` to https://github.com/getsOU/HUD-WASM.

All eight views, existing panel settings, source handling, radar, inventory selection and chronological killfeed remain available. The public SDK stays at 0.2.0-preview.4; the package ID remains `sou-basic-wasm`. Artwork retains the separately documented upstream rights.

Validation: locked restore, formatting, 237 model/component/asset checks, Release publish, package validation, uploader tests and release guards. Browser checks cover missing/failed portraits, side changes and player-card bounds at 1280×720, 1920×1080, 2560×1440 and 2560×1080 with the normal and fallback fonts.

Import **0.4.11** and reopen outputs. Browser verification does not replace native CS2/OBS acceptance.
