# 0.4.14

This release replaces portrait color washes with procedural, interruptible avatar animations powered by locally bundled Motion 14.5.0.

- Smoke uses dense, rounded clouds. Every cloud starts at the same height below the avatar and arrives at its own destination at the same time; different travel distances create different speeds. The continuous buildup takes 3.45 seconds, followed by ongoing cloud movement.
- Fire uses broad, pointed flames with changing contours and rising sparks. After its initial buildup, it expands slowly over three seconds. Smoke and fire fly into the portrait as moving elements, without a rolling reveal boundary.
- Flash expands as a circular surface from the bottom-left corner to cover the avatar. It fades in place when cleared, retaining its reached radius.
- All transitions can be interrupted and resumed from their current state. Effects retain the actual portrait's alpha mask, including transparent holes, crop alignment and agent fallback changes. Death, stale data and player/round/image changes reset effects; hidden effects stop their frame loops.
- Honor reduced-motion preferences, keep all runtime assets local and include Motion's MIT license. HE-hit effects remain deferred until reliable cause-specific events exist.

Validation: locked restore, formatting, 262 model/component/asset checks, Release publish, package validation, 13 uploader tests and release guards. Browser checks cover ongoing geometry, smoke timing, interrupted buildup/exit, flash expansion and uniform fading, fallback alpha masks and reduced-motion behavior.

Import **0.4.14** and reopen outputs. The package ID remains `sou-basic-wasm` and the public SDK remains 0.2.0-preview.4.
