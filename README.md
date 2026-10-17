# sOU Broadcast HUD · Blazor WebAssembly

A public C#/Blazor WebAssembly reference project for a complete CS2 broadcast: live HUD, prematch, real lineups, map veto, break, results, statistics and player spotlight. Modular components consume the published **SOU.Hud.Sdk** and **SOU.Live.Contracts** NuGet packages `0.2.0-preview.4`; no private application source is required.

The equivalent [React implementation](https://github.com/getsOU/sOU-HUD-React) uses the same public contracts and panel. Both projects' own code is MIT licensed and keeps all scripts, fonts and assets local. Included original Valve game artwork has separate copyright notices; it is not relicensed under MIT.

![Packaged HUD with calibrated map radar, local icons and compact edge displays over a CS2 reference scene](docs/preview.png)

The screenshot shows the actual packaged renderer with synthetic HUD data in an isolated local host over an unmodified [official CS2 reference screenshot from Steam](https://store.steampowered.com/app/730/CounterStrike_2/). The Valve game image is only the test backdrop and is not included in the HUD. This is a browser preview, not a native CS2/OBS capture.

## Install and use

1. Download `sou-basic-wasm-0.3.0.souhud` and its SHA-256 file from [Releases](https://github.com/getsOU/sOU-HUD-WASM/releases).
2. In the [sOU desktop app](https://www.getsou.app/get-sou), choose **HUDs → Paket importieren**, select the package and install it.
3. Select the HUD and **Ansicht**, then copy the address for an OBS Browser Source or open the CS2 output. Use **1920 × 1080** and a transparent browser background. Use sOU's actual address rather than guessing the local port.
4. Open its **Panel** and save/apply any captions, scene choices, references or images. All Hide checkboxes are unchecked by default: components stay enabled unless explicitly hidden.
5. Supply CS2/HLAE data through sOU's normal setup. Optional data can remain absent; the HUD does not create missing players, game events or statistics.

The stable package ID remains `sou-basic-wasm`; the current version is **0.3.0**, protocol v2. Select the new version and reopen outputs after installing. Caption/Hide fields and all eight views remain compatible. Previous releases and already running outputs are unchanged.

[Eight output views](docs/views.md) · [All panel fields](docs/panel.md) · [Feature/source matrix and limits](docs/features.md) · [Public author guide](https://www.getsou.app/docs/hud-entwicklung)

## What's implemented

- Current-side scoreboard and team logos, map/round/phase, actual round/bomb seconds, best-of and series score.
- Compact two-line live rosters with observer digits, small optional portraits, HP/armor, real weapon silhouettes, utility/equipment/status icons, money and K/D/A. The complete inventory, round damage/kills and recorded metrics remain in presentation/statistics views.
- Actual observed player with active weapon, firearm ammunition and utility icons. No grenade ammo fractions are invented.
- Per-side complete-or-unknown economy in the roster footer, with full alive/kit/utility/timeouts/loss-streak detail in statistics; actual round history and explicit round-win banners remain in the header area.
- Real HLAE killfeed, keyed by connection/epoch/event ID, with all supplied modifier flags; no kills inferred from health changes.
- Own MIT radar renderer with fresh HLAE → GSI fallback, directions/deaths/observer digits/bomb/grenades/flames. Known maps automatically use a bundled original overview with fixed real world calibration. Nuke, Train and Vertigo select floors from actual observed height when provided. An explicitly calibrated local overview remains an override; unavailable/unknown maps are honestly labeled.
- Managed match/competition/season/section branding, team logos, explicit series maps and veto decisions, stored results and conditionally verified series winner.
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
artifacts/sou-basic-wasm-0.3.0.souhud
artifacts/sou-basic-wasm-0.3.0.souhud.sha256
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

Tests read the actual manifest/panel schema and exercise initial host false values, caption-only save, explicit Hide toggles, missing data, side mapping, scene routes, source totals, metadata matching/size/cancellation/session races, fixed Dust2 projection/floor boundaries/catalog loading, radar freshness/deaths/resets, actual Razor image-error dispatch, compact image omission, safe one-pass source-name decoding, grenade counts, killfeed identity/lifetime, equal-revision reference refreshes, action deduplication and subscription disposal. Release builds omit debug symbols and all preview fixtures. Optional native WASM AOT/`wasm-tools` optimization is not required.

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
src/BasicHud/RadarCatalogLoader.cs one-shot generated local overview-catalog reader
src/BasicHud/Development/ Debug-only synthetic feeds
src/BasicHud/wwwroot/     local CSS, installed entry point, common panel schema
                          assets/radar and assets/weapons contain original Valve artwork
                          (cast-media/preview.svg is Debug-only)
tests/BasicHud.Tests/     model/lifecycle/metadata checks and actual Razor event dispatch tests
scripts/Build.ps1         locked restore → format → test → publish → pack → validate → hash
```

One public SDK live stream and one panel stream serve the chosen scene. Both are canceled/awaited on disposal, as are the bounded metadata poller, one-shot catalog loader and UI clock. The clock refreshes presentation freshness/planned start, not speculative GSI countdowns. A long-lived injected `HttpClient` supports browser sessions. Cast media resolve through the SDK's local path guard; bundled artwork stays within the installed package. No private types, external UI libraries or GPL HUD assets are copied.

## Compatibility and security

HUD code executes in the trusted local sOU browser origin; package validation is not runtime isolation. Install packages only from authors you trust. This HUD embeds no cloud/diagnostic token, API key, external camera URL, tracker or account credentials. Optional metadata polls only the local session-protected options endpoint, with bounded size/time and source-context matching; failure gracefully removes optional metadata.

All page backgrounds stay transparent. Earlier transparency, initial visibility and image-error fixes remain included. [Panel documentation](docs/panel.md) explains automatic radar maps and custom geometry, manual production controls, reference limits and the host's false checkbox defaults.

Builds, synthetic tests and browser checks do not certify native CS2/OBS capture or long-session performance. No source contract guarantees complete live data in every viewing mode. Test the actual target capture setup before a broadcast.

## License

MIT for this repository's own code, components, fixtures and original synthetic art. Included original Counter-Strike 2 artwork remains Valve/respective-owner content, separately documented in [LICENSES/VALVE-ASSETS.md](LICENSES/VALVE-ASSETS.md) with its [source/hash inventory](LICENSES/valve-assets.json); the MIT source license grants no rights to that artwork. See [LICENSE](LICENSE), [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) and `LICENSES/`. Dependencies retain their licenses; no endorsement or rights to sOU/Valve trademarks are implied.

## Publishing a new version

Update the manifest and reviewed documentation, then run build/tests and the existing release guards. Commit the source to `main`, wait for its successful workflow, then create a matching `vVERSION` tag. Its workflow repeats validation and publishes the built `.souhud` and SHA-256 as a GitHub Release. Existing releases are never overwritten.

Build jobs have read-only repository access. Only the release job can create release assets, using the automatic short-lived GitHub Actions token. No personal access token or NuGet publishing credential is required. Pull requests and ordinary branch builds keep CI artifacts without publishing a release.
