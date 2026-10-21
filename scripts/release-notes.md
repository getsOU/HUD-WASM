# 0.4.15

This release removes a visible card seam and redundant condition indicators.

- Fix the dark vertical line next to T-side portraits at fractional pixel positions. Right-align the HP fill directly instead of mirroring its background layer, preserving the intended health direction.
- Remove the extra smoke, flash and fire status symbols from roster cards and the observed player's equipment. The animated avatar effects already communicate these conditions.

Validation: locked restore, formatting, 262 model/component/asset checks, Release publish, package validation, 13 uploader tests and release guards. Browser pixel checks verify clean CT/T card boundaries at 1600, 1920 and 2560 pixel widths, with correct HP fill direction at 0, 50 and 100 HP.

Import **0.4.15** and reopen outputs. The package ID remains `sou-basic-wasm`.
