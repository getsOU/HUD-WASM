# sOU Basic HUD · Blazor WebAssembly

A small, readable CS2 broadcast HUD built with C# and Blazor WebAssembly. It consumes the **public** [SOU.Hud.Sdk](https://www.nuget.org/packages/SOU.Hud.Sdk/0.2.0-preview.4) and [SOU.Live.Contracts](https://www.nuget.org/packages/SOU.Live.Contracts/0.2.0-preview.4) NuGet packages, both pinned to `0.2.0-preview.4`. There are no references to sOU's private application source.

The equivalent React implementation lives in [getsOU/sOU-HUD-React](https://github.com/getsOU/sOU-HUD-React). Both use the native sOU v2 protocol and the same panel fields.

![Basic Blazor WebAssembly HUD over a checkerboard behind its transparent output](docs/preview.png)

The screenshot shows the actual Release package with synthetic example data and the initial sOU panel state in an isolated local package host. Both player sections are enabled. The checkerboard sits behind the embedded HUD to demonstrate transparency; it is not included in the HUD.

## Use the HUD

1. Download `sou-basic-wasm-0.1.2.souhud` from this repository's [Releases](https://github.com/getsOU/sOU-HUD-WASM/releases).
2. Open the [sOU desktop app](https://www.getsou.app/get-sou) and choose **HUDs → Paket importieren**. Select the `.souhud` file and install it.
3. Open its output through sOU. Copy that local output URL into an OBS Browser Source with **1920 × 1080** dimensions. Keep the browser background transparent.
4. Team rosters and the observed player are enabled by default. In the HUD's **Panel**, set the scoreboard caption or check **Hide team rosters** / **Hide observed player** to hide a section. Save and apply your changes.
5. Supply CS2 game data using sOU's normal setup. Before the first game snapshot, the overlay says **Waiting for game data**. Starting a separate development server does not connect it to a running cast.

Package ID: `sou-basic-wasm`. Package version: `0.1.2`. Protocol: v2. Installed versions are immutable; increase the manifest version for a changed release.

### Visibility defaults and upgrading to 0.1.2

Earlier versions used **Show** checkboxes, which sOU initialized to unchecked and therefore hid the rosters and observed-player section on first use. Version `0.1.2` uses **Hide** checkboxes: unchecked or missing values leave both sections enabled, including after saving only a new caption. An observed-player card still requires an observed player in the game data.

Your saved caption is inherited when sOU initializes the new version's panel. The visibility fields have new names, so set any desired hidden sections once again after upgrading. Prior releases, their saved settings and already running outputs are unchanged. Install `0.1.2`, select it and reopen the output to use the fix; no SDK or sOU desktop update is required.

### Black background in version 0.1.0

Version `0.1.0` can show an opaque black background in an embedded sOU overlay despite transparent CSS backgrounds. Version `0.1.1` removes the root document's forced dark color scheme while keeping the HUD panels and text colors. Install `0.1.1`, select that version and reopen the output; an already running output keeps its original package version. The previous screenshot's dark background was not evidence that the embedded output was transparent.

## What it shows

- CT on the left in blue, T on the right in warm orange, with current-side scores.
- Managed team names only when sOU explicitly resolves that team's current side; otherwise source-side names or CT/T labels. Team A is never assumed to be CT.
- Map, round number, phase and a source-provided round or bomb countdown. Timer values are rounded up to `m:ss`; the HUD does not invent or locally extrapolate missing seconds.
- Team rosters sorted by observer digits **1–9, then 0**, with health, armor, money, kills/deaths/assists, active/reloading weapon and ammunition.
- Observed player details at the bottom. It does not substitute the local/menu player.
- Separate transport and data-freshness status. Disconnected or stale data is clearly labeled; the last snapshot stays visible until replaced. A new session replaces old player and score state.

Missing values are `—`; missing health does not mean dead. The layout fits a transparent 1920 × 1080 canvas and scales uniformly inside other 16:9 or letterboxed windows. This basic layout targets normal 5v5 casts. It has no radar, killfeed, external assets or gameplay inference.

## Panel fields

| Section / field | Type | Default |
| --- | --- | --- |
| `display.caption` | text | `sOU BASIC` (also used for blank text) |
| `display.hideRosters` | checkbox · Hide team rosters | `false`; only `true` hides the rosters |
| `display.hideObservedPlayer` | checkbox · Hide observed player | `false`; only `true` hides the observed player |

Panel state is replayable. There are no action cues to replay. After a host reconnect, current panel state is applied again, including reset revision numbering.

## Build, test and package

Install the **.NET SDK 10.0.402** and **PowerShell 7**. Packages come only from `nuget.org`; no private feed or credentials are required. `global.json`, lock files and the local tool manifest pin the SDK, package graph and packaging tool. The application targets .NET / ASP.NET Core **10.0.13**. Use this current SDK rather than an older SDK with a lower host runtime: Release trimming tools also need the matching runtime patch.

From the repository root:

```powershell
pwsh ./scripts/Build.ps1
```

The script restores locked dependencies, restores public `SOU.Hud.Tool`, verifies C# formatting, runs the focused tests, publishes the Release WASM app, creates the installable `.souhud`, validates it and writes a SHA-256 file. Outputs:

```text
artifacts/sou-basic-wasm-0.1.2.souhud
artifacts/sou-basic-wasm-0.1.2.souhud.sha256
```

It refuses to overwrite an existing package or publish directory. For another build, choose a fresh directory:

```powershell
pwsh ./scripts/Build.ps1 -OutputDirectory artifacts/check-2
```

To run only the focused tests:

```powershell
dotnet restore tests/BasicHud.Tests --configfile NuGet.Config --locked-mode
dotnet run --project tests/BasicHud.Tests -c Release --no-restore
```

Tests cover missing values, explicit side identities, conflicting mappings, observer ordering, timers, panel defaults, stale revisions, new-session reset, stream failure and cancellation/disposal of both subscriptions. Optional native WebAssembly AOT / `wasm-tools` optimization is not required by this build.

## Isolated visual preview

Synthetic preview fixtures compile **only into Debug builds** and activate **only on a loopback address** with an explicit recognized query. They are read-only in the browser and never connect to or write into a cast, GSI input or sOU host.

```powershell
dotnet restore src/BasicHud --configfile NuGet.Config --locked-mode
dotnet run --project src/BasicHud --urls http://127.0.0.1:5178
```

Open <http://127.0.0.1:5178/?preview=live>. Other scenarios: `waiting`, `stale`, `swapped`, `missing`, `bomb`. The **LOCAL PREVIEW** badge identifies synthetic data. Release packages exclude the fixture source and cannot activate it. Without preview mode, open the installed package through sOU rather than this development URL.

## Code structure

```text
src/BasicHud.Core/       immutable presentation models, panel defaults, subscription lifecycle
src/BasicHud/            Razor components, public SDK adapter, local CSS and package manifest
src/BasicHud/Development Debug-only synthetic fixtures
tests/BasicHud.Tests/    executable focused checks, no UI or live game required
scripts/Build.ps1       locked restore → tests → Release publish → pack → validate → hash
```

The SDK owns browser-session setup and live/panel reconnect behavior. The HUD owns only presentation. Both subscriptions share a long-lived `HttpClient`, are canceled and awaited on disposal, and unsubscribe their connection events. An unexpected consumer failure becomes a visible failed channel state rather than an unobserved task exception. CSS and scripts are local; the relative `<base href="./">` supports installed versioned package URLs.

## Security and limits

HUD packages execute code in the local sOU browser origin. Install packages only from authors you trust; validation is not isolation. This HUD requires no cloud token, diagnostic token, API key, external network service, tracking cookie or personal account data. Runtime requests go to the local sOU session and its native live/panel streams. Never paste account credentials into a HUD.

Builds and synthetic/browser checks do not constitute a native CS2/OBS capture or long-session performance certification. Follow the [public HUD author guide](https://www.getsou.app/docs/hud-entwicklung) for the current host contract and installation details.

## License

MIT for this repository's own code and fixtures; see [LICENSE](LICENSE). Dependencies keep their licenses; see [THIRD-PARTY-NOTICES.md](THIRD-PARTY-NOTICES.md) and `LICENSES/`. No rights to sOU trademarks or third-party game artwork are implied.

## Publishing a new version

Update the HUD manifest version, then run the
build and tests. Commit the reviewed source to `main`. An existing `vVERSION`
tag matching `hud.json` starts the same checks, then publishes the built
`.souhud` and its SHA-256 as a GitHub Release. Pull requests and ordinary branch
builds only retain CI artifacts. Publication checks repository, event, tag,
manifest and package hash; existing releases are never overwritten.

The build job has read-only repository access. Only the release job can create
release assets, using the automatic short-lived GitHub Actions token. No
personal access token or NuGet publishing credential is required. Test the
actual target CS2/OBS setup separately before using a changed HUD for a cast.
