# 0.4.21

Fix inconsistent player names in the killfeed.

- Use the same sOU-provided profile names for the killer, victim and assists as in the player cards, matched by Steam ID. Apply them to both visible names and tooltips.
- Update existing killfeed entries when a participant's profile name changes. Portrait-only changes and unrelated movement do not refresh the feed.
- Retain event names when no matching profile is available. Reject profiles from unrelated match teams and preserve world kills.

Requires **sOU desktop 0.3.34 or newer**. Import **0.4.21**, select the new version and reopen outputs. The package ID remains `sou-basic-wasm`.

Validation: locked restore, formatting, 333 model/component/asset checks, Release publish, SDK package validation, Chromium rendering and profile-name update checks, 14 uploader tests and release guards. Browser tests use synthetic data; they do not claim native CS2/OBS coverage.
