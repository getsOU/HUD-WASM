# 0.4.12

This release improves observed-player visibility and utility sizing, adds an agents-only panel option and supplies matching in-game colors.

- Remove borders from inactive roster cards. Only the observed player has a white outline and a small drop shadow, on either side.
- Align utility on one horizontal baseline and match visible grenade heights to Flashbang. Fix the optical-layout generator's floating-point bounds so SVG whitespace no longer makes symbols appear undersized.
- Add **Display → Nur Agenten anzeigen** (`display.agentsOnly`). When enabled, all player photos, including spotlight references, use the corresponding CT/T observer-slot agent. When disabled, supplied photos take priority and agents remain the missing/failed-photo fallback. Names, statistics, logos and sponsors are unaffected.
- Declare CT blue `#B6DEF5` and T gold `#EEE1A0` in the package's `outlineColors` palette. Supporting sOU/HLAE hosts use it for X-Ray/outlines, utility trails and derived smoke colors when this HUD is opened through **Über CS2 anzeigen**. New effects pick up the colors; existing effects may retain their previous appearance.

All eight views, existing panel settings, source handling, radar, inventory selection and chronological killfeed remain available. The public SDK stays at 0.2.0-preview.4; the package ID remains `sou-basic-wasm`. Artwork retains the separately documented upstream rights.

Validation: locked restore, formatting, 246 model/component/asset checks, Release publish, package validation, uploader tests and release guards. Browser checks cover observed/inactive card styling on both sides and visible utility heights/baselines. In-game HLAE color application requires the supporting sOU desktop host.

Import **0.4.12** and reopen outputs. Browser verification does not replace native CS2/OBS acceptance.
