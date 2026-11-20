# Recording broadcasts · 0.4.52

The six recording scenes are passive broadcast graphics. They share the existing
16:9 HUD canvas, blue palette, CT/T accents, condensed card typography, local
weapon silhouettes, branding and player imagery. There are no controls,
navigation links, debug tables, revision identifiers, raw IDs, expandable
sections or scroll areas in the output. Narrow outputs scale with the same
canvas as the live HUD. All subviews have an opaque background covering the entire output, including areas outside the fitted 16:9 content canvas. Scene safe areas are internal padding, so neither regular views nor lower-third layouts leave transparent borders.

| View | Output |
| --- | --- |
| `views/team-economy` | Two team headlines, a straight-line round graph, gaps and observed player coverage |
| `views/round-compare` | Matched player strips with two historical inventories and known-value deltas |
| `views/side-statistics` | CT/T player scoreboard with independent kill/headshot/damage coverage and a separate unassigned sample |
| `views/multikills` | One exclusive 2K/3K/4K/5+ total per player, with captured kill-round counts |
| `views/round-buys` | Mirrored team buy classes, observed equipment, survivors and received end inventories |
| `views/economy-evidence` | Received event roles, weapon silhouettes, cash changes, refund/carryover evidence and force-buy assessments |

The live and seven original presentation views remain available through the
existing view manager. Package identity remains `sou-basic-wasm`.

## Operator panels

Six dedicated panel sections configure the views; see [panel reference](panel.md).
All have a title override and **Synchronize recording** action. The operator can
select money/equipment, the received checkpoint, rounds, historical sample,
player focus, inventory pages and event pages where relevant. The main scene selector and the
existing host view manager continue to switch scenes.

Blank round fields follow the newest saved round; comparison A follows the
preceding saved round. `previous` selects the preceding round explicitly.
An explicit missing round or phase is not silently replaced with another sample.
The default checkpoint is Freeze end: the last received freeze sample is eligible only after a saved round-start/end checkpoint proves completion. Adaptive columns and rows show all observed players simultaneously, without player pages or a fixed five-player assumption. Event and inventory pages remain available for those larger lists. Each output shows the visible/observed scope.

Panel-only presentation changes use already loaded data. Only the active view's synchronization action replaces the shared subscription; inactive view actions have no effect.

## SDK and lifecycle

The HUD uses the published `0.3.0` NuGet packages for desktop 0.3.42
or newer. Project NuGet.Config uses nuget.org and a separate
`artifacts/nuget-packages` cache, with public package hashes in the lock
files. Build.ps1 invokes the public tool from `artifacts/published-sdk-tools`
instead of a cached private executable with the same version number. No local
sOU source projects or private SDK ZIPs are required. Package format 2 declares
the actual SDK version and the capabilities used by the existing API 3 HUD.

