# Feature and source matrix

The HUD consumes public SDK `0.2.0-preview.4` contracts. It renders supplied state; it does not calculate a second game state or collect CS2 payloads itself. Missing numeric values are `—`. Unknown enum values do not become an assumed known state.

| Feature | Actual source | Missing-data behavior |
| --- | --- | --- |
| Scoreboard | `Game.Ct/T`, map, round, phases, countdown; reliable `ManagedMatch.Ct/T.TeamId` | Side labels fall back to source names or CT/T. Team A is never assumed CT. |
| Team logos and series | `ManagedMatch.TeamA/B.Logo`, `BestOf`, `SeriesWins` | Only permitted local images; neutral monogram on absent/failed images. |
| Rosters | `Game.Players`, `ObserverSlot`, health/armor, helmet/kit, inventory, ammo, money/equipment, K/D/A, round kills/damage, flash/smoke/burn | Every supplied side player remains a row even with unknown stats. Unknown health is not death. |
| Observed player | `Game.ObservedPlayer`, inventory and recorded statistics | No substitution with `CurrentPlayer` or an unrelated menu/local player. |
| Alive and economy | Explicitly available allplayers, per-side player health, money, equipment, kit, grenade inventory entries; side timeouts/loss streak | A missing summand, unavailable roster or globally unknown side prevents complete-looking totals. Unknown health is explicitly counted. Utility counts inventory slots, not an invented grenade quantity. |
| Round history and win banner | `RoundWins`, explicit `RoundWinner` and `RoundPhase.Over` | No fabricated rounds, MVP, clutch or overtime message. Source winner is a historical side, not a persistent team ID. |
| Bomb / defuse | `Bomb` or available `RoundBomb`, actual `SecondsRemaining`; `Countdown` | Source seconds only, rounded upward to `m:ss`. No assumed plant/defuse duration, speculative ticking or fabricated progress. |
| Killfeed | Actual `Game.KillFeed` HLAE events and all six supplied modifier flags | No HP-difference kills. Identity is connection + epoch + event ID. Last six events from the current timeline, visible up to eight presentation seconds without snapshot replay. |
| Radar | Fresh matching HLAE frame (750ms), otherwise GSI positions/forward; source bomb/grenades and individual inferno flame points | Relative world-position plot by default, explicitly labeled **no map**. Failed overview images recompute the relative projection. See [radar setup](panel.md#radar). No GPL map assets or guessed geometry. |
| Lineups / portraits | Optional local metadata, exact active-match team ID binding, real roster Steam IDs; matching live players where available | No invented five-player roster or external camera embedding. Local monogram when no portrait is available. |
| Competition | Managed competition name/logo, season, section path and sponsors | Missing competition remains absent. |
| Map veto | Explicit `Vetos` sorted by `Number`, then `PlayNumber`; `TeamId` | Picks/bans with missing assignments stay unassigned. A decider legitimately has no acting team. Maps order does not imply a pick. |
| Stored results | `Maps.Status/ScoreA/ScoreB/WinnerTeamId` | Incomplete scores stay incomplete. A series winner requires an odd supported best-of, enough explicit completed-map wins and consistent `SeriesWins`. |
| Player statistics | Match K/D/A, MVP count, score, round stats, economy, `RecordedStats` ADR/HS and coverage | Partial coverage is labeled; it never becomes whole-match ADR. No automatically declared match MVP. |
| Spotlight | Public player/team/match panel references, matching real source player and local images | Missing references are identified; an unrelated live player is never substituted for the selected person. Without a selection, actual ObservedPlayer may be used. |
| Production | Operator titles, subtitle/ticker, manual replay/status label, broadcast/sponsor images, deduplicated lower-third action | These are explicitly operator controlled; no automatic replay or camera action is inferred. |

## Optional metadata and media

The existing browser-session-protected `GET /hud-browser/options` supplements snapshots with a narrow own DTO: `players[steamId]` name/avatar/camera and `match.left/right` ID/name/shortName/logo/Steam IDs. Camera text is never retained or embedded. Names, team identities and scores remain governed by the current managed match and snapshot.

The poller uses the package origin, same-origin browser credentials/mode and rejects redirects. It polls every two seconds with a three-second request deadline and a 1MiB body limit. Parsing uses generated JSON metadata for trimming. Both team IDs must match the active managed match; source session/match/team changes clear old metadata and invalidate in-flight results. Optional request/JSON/size failures clear metadata without stopping live or panel subscriptions.

Only valid `/cast-media/` images resolved by the public SDK are embedded. No remote cameras, arbitrary image URLs, cloud tokens, CDN fonts, tracking or personal credentials are used. Image failures produce a neutral monogram.

## Lifecycle and presentation limits

One SDK live subscription and one SDK panel subscription serve every scene. Equal panel revisions can refresh public references; older state revisions are ignored. A panel reconnect can restart numbering. Lower-third actions require the expected section/name/value and an unseen action ID; the recent-ID set is bounded. A new source session clears transient production and live presentation state. Radar death positions reset for a new session/map/round/freezetime/timeline; a restarted or backwards HLAE timeline cannot silently supply old movement.

There is no public synchronized demo playback clock. The HUD therefore does not extrapolate GSI countdowns. The planned-start display uses the explicit managed start timestamp and the local UTC clock; it does not assert that the match actually started. An eight-second killfeed lifetime is a presentation duration, not a reconstructed game-time event stream.

Successful synthetic/browser tests remain separate from native CS2/OBS capture, long-session performance and complete real-world source coverage.
