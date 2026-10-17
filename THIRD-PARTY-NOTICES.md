# Third-party notices

This project uses the following public packages. From 0.4.0 the HUD is GPL-3.0-only under `LICENSE`. The SDK and other dependencies retain the licenses below. Earlier HUD releases remain under their original MIT terms; `LICENSING.md` explains the scope.

| Dependency | Version | License / upstream |
| --- | --- | --- |
| SOU.Hud.Sdk | 0.2.0-preview.4 | MIT, copyright 2026 sofc; [NuGet](https://www.nuget.org/packages/SOU.Hud.Sdk/0.2.0-preview.4) |
| SOU.Live.Contracts | 0.2.0-preview.4 | MIT, copyright 2026 sofc; [NuGet](https://www.nuget.org/packages/SOU.Live.Contracts/0.2.0-preview.4) |
| SOU.Hud.Tool (build only) | 0.2.0-preview.4 | MIT, copyright 2026 sofc; [NuGet](https://www.nuget.org/packages/SOU.Hud.Tool/0.2.0-preview.4) |
| ASP.NET Core, Blazor WebAssembly and SignalR | 10.0.13 | MIT, .NET Foundation and Contributors; [source](https://github.com/dotnet/aspnetcore/tree/v10.0.13) |
| .NET WebAssembly runtime and libraries | 10.0.13 | MIT with bundled third-party components; [source](https://github.com/dotnet/runtime/tree/v10.0.13) |

`LICENSES/DOTNET-LICENSE.txt` preserves the upstream .NET MIT license. `LICENSES/DOTNET-THIRD-PARTY-NOTICES.txt` preserves the upstream runtime notices verbatim; some entries apply to other runtime platforms. These files are copied into every HUD package. Resolved package identities and content hashes are in the committed `packages.lock.json` files.

The package includes original Counter-Strike 2 radar textures, factual overview calibration and equipment/UI/killfeed artwork exported directly from the game. These assets belong to Valve Corporation and their respective creators. They are **not MIT licensed by this project**; no unrestricted reuse, commercial distribution right, endorsement or ownership is claimed. Read [LICENSES/VALVE-ASSETS.md](LICENSES/VALVE-ASSETS.md) and the accompanying [source/hash inventory](LICENSES/valve-assets.json). Both files are installed with the HUD. Original overview configuration and the factual local catalog are included under `assets/radar/`.

The radar incorporates original GPLv3 Boltobserv map content and adaptations of the upstream marker/projection renderer at revision `0320883198e438cd6ae9fd490fa310027003727e`. Read `LICENSES/BOLTOBSERV-NOTICE.md` and `LICENSES/boltobserv-origins.json`. `LICENSES/BOLTOBSERV-SOURCE.zip` contains the complete original upstream source, and original map-specific notices are retained. HUD source is available in this repository; public SDK packages remain MIT. No Default/DCM/NYXIA bundle was copied. No external fonts, player photos, team logos, CDN scripts, analytics or advertising libraries are included.
