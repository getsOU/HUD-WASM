# Third-party notices

The HUD bundles twenty CS2 agent portrait placeholders from [CSGOSKINS.GG](https://csgoskins.gg/categories/agent). Character artwork retains Valve's and its creators' rights; it is not relicensed under the HUD source license. See `LICENSES/AGENT-PORTRAITS.md` and `LICENSES/agent-portraits.json`. All images load from the local package.

This project uses the following public packages. From 0.4.0 the HUD is GPL-3.0-only under `LICENSE`. The SDK and other dependencies retain the licenses below. Earlier HUD releases remain under their original MIT terms; `LICENSING.md` explains the scope.

| Dependency | Version | License / upstream |
| --- | --- | --- |
| Motion | 14.5.0 | MIT, Motion B.V.; [source](https://github.com/motiondivision/motion); bundled locally, license in `LICENSES/MOTION-MIT.txt` |
| SOU.Hud.Sdk / SOU.Hud.Sdk.Blazor | 0.3.0-preview.2 | MIT, copyright 2026 sofc; [NuGet SDK](https://www.nuget.org/packages/SOU.Hud.Sdk/0.3.0-preview.2), [Blazor adapter](https://www.nuget.org/packages/SOU.Hud.Sdk.Blazor/0.3.0-preview.2) |
| SOU.Live.Contracts | 0.3.0-preview.2 | MIT, copyright 2026 sofc; [NuGet](https://www.nuget.org/packages/SOU.Live.Contracts/0.3.0-preview.2) |
| SOU.Hud.Tool (build only) | 0.3.0-preview.2 | MIT, copyright 2026 sofc; [NuGet](https://www.nuget.org/packages/SOU.Hud.Tool/0.3.0-preview.2) |
| ASP.NET Core, Blazor WebAssembly and SignalR | 10.0.14 | MIT, .NET Foundation and Contributors; [source](https://github.com/dotnet/aspnetcore/tree/v10.0.14) |
| .NET WebAssembly runtime and libraries | 10.0.14 | MIT with bundled third-party components; [source](https://github.com/dotnet/runtime/tree/v10.0.14) |
| Dart Sass (build only) | 1.106.1 | MIT; [source](https://github.com/sass/dart-sass) |
| Playwright (tests only) | 1.64.1 | Apache-2.0; [source](https://github.com/microsoft/playwright) |
| Prettier (source formatting only) | 3.9.10 | MIT; [source](https://github.com/prettier/prettier) |

`LICENSES/DOTNET-LICENSE.txt` preserves the upstream .NET MIT license. `LICENSES/DOTNET-THIRD-PARTY-NOTICES.txt` preserves the upstream runtime notices verbatim; some entries apply to other runtime platforms. These files are copied into every HUD package. Resolved package identities and content hashes are in the committed `packages.lock.json` files.

81 weapon, grenade, status and killfeed SVGs come from [JohnTimmermann/OpenHud-React-Hud](https://github.com/JohnTimmermann/OpenHud-React-Hud/tree/b2c1cedc4f03e0a1df86c9be6f9794ce0a7f8cc0). The upstream MIT license, copyright (c) 2021 Lexogrine, is preserved in `LICENSES/OPENHUD-MIT.txt`. `LICENSES/OPENHUD-ICONS.md` and `LICENSES/openhud-icons.json` document this import and the separate rights of game-art owners. The same hash inventory ships at `assets/openhud-icons.json`.

The package also retains original Counter-Strike 2 radar textures, factual overview calibration and additional equipment/UI/killfeed artwork exported directly from the game. These remaining assets belong to Valve Corporation and their respective creators. They are **not MIT licensed by this project**; no unrestricted reuse, commercial distribution right, endorsement or ownership is claimed. Read [LICENSES/VALVE-ASSETS.md](LICENSES/VALVE-ASSETS.md) and the accompanying [source/hash inventory](LICENSES/valve-assets.json). Both files are installed with the HUD. Original overview configuration and the factual local catalog are included under `assets/radar/`.

The radar incorporates original GPLv3 Boltobserv map content and adaptations of the upstream marker/projection renderer at revision `0320883198e438cd6ae9fd490fa310027003727e`. Read `LICENSES/BOLTOBSERV-NOTICE.md` and `LICENSES/boltobserv-origins.json`. `LICENSES/BOLTOBSERV-SOURCE.zip` contains the complete original upstream source, and original map-specific notices are retained. HUD source is available in this repository; public SDK packages remain MIT. No Default/DCM/NYXIA bundle was copied. No external fonts, player photos, team logos, CDN scripts, analytics or advertising libraries are included.

The sOU wordmark in `assets/branding/sou-wordmark.png` is the unmodified official local sOU branding asset (`src/SOU.Api/wwwroot/brand/sou-wordmark-icon.png` in the sOU project). The HUD displays it in white using CSS. The sOU mark retains its owner's rights.