See the published [compatibility and package rules](https://www.getsou.app/docs/hud-paketformat?ui_lang=de).

One WatchRecordingAsync subscription serves the six related views for the
selected match/map. Omitted selection requests all three datasets, so switching
between these views or changing operator presentation settings does not create
a new subscription. Leaving the view family cancels it. Match/map changes clear
old data, cancel old work and fence delayed callbacks by subscription generation.
The manual Synchronize action replaces the subscription with a new baseline.
There are no recording data HTTP calls, page cursors, polling or HUD retry timers.

The SDK assembles bounded chunks and delivers complete states. The HUD replaces
those states directly, including removed/replaced rounds, late earlier-round
events and lower totals. Map summaries are never accumulated between updates.
Dataset revisions are not treated as interchangeable. During recording reconnect,
retained data is marked STALE / RECONNECTING. Connected transport alone does not
clear stale status: a fully synchronized SDK state does. Transient loss of the
live session graph is not interpreted as a user-selected match/map change.

Not recorded, unavailable, loading, reconnecting, connection failure and unknown
metrics remain distinct. The new SDK has no Unsupported recording status; an
older desktop lacking the channel is a connection failure, not a fabricated
capability result.

## Local build and preview

```powershell
pwsh ./scripts/Build.ps1 -OutputDirectory artifacts/published-compatible-2026-11-16
$sdkTool = "artifacts/published-sdk-tools/.store/sou.hud.tool/0.3.0/sou.hud.tool/0.3.0/tools/net10.0/any/SOU.Hud.Tool.dll"
dotnet $sdkTool fixtures validate tests/fixtures/recording-showcase.json
dotnet $sdkTool dev artifacts/published-compatible-2026-11-16/package --port 0 --fixture tests/fixtures/recording-showcase.json
node tests/browser/recording.mjs artifacts/published-compatible-2026-11-16/package
node tests/browser/v3.mjs artifacts/published-compatible-2026-11-16/package
```

The fixture includes the SDK's complete recordingSnapshot: six rounds, halftime,
known zeros, unknown inventory and absent round ends. It is explicitly marked
EXAMPLE DATA. The synthetic browser suite uses the SDK dev host and its guarded
recording command for actual push updates. It verifies late earlier-round events,
full replacement with lower values, missing checkpoints, no summary accumulation,
reconnect/baseline recovery, cancellation, shared view lifetime and context changes.
It also checks 1920/390 output widths, long encoded names, all 10/32 players,
historical team names/logos and the existing player-image/agents-only setting.

The simulator accepts larger baselines from a fixture file but rejects oversized
recording command bodies with HTTP 413. Push tests use compact complete synthetic
maps (one round for the 32-player density check); this is not a recording data GET
or a backend workaround. The SDK owns real wire chunking.

Known evidence limits remain: no event-completeness proof, no purchase prices or
intent, active-weapon identity is not continuous/full inventory ownership, and
starting equipment cannot be separated from physical pickups by this DTO. Native
adapter late-load correlation now has a scoped handshake in this SDK; only
post-handshake events can bind, and start/gap limits remain. Late recorded events
are displayed when pushed; the HUD does not claim to recover events never captured.
Optional cashFlow values show sampled cumulative movements separately from the
first-to-last net cash figures. Refund and force-buy evidence stays SDK-supplied.

The documented checks are synthetic SDK/UI tests,
not a real CS2, OBS, desktop-permission or native-overlay acceptance test.

## Viewer copy and freeze completion

Output and all newly added panel labels are English, independently of browser
language. There is no language selector. Broadcast copy is kept to metrics,
rounds, phases, relevant unknown values and evidence levels. Technical data-source
caveats live in panel labels and this guide. CT SIDE / T SIDE identifies side
history when stable team IDs are unavailable; it does not pretend to follow a
team across halftime. Player/map/team names remain supplied text.

The SDK explicitly defines FreezeLast as the most recently received freeze
state, which can change while freeze is still in progress. The HUD therefore
uses it as Freeze end only when the same recorded round contains LiveStart or
RoundEnd received at or after that freeze sample. A live state from another stream
or a bare round status is not sufficient. Automatic graph selection stays on the
latest eligible phase; an explicitly selected unfinished phase remains unknown.
This fixes HUD interpretation, not SDK recording. Even a finalized last-received
freeze sample is not a guaranteed exact phase-boundary tick.

## All-player lists and native event meaning

Player lists have no paging or fixed player cap: columns, rows and density follow
the supplied player count. All ten players in a normal lineup are visible at once;
the browser suite also verifies 32 historical players. Multikills group by Steam
ID and sum the SDK's exclusive side/unassigned samples once into one total row.
The multikill view has no CT/T split or historical-side selector. CT/T statistics
remain side-specific in their separate view.

Recorded source names use the same single-pass entity decoding as live names.
`Wank&apos;ePEW` displays as `Wank'ePEW`. Decoded text remains ordinary Razor text:
markup-like names cannot create HTML, images or scripts.

The public economy event DTO has kind, actor, item and timestamps but no reason
that distinguishes a spawn loadout grant from a physical ground pickup. The HUD
therefore labels this event **Item received**, with **Received by** as the actor
role. It does not exclude all knives or default pistols, which could also be
received later. A reliable distinction requires upstream recording/contract
classification. Routine Confirmed event-occurrence acknowledgements are removed
from event rows; evidence levels remain on carryover, refunds and force-buy
assessments where they qualify an actual interpretation.

## Team and player imagery

Player lists share the live HUD profile-image setting and Agents only option. Missing or failed profile images use the bundled decorative agents. Recorded side and a matching live Steam ID provide the side/observer digit; historical players without a live digit use a stable decorative selection. Unknown sides are never assigned a CT/T portrait by guesswork. Portraits are decoration, not evidence of the actual in-game character.

Economy Review resolves team names and logos from the selected recorded round history in the shared push state. It never uses current side recognition for a historical round. Missing or conflicting recorded IDs keep the CT/T side label. Round Loadouts and Team Economy already use their recorded team IDs. CT/T statistics retain their side labels because those identify the statistic sample.

## Team identity colors

Team names, graph series and player/team identity accents use the confirmed current CT/T assignment of the stable team ID, including when an older round is selected. Team A/B order never determines side colors. A side swap recolors the same series without swapping its historical values. Unknown or conflicting current recognition uses neutral colors. Explicit historical CT/T sample labels retain the recorded side colors; multikills remain one combined total per player.
