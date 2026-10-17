# sOU Broadcast HUD · Blazor WebAssembly

A public C#/Blazor WebAssembly reference project for a complete CS2 broadcast: live HUD, prematch, real lineups, map veto, break, results, statistics and player spotlight. Modular components consume the published **SOU.Hud.Sdk** and **SOU.Live.Contracts** NuGet packages `0.2.0-preview.4`; no private application source is required.

The equivalent [React implementation](https://github.com/getsOU/sOU-HUD-React) uses the same public contracts and panel. From version 0.4.0 the HUD is GPL-3.0-only and includes the original Boltobserv radar maps, metadata, marker shapes and utility artwork with their notices and source archive. The public sOU SDK remains MIT. Valve game artwork retains its separate copyright notices; no ownership or unrestricted artwork license is claimed.

![Packaged HUD with calibrated map radar, local icons and compact edge displays over a CS2 reference scene](docs/preview.png)

The screenshot shows the actual packaged renderer with synthetic HUD data in an isolated local host over an unmodified [official CS2 reference screenshot from Steam](https://store.steampowered.com/app/730/CounterStrike_2/). The Valve game image is only the test backdrop and is not included in the HUD. This is a browser preview, not a native CS2/OBS capture.

## Install and use

1. Download `sou-basic-wasm-0.4.2.souhud` and its SHA-256 file from [Releases](https://github.com/getsOU/sOU-HUD-WASM/releases).
2. In the [sOU desktop app](https://www.getsou.app/get-sou), choose **HUDs → Paket importieren**, select the package and install it.
3. Select the HUD and **Ansicht**, then copy the address for an OBS Browser Source or open the CS2 output. Use **1920 × 1080** and a transparent browser background. Use sOU's actual address rather than guessing the local port.
4. Open its **Panel** and save/apply any captions, scene choices, references or images. All Hide checkboxes are unchecked by default: components stay enabled unless explicitly hidden.
5. Supply CS2/HLAE data through sOU's normal setup. Optional data can remain absent; the HUD does not create missing players, game events or statistics.

The stable package ID remains `sou-basic-wasm`; the current version is **0.4.2**, protocol v2. Select the new version and reopen outputs after installing. Caption/Hide fields and all eight views remain compatible. Previous releases and already running outputs are unchanged.

[Changelog](CHANGELOG.md) · [Eight output views](docs/views.md) · [All panel fields](docs/panel.md) · [Feature/source matrix and limits](docs/features.md) · [Public author guide](https://www.getsou.app/docs/hud-entwicklung)

## What's implemented

- A single clear match header: real current-side team names/logos, large round scores, actual source timer and central round/state label. One explicit caption or competition name; no raw map/phase diagnostic strip. Series-win marks stay attached to their actual teams.
- Compact two-line live rosters with observer digits, small optional portraits, HP/armor, real weapon silhouettes, utility/equipment/status icons, money and K/D/A. The complete inventory, round damage/kills and recorded metrics remain in presentation/statistics views.
- Actual observed player with active/reloading weapon, firearm ammunition and utility icons. Dead players show no stale current weapons or inventory; an alive player without an active weapon has an empty weapon area. Holstered equipment is never substituted for an active weapon. A genuinely supplied unknown active weapon keeps its own fallback. No grenade ammo fractions are invented.
- Per-side complete-or-unknown economy in the roster footer, with full alive/kit/utility/timeouts/loss-streak detail in statistics; numbered recent round results appear only during freeze, pause, timeout or intermission; active rounds including bomb/defuse remain clear. The statistics view retains the complete supplied history.
- Real HLAE killfeed, keyed by connection/epoch/event ID, with all supplied modifier flags; no kills inferred from health changes.
- Original Boltobserv radar with the original transparent maps, coordinate metadata and object-specific Nuke/Vertigo split projection. Original-style direction drops, observer digits, observed ring, bomb carrier, flashed/dead markers and utility connect to actual public data. Buyzones appear only during source freeze/warmup. Fresh HLAE supplies the whole player frame; absent/expired HLAE falls back to GSI; optional custom calibration and separately attributed Valve overview fallbacks remain available. No permanent technical radar caption or empty replacement grid.
- Ordered compact map-series cells below the radar, explicit stored results, exact current-map highlighting and side-aligned series wins. Full maps/veto/results and competition details remain in their presentation views. An unrelated source map never claims a configured current series slot.
- Real prematch rosters and local profile pictures from optional, strictly match-bound local browser metadata; absent metadata does not break live output.
- Full statistics, explicit player/team/match spotlight references, broadcast/sponsor images, ticker, manual replay/status text and deduplicated lower-third actions.

Missing values remain `—`. Team A is never assumed CT. Unknown health is not treated as death. The observed card never substitutes the local/menu player. No invented MVP, clutch, overtime, score, map geometry, player photo, win probability or source event is supplied. The [matrix](docs/features.md) identifies each real source and limitation.

The 1920×1080 live composition reserves the central game area (x360–1560, y160–930). The score header is 960×72; the radar is 288×288; each bottom-corner roster is at most 312×318 including five 54px rows and its economy footer; the observer card is 520×82. The whole composition scales proportionally in smaller or wider windows. Missing live portraits/logos consume no placeholder box; presentation scenes retain their neutral fallbacks. Only an explicit lower-third production action crosses the reserved game area.

## Build, test and package

Install **.NET SDK 10.0.402** and **PowerShell 7**. The application targets .NET/ASP.NET Core **10.0.13**. `NuGet.Config` permits only nuget.org; SDK/package/tool versions and dependency hashes are committed in `global.json`, lock files and the local tool manifest. No private feed or credentials are required.

```powershell
pwsh ./scripts/Build.ps1
```

The script performs locked restore, restores the public `SOU.Hud.Tool`, verifies formatting, runs focused model/lifecycle/metadata tests and actual Razor image-error event checks, publishes Release WASM, packs and validates the HUD and writes a checksum:

```text
artifacts/sou-basic-wasm-0.4.2.souhud
artifacts/sou-basic-wasm-0.4.2.souhud.sha256
```

Existing output directories/packages are never overwritten. Repeat in a fresh directory:

```powershell
pwsh ./scripts/Build.ps1 -OutputDirectory artifacts/check-2
```

Run only focused checks:

```powershell
dotnet restore tests/BasicHud.Tests --configfile NuGet.Config --locked-mode
dotnet run --project tests/BasicHud.Tests -c Release --no-restore
```

Tests read the actual manifest/panel schema and exercise initial host false values, caption-only save, explicit Hide toggles, missing data, side mapping, scene routes, source totals, metadata matching/size/cancellation/session races, fixed Valve and original Bolt Dust2 projection, object-specific Nuke/Vertigo split boundaries, original asset hashes, header/history state rendering and bounded catalog loading, radar freshness/deaths/resets, actual Razor image-error dispatch, compact image omission, actual dead/stale-inventory and unarmed/holstered/unknown-weapon rendering, safe one-pass source-name decoding, grenade counts, killfeed identity/lifetime, equal-revision reference refreshes, action deduplication and subscription disposal. Release builds omit debug symbols and all preview fixtures. Optional native WASM AOT/`wasm-tools` optimization is not required.

## Isolated Debug preview

```powershell
dotnet restore src/BasicHud --configfile NuGet.Config --locked-mode
dotnet run --project src/BasicHud --urls http://127.0.0.1:5178
```

Open <http://127.0.0.1:5178/?preview=live>. Scene previews: `prematch`, `lineups`, `veto`, `break`, `postmatch`, `statistics`, `spotlight`. Additional cases: `waiting`, `stale`, `swapped`, `missing`, `bomb`, `overview`, `production` (lower-third action). Fixtures include source weapons/stats, real-shaped metadata, local original synthetic SVG art, radar/grenades/kills, maps/veto, competition/sponsors and panel references.

Preview activation requires **Debug compilation**, **loopback hostname** and an **explicit recognized query**. The **LOCAL PREVIEW** badge marks it. It is a read-only browser feed; it neither connects to nor injects fixtures into an actual cast/GSI host. Release excludes the preview classes and synthetic asset. Opening a normal development URL does not supply the installed sOU package identity/session. For actual subview routing/base/CSP testing, import the built Release package into an isolated sOU-compatible test host or the real desktop app; the development server does not replace that integration check.

## Code structure

```text
src/BasicHud.Core/        snapshot/panel presentation, scene selection, metadata binding,
                          complete totals, radar projection and event identity
src/BasicHud/Components/  reusable scoreboard/player/inventory/radar/series/branding layers
src/BasicHud/Views/       seven presentation scenes; main live composition is in App.razor
src/BasicHud/MetadataPoller.cs narrow source-generated optional local metadata reader
src/BasicHud/RadarCatalogLoader.cs bounded generated local Bolt/Valve catalog readers
src/BasicHud/Development/ Debug-only synthetic feeds
src/BasicHud/wwwroot/     local CSS, installed entry point, common panel schema
                          assets/boltobserv contains original upstream radar content;
                          assets/radar and assets/weapons retain Valve fallback artwork
                          (cast-media/preview.svg is Debug-only)
tests/BasicHud.Tests/     model/lifecycle/metadata checks and actual Razor event dispatch tests
scripts/Build.ps1         locked restore → format → test → publish → pack → validate → hash
```

One public SDK live stream and one panel stream serve the chosen scene. Both are canceled/awaited on disposal, as are the bounded metadata poller, one-shot catalog loader and UI clock. The clock refreshes presentation freshness/planned start, not speculative GSI countdowns. A long-lived injected `HttpClient` supports browser sessions. Cast media resolve through the SDK's local path guard; bundled artwork stays within the installed package. No private application types or external UI runtime is required. The GPL Boltobserv radar port preserves source attribution and original upstream files in LICENSES/.

## Compatibility and security

HUD code executes in the trusted local sOU browser origin; package validation is not runtime isolation. Install packages only from authors you trust. This HUD embeds no cloud/diagnostic token, API key, external camera URL, tracker or account credentials. Optional metadata polls only the local session-protected options endpoint, with bounded size/time and source-context matching; failure gracefully removes optional metadata.

All page backgrounds stay transparent. Earlier transparency, initial visibility and image-error fixes remain included. [Panel documentation](docs/panel.md) explains automatic radar maps and custom geometry, manual production controls, reference limits and the host's false checkbox defaults.

Builds, synthetic tests and browser checks do not certify native CS2/OBS capture or long-session performance. No source contract guarantees complete live data in every viewing mode. Test the actual target capture setup before a broadcast.

## License

GPL-3.0-only from 0.4.0, including the adapted Boltobserv radar. Previous releases through 0.3.0 remain under their original MIT terms. The public SDK packages remain MIT. See [LICENSING.md](LICENSING.md), [LICENSE](LICENSE), [Boltobserv provenance](LICENSES/BOLTOBSERV-NOTICE.md) and [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md). Original map-specific MIT notices, original Boltobserv source archive and separate Valve copyright notices ship with the package. Dependencies and trademarks retain their respective terms.

## Publishing a new version

Update the manifest and reviewed documentation, then run build/tests and the existing release guards. Commit the source to `main`, wait for its successful workflow, then create a matching `vVERSION` tag. Its workflow repeats validation and publishes the built `.souhud` and SHA-256 as a GitHub Release. Existing releases are never overwritten.

Build jobs have read-only repository access. Only the release job can create release assets, using the automatic short-lived GitHub Actions token. No personal access token or NuGet publishing credential is required. Pull requests and ordinary branch builds keep CI artifacts without publishing a release.

## Radar movement and pick ownership (0.4.2)

Radar motion follows NYXIA: short 50 ms linear transitions, stable player identity and immediate resets across source/session/map/round/floor changes. A fresh HLAE frame supplies all player positions; missing pawns are omitted rather than mixed with older GSI. At 750 ms the whole frame expires and existing GSI positions may be used. The HUD clock advances every 50 ms; WASM also updates it when receiving a snapshot. No positions are extrapolated.

Live and full map series display the explicit pick team from the corresponding veto identity, or DECIDER. Pick ownership stays with TeamId through CT/T changes; absent, foreign or conflicting assignments remain unresolved. See CHANGELOG.md for earlier corrections.
