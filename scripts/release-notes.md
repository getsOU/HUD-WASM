# 0.4.16

This release redesigns the observed-player card around the broadcast reference while retaining the HUD's own palette, typography and icons.

- Place a large player portrait above a compact information strip. Show the assigned team logo directly left of the player name and K/D, with ammunition, health and armor on the right. Use the observer slot when no team logo is available.
- Keep agent fallback, the agents-only option and alpha-masked portrait animations in the enlarged portrait.
- Attach configured sponsors below the card without duplicating them elsewhere in the live output. Respect the existing sponsor visibility option; retain standalone sponsors when the observer card is absent and in other scenes.

Validation: locked restore, formatting, 265 model/component/asset checks, Release publish, package validation, 13 uploader tests and release guards. Browser checks at 1600, 1920 and 2560 pixel widths verify portrait/strip/footer ordering, a single sponsor section, no name/ammo/health overlap and placement inside the output.

Import **0.4.16** and reopen outputs. The package ID remains `sou-basic-wasm`.
