# sOU Broadcast HUD · Blazor WebAssembly

A public C#/Blazor WebAssembly reference project for a complete CS2 broadcast: live HUD, prematch, real lineups, map veto, break, results, statistics and player spotlight. Modular components consume the published **SOU.Hud.Sdk** and **SOU.Live.Contracts** NuGet packages `0.2.0-preview.4`; no private application source is required.

The equivalent [React implementation](https://github.com/getsOU/sOU-HUD-React) uses the same public contracts and panel. Both projects are MIT licensed and keep all scripts, fonts and assets local.

![Actual Release HUD with synthetic example data over a checkerboard](docs/preview.png)

The screenshot uses an isolated local host and synthetic data. Its checkerboard sits behind the embedded output to demonstrate transparency; it is not part of the HUD.

## Install and use

1. Download `sou-basic-wasm-0.2.1.souhud` and its SHA-256 file from [Releases](https://github.com/getsOU/sOU-HUD-WASM/releases).
2. In the [sOU desktop app](https://www.getsou.app/get-sou), choose **HUDs → Paket importieren**, select the package and install it.
3. Select the HUD and **Ansicht**, then copy the address for an OBS Browser Source or open the CS2 output. Use **1920 × 1080** and a transparent browser background. Use sOU's actual address rather than guessing the local port.
4. Open its **Panel** and save/apply any captions, scene choices, references or images. All Hide checkboxes are unchecked by default: components stay enabled unless explicitly hidden.
5. Supply CS2/HLAE data through sOU's normal setup. Optional data can remain absent; the HUD does not create missing players, game events or statistics.

The stable package ID remains `sou-basic-wasm`; the current version is **0.2.1**, protocol v2. Previous releases and active outputs are unchanged. Select the new version and reload/reopen outputs after installing. Caption/Hide fields remain compatible; the seven additional output routes were introduced in 0.2.0. Version 0.2.1 restores local image error fallbacks, including the radar's relative-position fallback when an overview image cannot load.

[Eight output views](docs/views.md) · [All panel fields](docs/panel.md) · [Feature/source matrix and limits](docs/features.md) · [Public author guide](https://www.getsou.app/docs/hud-entwicklung)

## What's implemented

- Current-side scoreboard and team logos, map/round/phase, actual round/bomb seconds, best-of and series score.
- Rosters with observer digits, optional verified portraits, HP/armor, helmet/kit/bomb, inventory/ammo/utility, economy, K/D/A and actual round damage/kills and flash/smoke/burn status.
- Actual observed player, detailed inventory and recorded ADR/HS with honest whole/partial coverage.
- Per-side complete-or-unknown economy totals, alive/kit/utility information, timeouts/loss streak, actual round history and explicit round-win banners.
- Real HLAE killfeed, keyed by connection/epoch/event ID, with all supplied modifier flags; no kills inferred from health changes.
- Own MIT radar renderer with fresh HLAE → GSI fallback, directions/deaths/observer digits/bomb/grenades. Default is an honestly labeled relative world-position plot; an optional local overview needs an exact map name and valid explicit bounds.
- Managed match/competition/season/section branding, team logos, explicit series maps and veto decisions, stored results and conditionally verified series winner.
- Real prematch rosters and local profile pictures from optional, strictly match-bound local browser metadata; absent metadata does not break live output.
- Full statistics, explicit player/team/match spotlight references, broadcast/sponsor images, ticker, manual replay/status text and deduplicated lower-third actions.

Missing values remain `—`. Team A is never assumed CT. Unknown health is not treated as death. The observed card never substitutes the local/menu player. No invented MVP, clutch, overtime, score, map geometry, player photo, win probability or source event is supplied. The [matrix](docs/features.md) identifies each real source and limitation.

## Build, test and package

Install **.NET SDK 10.0.402** and **PowerShell 7**. The application targets .NET/ASP.NET Core **10.0.13**. `NuGet.Config` permits only nuget.org; SDK/package/tool versions and dependency hashes are committed in `global.json`, lock files and the local tool manifest. No private feed or credentials are required.

```powershell
pwsh ./scripts/Build.ps1
```

The script performs locked restore, restores the public `SOU.Hud.Tool`, verifies formatting, runs focused model/lifecycle/metadata tests and actual Razor image-error event checks, publishes Release WASM, packs and validates the HUD and writes a checksum:

```text
artifacts/sou-basic-wasm-0.2.1.souhud
artifacts/sou-basic-wasm-0.2.1.souhud.sha256
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

Tests read the actual manifest/panel schema and exercise initial host false values, caption-only save, explicit Hide toggles, missing data, side mapping, scene routes, source totals, metadata matching/size/cancellation/session races, radar freshness/calibration/deaths/resets, killfeed identity/lifetime, equal-revision reference refreshes, action deduplication and subscription disposal. Release builds omit debug symbols and all preview fixtures. Optional native WASM AOT/`wasm-tools` optimization is not required.

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
src/BasicHud/Development/ Debug-only synthetic feeds
src/BasicHud/wwwroot/     local CSS, installed entry point, common panel schema
                          (cast-media/preview.svg is Debug-only)
tests/BasicHud.Tests/     model/lifecycle/metadata checks and actual Razor event dispatch tests
scripts/Build.ps1         locked restore → format → test → publish → pack → validate → hash
```

One public SDK live stream and one panel stream serve the chosen scene. Both are canceled/awaited on disposal, as are the bounded metadata poller and UI clock. The clock refreshes presentation freshness/planned start, not speculative GSI countdowns. A long-lived injected `HttpClient` supports browser sessions. Media resolve through the SDK's local path guard. No private types, external UI libraries or GPL HUD assets are copied.

## Compatibility and security

HUD code executes in the trusted local sOU browser origin; package validation is not runtime isolation. Install packages only from authors you trust. This HUD embeds no cloud/diagnostic token, API key, external camera URL, tracker or account credentials. Optional metadata polls only the local session-protected options endpoint, with bounded size/time and source-context matching; failure gracefully removes optional metadata.

All page backgrounds stay transparent. Version 0.1.1 fixed the earlier root dark-color-scheme issue, and 0.1.2 corrected initial visibility defaults. Both fixes remain in 0.2.1. [Panel documentation](docs/panel.md) explains overview geometry, manual production controls, reference limits and the host's false checkbox defaults.

Builds, synthetic tests and browser checks do not certify native CS2/OBS capture or long-session performance. No source contract guarantees complete live data in every viewing mode. Test the actual target capture setup before a broadcast.

## License

MIT for this repository's own code, components, fixtures and original synthetic art. See [LICENSE](LICENSE), [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) and `LICENSES/`. Dependencies retain their licenses. No rights to sOU trademarks or third-party game artwork are implied.

## Publishing a new version

Update the manifest and reviewed documentation, then run build/tests and the existing release guards. Commit the source to `main`, wait for its successful workflow, then create a matching `vVERSION` tag. Its workflow repeats validation and publishes the built `.souhud` and SHA-256 as a GitHub Release. Existing releases are never overwritten.

Build jobs have read-only repository access. Only the release job can create release assets, using the automatic short-lived GitHub Actions token. No personal access token or NuGet publishing credential is required. Pull requests and ordinary branch builds keep CI artifacts without publishing a release.
