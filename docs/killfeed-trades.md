# Possible trades in the killfeed

- SDK: `0.3.0-preview.9`; documented desktop support: sOU `0.3.42` or newer.
- Manifest: `killfeed.trades.v1` is optional. Missing `TradedKill` produces no indicator.
- The indicator reads only `kill.TradedKill.Victim`, resolves the usual profile/source name and uses that participant's side. It never searches visible kills or walks deeper links.
- Row order: killer, optional assist/flash assist, trade arrows and teammate, weapon, modifiers, victim. The arrows and assist symbols are white; names retain their side colors.
- Display panel: **Hide trade indicators** (`display.hideTrades`). Missing/false keeps them enabled.
- The full resolved name appears in the requested tooltip, `Möglicher Trade für {name}`. Names remain text and use ellipsis when space is limited.
- Trade/profile/panel updates preserve the row key and its existing eight-second lifetime, including during round review.

Reference: [sOU SDK: possible trades in the V3 killfeed](https://www.getsou.app/docs/hud-sdk-referenz?ui_lang=de#killfeed-trades).

## Verification

Run the normal package build, then:

```powershell
npm run test:trades -- artifacts/package
```

The browser test loads the actual Release WebAssembly output through the public SDK runtime. It covers ordinary kills, late nested mappings with predecessors outside the visible feed, assists/flash assists, alias updates, literal markup, long-name truncation, white icons, participant colors, panel hide/show, round review, stable row identity and unchanged lifetime. The normal browser regression suite remains separate.

The locally restored public SDK tool's raw snapshot-fixture path does not forward supplied `TradedKill` into its V3 kill messages. The test therefore supplies explicit synthetic public V3 kill DTOs at the browser transport boundary, while retaining the dev host's sessions, revisions and options/panel transport. This is confined to the test harness; production has no injection, replacement fetch or trade inference. These synthetic tests do not constitute a live Desktop/CS2 acceptance test.
