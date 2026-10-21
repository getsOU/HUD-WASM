# Changelog

## 0.4.10

- Correct the actual player-card structure: kill/death icon groups, portrait, larger weapon/HP, utility near the portrait and kit/bomb/armor at the inner edge. Keep assists accessible.
- Confine health fill to the loadout, shorten known-dead cards with a large death cue, and use a white observed-player outline. Mirror regions without reversing statistic order.
- Add one unchanged original CS2 crosshair SVG with archive provenance and measured optical bounds.

- Rebuild the live-scene layout around the supplied cast reference: narrow central scoreboard, vertical series markers, larger radar and map/pick strip above it.
- Use mirrored blue/gold rosters, health-driven background fill, central portraits and compact equipment rows. Preserve optional economy in a slim rail above each roster.
- Center the observed player in a compact portrait/loadout card with a bottom name/HP strip; no webcam or reference-video assets.
- Preserve other scenes, transparent output, source logic, original icons and existing inventory/killfeed behavior.

## 0.4.9

- Mirror the two roster layouts around larger central portraits.
- Separate name / K-D-A / cash, carried weapon with large health digits, and a compact utility/status row.
- Keep stable areas for absent images, inventory changes and bounded long names; retain all previous equipment and killfeed behavior.

## 0.4.8

- Show carried primary firearm, otherwise pistol, otherwise knife in roster rows, independently of the observer held weapon.
- Separate weapon and compact utility with a dedicated gutter; preserve absent/dead inventory handling.

## 0.4.7

- Keep utility and ammo compact within stable outer data areas; replace counts with repeated grenade icons.
- Preserve complete killfeed names and every current event, with eight-second expiry and no six-row cap.
- Color assisters by their own side; place blind/flash-assist cues by participants and shot modifiers before the victim.

## 0.4.6

- Remove duplicate active-weapon text beside original observer icons.
- Reserve live roster, observer and economy data with fixed grid tracks, including empty cells.
- Keep known utility and status types in stable slots; counts and weapon silhouettes cannot move adjacent fields.

## 0.4.5

- Balance visible original SVG contours instead of forcing different shapes into equal square boxes.
- Align utility on a common baseline with consistent spacing and count placement.
- Append new kills at the bottom; retain six newest events in chronological order.


## 0.4.4

- Use original CS2 SVGs consistently for status, equipment, killfeed and round results. Add original clock and rescued-hostage artwork.
- Remove hand-drawn icon fallbacks; unavailable assets retain a text label. Preserve SVG aspect ratios and size status/utility icons for their HUD context.


## 0.4.3

- Restore original Boltobserv Arial numerals, line height, marker/label proportions, active size and shared height scaling.
- Render WASM player dots and upright labels in a separate HTML layer, matching the original and React.


## 0.4.2

- Match NYXIA radar motion and coherent HLAE source selection; correct the WASM receipt clock.
- Show explicit pick teams and deciders in all map-series displays, without guessing missing decisions.

## 0.4.1

- Leave the current weapon area empty when an alive player has no active/reloading weapon. Holstered inventory is never used as a substitute.
- Hide stale weapon/inventory icons, names and ammunition for known dead players in live rosters, the observed-player card and detail views.
- Preserve the separate fallback for an actual supplied unknown weapon, actual reloading weapons and historical killfeed weapon icons. Unknown health remains unknown.
- Keep the existing composition, original Boltobserv radar, all eight views, GPL terms and public MIT SDK dependencies unchanged.

Install the new package version and reopen its outputs. Earlier release archives remain unchanged. See [release notes](scripts/release-notes.md) for the full history through 0.4.0 and earlier.
