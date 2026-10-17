# Third-party notices

This project uses the following public packages. The MIT license in `LICENSE` covers this repository's own HUD code and synthetic development fixtures; it does not change upstream licenses or grant rights to sOU trademarks.

| Dependency | Version | License / upstream |
| --- | --- | --- |
| SOU.Hud.Sdk | 0.2.0-preview.4 | MIT, copyright 2026 sofc; [NuGet](https://www.nuget.org/packages/SOU.Hud.Sdk/0.2.0-preview.4) |
| SOU.Live.Contracts | 0.2.0-preview.4 | MIT, copyright 2026 sofc; [NuGet](https://www.nuget.org/packages/SOU.Live.Contracts/0.2.0-preview.4) |
| SOU.Hud.Tool (build only) | 0.2.0-preview.4 | MIT, copyright 2026 sofc; [NuGet](https://www.nuget.org/packages/SOU.Hud.Tool/0.2.0-preview.4) |
| ASP.NET Core, Blazor WebAssembly and SignalR | 10.0.13 | MIT, .NET Foundation and Contributors; [source](https://github.com/dotnet/aspnetcore/tree/v10.0.13) |
| .NET WebAssembly runtime and libraries | 10.0.13 | MIT with bundled third-party components; [source](https://github.com/dotnet/runtime/tree/v10.0.13) |

`LICENSES/DOTNET-LICENSE.txt` preserves the upstream .NET MIT license. `LICENSES/DOTNET-THIRD-PARTY-NOTICES.txt` preserves the upstream runtime notices verbatim; some entries apply to other runtime platforms. These files are copied into every HUD package. Resolved package identities and content hashes are in the committed `packages.lock.json` files.

The package includes original Counter-Strike 2 radar textures, factual overview calibration and equipment/UI/killfeed artwork exported directly from the game. These assets belong to Valve Corporation and their respective creators. They are **not MIT licensed by this project**; no unrestricted reuse, commercial distribution right, endorsement or ownership is claimed. Read [LICENSES/VALVE-ASSETS.md](LICENSES/VALVE-ASSETS.md) and the accompanying [source/hash inventory](LICENSES/valve-assets.json). Both files are installed with the HUD. Original overview configuration and the factual local catalog are included under `assets/radar/`.

The HUD's own C#/Razor/CSS and small state-icon renderer remain MIT. No files were copied from GPL Default/DCM/NYXIA HUD bundles. No external fonts, player photos, team logos, CDN scripts, analytics or advertising libraries are included.
