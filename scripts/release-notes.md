# 0.4.22

- Unify status, utility and ammunition icons across player/observer cards, killfeed, round history and presentation views with 23 local Lucide/custom outline symbols. Add no runtime dependency; retain original weapon, knife and C4 silhouettes and dynamic radar drawing.
- Apply consistent team-colored card icons, white text and equipment exceptions, and green money amounts with a team-colored dollar sign. Color every round-result icon by its winning side, including the defuse kit and original C4.
- Improve the observer strip with a larger bounded white weapon and separate loaded-rounds/reserve icons on the existing lower row. Keep weapon and ammunition together, omit unavailable ammunition, and place armor directly left of numeric health.
- Correct Bahnschrift Condensed selection in Firefox and Chromium without changing the chosen font or bundling font files.
- Fix health-fill proportions outside the observer-slot strip, align slot digits with player names, and mirror the skull crop correctly on right-side dead cards. Keep horizontal dead cards compact, bottom-aligned and without the large skull.
- Animate the neutral-blue MVP card with a proportional gap below the matchbar. Show each award for up to eight seconds, ending early at the next freeze; preserve expiry during pauses, match end and repeated updates.
- Expand the Debug preview with all supported killfeed modifiers, all six utility types, winner-colored round results, and separate Kevlar-only and unarmored examples. Keep synthetic data out of Release builds.
- Preserve API v3 data bindings, managed player names and independent SDK subscriptions; position and ammunition updates continue to avoid unrelated card refreshes.
- Require **sOU desktop 0.3.34 or newer**. Import **0.4.22**, select the new version and reopen outputs. The package ID remains `sou-basic-wasm`.
