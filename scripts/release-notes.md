# 0.4.19

Migrate to HUD API v3 and add an optional horizontal playercard layout.

- Use SDK 0.3.0-preview.2 for one shared production session, typed options, manager views, synchronized time and reconnection. Update only the HUD regions affected by changed data.
- Add **Playercards horizontal** in the panel. Both teams use the same content order and left observer slots. Health fills the entire background; compact dead cards omit the large skull and align to the bottom.
- Consolidate presentation logic in the main project and keep legacy preview sessions Debug-only. Split styles into SCSS modules.
- Keep explicitly cleared panel captions empty instead of restoring the options caption.

Requires **sOU desktop 0.3.34 or newer**. Import **0.4.19**, select the new version and reopen outputs. The package ID remains `sou-basic-wasm`; existing release packages remain unchanged.

Validation: locked restore, formatting, 291 model/component/asset checks, Release publish, SDK package validation, Chromium v3 rendering checks, uploader tests and release guards. Synthetic preview checks do not replace testing OBS/native output with real game data.
