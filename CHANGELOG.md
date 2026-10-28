# Changelog

## Unreleased

- Add the opt-in **Playercards horizontal** panel setting: two rows of five compact cards at the bottom, with the observer between them. Both teams share the same content order, with observer digits and strips on the left. Health fills the complete card background and recedes from top to bottom as damage is taken. Dead cards omit the large skull and lower equipment row, retaining a shorter portrait/identity card aligned to the same bottom edge. Preserve the vertical default, side colors, agent/profile portraits, condition effects and observed-player outline.

- Consolidate presentation logic, region selectors and clock display in the main project's `Presentation/` folder and namespace. Remove the separate Core project, references and lock file; tests link the production sources directly.
- Keep the synthetic session, feed interface and connection states in Debug-only `Development/PreviewSession.cs`. Exclude them and the unused legacy connection indicator from Release compilation; production sessions are owned entirely by the SDK.

- Migrate to public SDK 0.3.0-preview.2 and HUD API v3 with the official Blazor host, one shared session, typed options, manager views, synchronized clock and SDK-owned reconnect/disposal.
- Subscribe independently to radar, scoreboard, each roster player, observer, history, series, killfeed, sponsors, production controls and presentation data. Ignore unrelated pose/ammunition changes and remove the global production rerender timer. Preserve the current design and explicit Debug-only synthetic preview.
- Move hand-authored CSS into ordered SCSS modules for foundations, components, live theme and presentation views. Compile one stylesheet with pinned Dart Sass; verify generated CSS and icon geometry during builds and CI.
- Update .NET SDK/runtime to 10.0.403/10.0.14 and support API-v3 uploads while retaining manual uploads of historical API-v2 releases.

## 0.4.18

- Anchor both live player rosters with equal bottom and outer-side margins of 0.8% of the HUD width.

## 0.4.17

- Split the scoreboard heading like NYXIA: tournament and season on the left, white sOU wordmark in the center, section path and competition round on the right. Preserve the caption as fallback when competition data is absent.
- Stop displaying the radar branding overlay containing the Boltobserv and Simple Radar logos. Map terrain, buyzones, player markers and effects remain unchanged; original source assets and license notices are preserved.
- Remove the live roster economy strips and their obsolete panel toggle. Style each team name as a freestanding side-colored badge with reference-sized condensed text and a clear gap above the first player, preserving player-card placement.

## 0.4.16

- Rebuild the observed-player card around a large portrait, a compact team/name/K-D/ammo/health strip and an attached sponsor footer, using the existing HUD palette and icons. Keep alpha-masked effects and side/slot agent fallback.
- Move live sponsors into the observer footer when the card is visible, without duplicating logos. Preserve the sponsor visibility option and the standalone sponsor placement when the observer card is absent.

## 0.4.15

- Remove redundant smoke, flash and fire status badges from roster cards and the observed player's equipment; portrait animations already display these conditions.
- Remove the dark vertical seam beside T-side portraits at fractional card positions. Right-align the health fill directly instead of mirroring its background layer; preserve the mirrored health direction.

## 0.4.14

- Replace portrait color washes with procedural canvas smoke clouds, deforming flames/sparks and an expanding circular flash. Bundle Motion 14.5.0 locally with its MIT license.
- Start all smoke clouds simultaneously at the same height below the avatar; reach their distinct destinations together over 3450ms. Higher clouds move faster, and all clouds keep drifting and changing shape while active.
- Give flames broad curved bodies and distinct points. Fire reaches one-third at its original speed, pauses for 250ms, then grows slowly over three seconds. Smoke and fire enter as moving elements with no rolling reveal boundary.
- Expand flash from the bottom-left corner across the avatar. Fade opacity at the reached radius when cleared, with no shrinking back toward the origin.
- Keep all transitions interruptible, including reactivation during exit. Preserve the actual image's alpha mask, crop and fallback selection; reset on player/image/round changes, death or stale data. Stop animation frames when hidden and honor reduced-motion preferences.
- Add a Debug-only cycling preview for full effects and interrupted exits. HE-hit effects remain deferred until reliable cause-specific events are available.

## 0.4.13

- Add animated smoke, flash glare and rising fire overlays to player portraits across live rosters, observer cards, lineups and spotlight. Effects use only positive, fresh source condition flags; dead/stale/cleared states remove them.
- Mask the combined effect layer with the alpha channel of the actually displayed player/agent image, using the same crop and position. Image failures and the agents-only panel option update both the image and mask together. Reduced-motion preferences keep a static effect.
- Do not infer HE hits from health loss: grenade-hit animation remains deferred until a reliable cause-specific event is available.

## 0.4.12

- Declare the live HUD's CT blue (`#B6DEF5`) and T gold (`#EEE1A0`) in the package's `outlineColors` palette, allowing sOU to apply matching HLAE X-Ray, utility-trail and derived smoke colors for the active CS2 desktop overlay.

- Add the panel option **Nur Agenten anzeigen** (`display.agentsOnly`) to replace every player photo, including selected spotlight images, with its side/slot agent. Default photo priority and missing-image agent fallback remain unchanged; logos and sponsors are unaffected.

- Remove borders from inactive roster cards; highlight only the observed player with a white outline and small drop shadow.
- Align utility on one baseline and normalize visible grenade heights to Flashbang. Correct floating-point SVG bounds in the optical-layout generator so fractional artwork padding is actually removed.

## 0.4.11

- Set the package information URL to the public HUD-WASM repository and remove the bottom-right connection/preview status overlay from every output view.

- Match roster-card proportions to the broadcast reference: 320 × 74.88 cards, 12-pixel row gaps, wider observer strips and portraits, inset identity text, and mirrored loadout padding. Reserve the actual Health text width so `100` stays fully inside either card edge, including fallback fonts and scaled outputs.

- Add twenty local CS2 agent portrait placeholders from CSGOSKINS.GG: ten distinct CT agents and ten distinct T agents, one per observer digit 0–9 on each side. Real player portraits take priority; missing or failed portraits use the current side/slot agent in live rosters, observer cards and player presentation views.
- Update placeholders on side/slot changes, stop retrying failed images, preserve unknown-side/slot behavior and ship exact image source/hash records.

- Import 81 weapon, utility, status and killfeed SVGs from JohnTimmermann/OpenHud-React-Hud at revision `b2c1cedc4f03e0a1df86c9be6f9794ce0a7f8cc0`. Artwork is unchanged; add the missing SVG namespace to Flashbang for standalone image loading.
- Use the dedicated skull and burning status icons, retain existing artwork where the reference has no equivalent, and recalculate optical sizing for imported SVG viewports.
- Ship the upstream Lexogrine MIT notice and exact source/hash inventory; retain separate rights and provenance for the remaining Valve assets.

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
