# Feature and source matrix

The HUD consumes public SDK `0.2.0-preview.4` contracts. It renders supplied state; it does not calculate a second game state or collect CS2 payloads itself. Missing numeric values are `—`. Unknown enum values do not become an assumed known state.

| Feature | Actual source | Missing-data behavior |
| --- | --- | --- |
| Scoreboard | `Game.Ct/T`, map, round, phases, countdown; reliable `ManagedMatch.Ct/T.TeamId` | Side labels fall back to source names or CT/T. Team A is never assumed CT. |
| Team logos and series | `ManagedMatch.TeamA/B.Logo`, `BestOf`, `SeriesWins` | Only permitted local images. Missing/failed live logos are omitted; presentation images retain neutral monograms. Series maps share the compact header line. |
| Rosters | `Game.Players`, `ObserverSlot`, health/armor, helmet/kit, inventory, ammo, money/equipment, K/D/A, round kills/damage, flash/smoke/burn | Live shows the first five players per side in observer order as two-line rows; full verified lineups remain in the lineups view. Unknown health is not death. Detailed inventories and round metrics remain in lineups/statistics/spotlight. |
| Observed player | `Game.ObservedPlayer`, active/reloading weapon, actual firearm ammo and utility | No substitution with `CurrentPlayer` or an unrelated menu/local player. Grenades have utility icons/counts, never a fabricated ammo fraction. Full recorded detail remains in statistics/spotlight. |
| Alive and economy | Explicitly available allplayers, per-side player health, money, equipment, kit, grenade inventory entries; side timeouts/loss streak | A missing summand, unavailable roster or globally unknown side prevents complete-looking totals. Unknown health is explicitly counted. Utility counts inventory slots, not an invented grenade quantity. |
| Round history and win banner | `RoundWins`, explicit `RoundWinner` and `RoundPhase.Over` | No fabricated rounds, MVP, clutch or overtime message. Source winner is a historical side, not a persistent team ID. |
| Bomb / defuse | `Bomb` or available `RoundBomb`, actual `SecondsRemaining`; `Countdown` | Source seconds only, rounded upward to `m:ss`. No assumed plant/defuse duration, speculative ticking or fabricated progress. |
| Killfeed | Actual `Game.KillFeed` HLAE events and all six supplied modifier flags | No HP-difference kills. Identity is connection + epoch + event ID. Last six events from the current timeline, visible up to eight presentation seconds without snapshot replay. |
| Radar | Fresh matching HLAE frame (750ms), otherwise GSI positions/forward; source bomb/grenades and individual inferno flame points; bundled original map overview/calibration | Exact known-map lookup uses fixed world bounds, not player-dependent scaling. Observed height selects genuine available floor layers; another floor is dimmed and marked higher/lower. Unknown maps, failed images or unavailable catalog use a labeled relative fallback. Explicit valid local calibration has priority. See [radar setup](panel.md#radar). No GPL renderer/assets or guessed geometry. |
| Lineups / portraits | Optional local metadata, exact active-match team ID binding, real roster Steam IDs; matching live players where available | No invented five-player roster or external camera embedding. Missing/failed live portraits consume no box; presentation images retain a local monogram. |
| Competition | Managed competition name/logo, season, section path and sponsors | Missing competition remains absent. |
| Map veto | Explicit `Vetos` sorted by `Number`, then `PlayNumber`; `TeamId` | Picks/bans with missing assignments stay unassigned. A decider legitimately has no acting team. Maps order does not imply a pick. |
| Stored results | `Maps.Status/ScoreA/ScoreB/WinnerTeamId` | Incomplete scores stay incomplete. A series winner requires an odd supported best-of, enough explicit completed-map wins and consistent `SeriesWins`. |
| Player statistics | Match K/D/A, MVP count, score, round stats, economy, `RecordedStats` ADR/HS and coverage | Partial coverage is labeled; it never becomes whole-match ADR. No automatically declared match MVP. |
| Spotlight | Public player/team/match panel references, matching real source player and local images | Missing references are identified; an unrelated live player is never substituted for the selected person. Without a selection, actual ObservedPlayer may be used. |
| Production | Operator titles, subtitle/ticker, manual replay/status label, broadcast/sponsor images, deduplicated lower-third action | These are explicitly operator controlled; no automatic replay or camera action is inferred. |

## Optional metadata and media

The existing browser-session-protected `GET /hud-browser/options` supplements snapshots with a narrow own DTO: `players[steamId]` name/avatar/camera and `match.left/right` ID/name/shortName/logo/Steam IDs. Camera text is never retained or embedded. Names, team identities and scores remain governed by the current managed match and snapshot.

The poller uses the package origin, same-origin browser credentials/mode and rejects redirects. It polls every two seconds with a three-second request deadline and a 1MiB body limit. Parsing uses generated JSON metadata for trimming. Both team IDs must match the active managed match; source session/match/team changes clear old metadata and invalidate in-flight results. Optional request/JSON/size failures clear metadata without stopping live or panel subscriptions.

User-provided images must be valid `/cast-media/` paths resolved by the public SDK. Bundled game artwork is loaded from the installed package's local `assets/` directory. No remote cameras, arbitrary image URLs, cloud tokens, CDN fonts, tracking or personal credentials are used. Live portraits/team logos omit failed images completely; presentation images use neutral monograms. An unavailable weapon image gets a small unknown-equipment icon.

GSI/HLAE player and source side names are decoded once for entities, then rendered as ordinary escaped text. Managed library/profile and operator texts are left unchanged. The HUD never treats a source name as HTML.

Original Valve artwork is separate from MIT source code; provenance, exact source entries and hashes are included in `LICENSES/VALVE-ASSETS.md` and `LICENSES/valve-assets.json` and shipped with the package.

## Lifecycle and presentation limits

One SDK live subscription and one SDK panel subscription serve every scene. Equal panel revisions can refresh public references; older state revisions are ignored. A panel reconnect can restart numbering. Lower-third actions require the expected section/name/value and an unseen action ID; the recent-ID set is bounded. A new source session clears transient production and live presentation state. Radar death positions reset for a new session/map/round/freezetime/timeline; a restarted or backwards HLAE timeline cannot silently supply old movement.

There is no public synchronized demo playback clock. The HUD therefore does not extrapolate GSI countdowns. The planned-start display uses the explicit managed start timestamp and the local UTC clock; it does not assert that the match actually started. An eight-second killfeed lifetime is a presentation duration, not a reconstructed game-time event stream.

Successful synthetic/browser tests remain separate from native CS2/OBS capture, long-session performance and complete real-world source coverage.
