# 0.1.2

Team rosters and the observed-player section are now enabled by default, including after the first panel update or a caption-only save. The panel uses **Hide team rosters** and **Hide observed player**: only a checked Hide field disables its section. The observed-player card still requires an observed player in the source data.

Install `sou-basic-wasm-0.1.2.souhud`, select version `0.1.2` and reopen the output. Your saved caption is inherited when sOU initializes the new version's panel. Visibility fields have new identifiers (`display.hideRosters` and `display.hideObservedPlayer`), so configure any sections you want hidden once again after upgrading. Existing releases, saved settings and already running outputs remain unchanged.

The transparency fix from `0.1.1` is retained. Public SDK dependencies stay at `0.2.0-preview.4`; no SDK or sOU desktop update is required.

## 0.1.1

Fixes an opaque black background when the HUD is embedded in sOU's overlay. The root document no longer forces a dark browser color scheme; the HUD's panel colors, transparent page backgrounds and layout remain the same.

Install `sou-basic-wasm-0.1.1.souhud`, select version `0.1.1` in sOU and reopen the output. Existing `0.1.0` packages and running outputs are unchanged. SDK dependencies remain at `0.2.0-preview.4`.

Source/build checks and isolated browser checks are separate from a native CS2/OBS capture test.
