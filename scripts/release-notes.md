# 0.4.51

- Make all 13 subviews fully opaque and fill the complete output, including non-16:9 areas, while preserving proportional content scaling and the transparent live overlay.
- Color team identities by their current recognized CT/T side across match and recording views, keeping historical data joined by stable team IDs.
- Anchor player images at the top by default and add nine alignment choices to the Display panel, shared by portraits, effect masks and agent fallbacks.
- Improve observer-slot contrast with dark team-colored digits in both playercard layouts.
- Replace matchbar bomb timers with compact state icons, actual remaining-time-driven plant/defuse progress and distinct kit/bare-hand symbols; omit visible captions and afterplant circles.
- Accelerate the afterplant double pulse using SDK remaining time and an explicit 40-second visual reference; turn the bomb red in its final ten seconds.
- Replace pulsing throughout the final five seconds with a fixed 300 ms double-shake cycle (200 ms movement, 100 ms rest), increasing only its strength and continuing until the SDK reports the terminal state.
- Smooth color and symbol transitions within the remaining time budget, show terminal symbols immediately, and respect paused/stale data and reduced-motion preferences.
