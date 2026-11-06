# Lucide UI icons

Ten SVG geometries are taken from [lucide-icons/lucide](https://github.com/lucide-icons/lucide/tree/1a49fd88d85202bad69f39462241b309a484bba0), revision `1a49fd88d85202bad69f39462241b309a484bba0`:

| Source | Local symbol in `assets/icons/hud-ui.svg` |
| --- | --- |
| `icons/heart.svg` | `health` |
| `icons/crosshair.svg` | `kills` |
| `icons/skull.svg` | `dead` |
| `icons/shield.svg` | `armor` |
| `icons/clock.svg` | `time` |
| `icons/eye-off.svg` | `blind` |
| `icons/cloud-fog.svg` | `smoke` |
| `icons/zap.svg` | `flashassist` |
| `icons/flame.svg` | `fire` |
| `icons/person-standing.svg` | `hostage` |

The original child shapes are unchanged. Their common SVG presentation attributes are supplied by `HudUiIcon.razor`: 24×24 viewBox, no fill, currentColor stroke, width 2, round caps and joins. No Lucide runtime or package is bundled. The source hashes and sprite hash are recorded in `hud-ui-icons.json`.

`helmet`, `kit`, `headshot`, `wallbang`, `noscope`, `utility-hegrenade`, `utility-flashbang`, `utility-smokegrenade`, `utility-decoy`, `utility-molotov`, `utility-incgrenade`, `rounds` and `magazine` are HUD-authored symbols, not official Lucide icons. `helmet` reuses Lucide's shield outline. These additions follow the HUD's GPL-3.0-only source license; the reused Lucide geometry retains its upstream terms. All custom symbols are enabled in the shared semantic renderer, including the three killfeed modifiers.

`LUCIDE-LICENSE.txt` preserves the complete upstream ISC notice and the Feather MIT notice. Crosshair is one of the Feather-derived icons. The license applies to these imported geometries, not the retained Valve assets.
