# 0.4.31

- Add six passive recording views: Team Economy, Loadout Comparison, CT/T Statistics, Multikills, Round Loadouts and Economy Review, configured through their existing host panels.
- Use the published SOU 0.3.0-preview.9 packages and SDK-owned recording push with complete state replacement, reconnect synchronization and cancellation on match/map changes.
- Declare package format 2, SDK version and the capabilities required by the existing API 3 HUD for sOU Desktop 0.3.42 or newer.
- Unify all presentation frames, headers, colors and typography; scale the live HUD, text and icons with output size, embedded previews and lower-third space.
- Show all observed players without player pagination, combine exclusive multikill totals per player, preserve historical team names/logos and reuse profile images with agent fallbacks.
- Keep unfinished freeze values out of freeze-end graphs, decode player-name entities safely and distinguish unknown values, real zero, missing inventory and SDK evidence levels.
