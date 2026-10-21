# 0.4.13

This release adds animated, alpha-masked condition effects to player portraits and agent placeholders.

- Add moving smoke, soft white flash glare and rising fire-color overlays to live rosters, observed-player cards, lineups and spotlight portraits. Multiple conditions can appear together.
- Use the actually displayed image's alpha channel as the effect mask. Transparent margins and holes stay untouched; mask sizing and position match the image's crop. Opaque photos receive the effect across their opaque rectangle.
- Keep the mask synchronized with player photos, selected spotlight images and agent fallbacks, including the **Nur Agenten anzeigen** panel option. Failed final images leave no orphan effect layer.
- Render effects only for positive condition flags in fresh live data. Death, cleared conditions and stale/unavailable feeds remove them. Reduced-motion preferences retain static cues without animation.
- Defer HE-hit animation until a reliable cause-specific event exists; health loss is not treated as evidence of a grenade hit.

All eight views, existing panel settings, source handling, radar, inventory selection and chronological killfeed remain available. The public SDK stays at 0.2.0-preview.4; the package ID remains `sou-basic-wasm`. Artwork retains the separately documented upstream rights.

Validation: locked restore, formatting, 261 model/component/asset checks, Release publish, package validation, uploader tests and release guards. Browser checks verify animation changes over time, live-state selection, fallback/crop alignment and reduced-motion behavior. Pixel comparisons confirm no effect pixels in transparent margins or holes.

Import **0.4.13** and reopen outputs. Browser verification does not replace native CS2/OBS acceptance.
