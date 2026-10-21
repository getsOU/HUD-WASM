# OpenHud icon assets

81 SVG files are imported from [JohnTimmermann/OpenHud-React-Hud](https://github.com/JohnTimmermann/OpenHud-React-Hud/tree/b2c1cedc4f03e0a1df86c9be6f9794ce0a7f8cc0), revision `b2c1cedc4f03e0a1df86c9be6f9794ce0a7f8cc0`. 80 files are byte-identical. `weapons/flashbang.svg` adds the missing `xmlns="http://www.w3.org/2000/svg"` declaration so browsers can load it as a standalone image; its artwork is unchanged.

The upstream repository carries the MIT license, copyright (c) 2021 Lexogrine. Its full license is preserved in `OPENHUD-MIT.txt`. Counter-Strike game artwork and trademarks retain their respective owners' rights; the upstream software license does not establish additional rights in Valve artwork. No endorsement is implied.

`openhud-icons.json` lists every source path, installed path, byte count, source SHA-256, installed SHA-256 and import operation. The same inventory ships at `assets/openhud-icons.json`. Copied files are not described as direct local game exports in `valve-assets.json`.

Weapon and grenade silhouettes use their existing equipment keys. Status mappings use the reference's health, kills, full armor, armor/helmet, defuse, timer, skull and burning icons. Killfeed modifiers use its flashed-kill, headshot, wallbang, smoke and noscope SVGs; its suicide symbol remains a separate asset. The existing flash-assist and hostage SVGs remain because no equivalent SVG is supplied by this reference. Radar artwork is unchanged.

Only local SVG files are imported. No React runtime, HUD Manager integration, upstream scripts, remote images or player portraits are included. Visible alpha bounds are measured at up to eight times the source resolution and recorded with source hashes in `docs/cs2-icon-geometry.json`; generated CSS fits the unchanged source artwork to the existing icon frames.
