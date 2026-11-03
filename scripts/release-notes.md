# 0.4.20

Add round MVP presentation and configurable live branding above the killfeed.

- Attach the MVP card directly below the matchbar at the same width, using the player's side color, portrait and completed-round kills/damage. Detect observed MVP-counter increases across round-end timing differences. Hide each award after five seconds, including during pauses and after match end; repeated data cannot extend or revive it. Fix clipped player names.
- Replace the center scoreboard wordmark through **Branding → Broadcast logo**, with the white sOU wordmark as fallback. Remove the outside matchbar/MVP borders and the separate CT/T win banner.
- Move the competition logo and combined competition/panel sponsors above the killfeed. Keep the competition logo fixed and rotate sponsors individually, with a ten-second default and panel-configurable duration. The sponsor toggle preserves the competition logo.
- Add independent 0–100% logo size controls. Height and width scale together, preserving image proportions and placement; both use the same full-size width.
- Remove the observer sponsor footer and **RELOAD** label. Retain the actual weapon and ammunition during reloading.

Requires **sOU desktop 0.3.34 or newer**. Import **0.4.20**, select the new version and reopen outputs. The package ID remains `sou-basic-wasm`.

Validation: locked restore, formatting, 325 model/component/asset checks, Release publish, SDK package validation, Chromium rendering/rotation/timeout/size checks, 14 uploader tests and release guards. Browser tests use synthetic data; they do not claim native CS2/OBS coverage.
