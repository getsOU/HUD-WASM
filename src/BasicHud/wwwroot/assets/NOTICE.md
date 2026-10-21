# Counter-Strike 2 artwork

This notice and `valve-assets.json` cover the remaining direct local game exports. The current source imports 81 icons from OpenHud-React-Hud, with separate provenance in `openhud-icons.json` and the packaged `LICENSES/OPENHUD-ICONS.md` and `LICENSES/OPENHUD-MIT.txt`. Imported paths are excluded from the direct-export inventory. Game-art rights remain with their respective owners.

The `radar/`, `weapons/`, `killfeed/`, and `ui/` artwork was exported directly
from the locally installed, public Counter-Strike 2 game archive on 2026-10-17.
It is original game artwork, owned by Valve Corporation and the respective
creators. It is **not relicensed by the HUD source license**. No ownership,
endorsement, or unrestricted redistribution/commercial license is claimed.

This HUD is an independent community project and is not endorsed by Valve.
Counter-Strike and its marks belong to Valve Corporation. Anyone reusing the
artwork must consider its owners' applicable terms separately from this project's
source license.

## Provenance

- Source application: Counter-Strike 2, Steam application `730`, installed build
  `25966348`.
- Source archive: `game/csgo/pak01_dir.vpk` and its referenced numbered VPK volumes.
- Radar textures: `panorama/images/overheadmaps/*_radar_psd.vtex_c`.
- Exact calibration and floor heights: `resource/overviews/*.txt`, preserved in
  `radar/config/`.
- Equipment silhouettes: `panorama/images/icons/equipment/*.vsvg_c`.
- Killfeed symbols: `panorama/images/hud/deathnotice/*.vsvg_c`.
- Additional UI symbols: `panorama/images/icons/ui/*.vsvg_c`.
- Development-only exporter: [Source 2 Viewer / ValveResourceFormat](https://github.com/ValveResourceFormat/ValveResourceFormat),
  version `20.0.6980+a06886f7d06049052d32a7381ec05523064a2ca0`.
  Its MIT license concerns that tool, not the game artwork. The tool is not shipped.
- `valve-assets.json` records each original archive entry, compiled-source SHA-256,
  source CRC-32, exported file size and SHA-256. Exported PNG geometry and SVG text
  are unchanged. Display coloring uses CSS rather than modifying the original files.

No files were copied from GPL Default/DCM/NYXIA HUDs or their radar renderers.
`radar/catalog.json` contains a small factual adapter generated from the accompanying
original overview definitions; it does not include a foreign renderer.

## Coordinate convention

For the unrotated 1024-pixel textures, use
`x = (worldX - posX) / scale` and `y = (posY - worldY) / scale`, then divide by the
texture width/height for normalized coordinates. The `rotate`/`zoom` fields in
some raw definitions are in-game presentation defaults, not extra rotations or
zooms to apply to these already north-up textures.

Nuke, Train and Vertigo include separate lower-floor images and the game's
explicit altitude ranges. Choose ranges with an inclusive minimum and exclusive
maximum so an exact shared boundary selects only one layer. Maps without declared
vertical sections have `minZ` and `maxZ` set to null. The supplied geometry and
calibration reflect the recorded game build and may need updating after game patches.

Map-level `sites` contain only the original overview's explicitly provided
`bombA_x`/`bombA_y` and `bombB_x`/`bombB_y` loading-screen positions, normalized
from 0 to 1. Missing, incomplete or invalid coordinates produce no marker. These
coordinates have no declared floor or altitude, and the catalog does not invent
one. Hostage maps without these fields have an empty `sites` array.

## Owner references

- [Counter-Strike 2](https://www.counter-strike.net/cs2)
- [Steam Subscriber Agreement](https://store.steampowered.com/subscriber_agreement/)
- [Valve Video Policy](https://store.steampowered.com/video_policy)

These links identify the owners' policies; this notice does not grant or assert
broader asset rights than those policies provide.
