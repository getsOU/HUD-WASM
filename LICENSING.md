# Licensing and corresponding source

Copyright (c) 2026 sofc. The HUD project from version **0.4.0** is distributed under **GNU GPL version 3 only** (`GPL-3.0-only`); see `LICENSE`. This includes its integration of the original Boltobserv radar. The original MIT releases through 0.3.0 remain available under their original license. The MIT notice for code retained from those releases is preserved in `LICENSES/SOU-HUD-MIT-THROUGH-0.3.0.txt`.

The complete HUD source, tests, locked dependency metadata and build/package scripts are published in [getsOU/sOU-HUD-WASM](https://github.com/getsOU/sOU-HUD-WASM). Each release's tag and GitHub source archives identify its corresponding source; follow `README.md` to build it. No private sOU application checkout or proprietary build service is required.

## Boltobserv

Original project: [boltgolt/boltobserv](https://github.com/boltgolt/boltobserv), revision `0320883198e438cd6ae9fd490fa310027003727e`. Original radar textures, map metadata and marker artwork retain their upstream copyrights and GPL-3.0 terms. Original Ancient and Anubis map directories each contain an additional MIT license, copyright 2021 Lexogrine; these texts are retained alongside the original assets and in `LICENSES/BOLTOBSERV-Ancient-MIT.txt` and `LICENSES/BOLTOBSERV-Anubis-MIT.txt`.

`LICENSES/BOLTOBSERV-SOURCE.zip` contains the complete unmodified upstream source at that revision, including original rendering sources, configuration, map assets and licenses. Its SHA-256 is `c808f6601727d41040c17c4b640bca60dcdb68655638a6d00833e3f020acd65e`. Local adaptation connects the radar to the public sOU v2 client, preserves source freshness/session rules and renders within the HUD. No Electron application, external GSI receiver or Boltobserv server is started. No endorsement by the upstream authors is implied.

## Dependencies and artwork

The public sOU SDK, NuGet packages and SDK templates **remain MIT**. This HUD's license change does not change their license or the license of independent HUDs built with that SDK. .NET and its bundled dependencies retain their notices. Original Valve weapon/UI/map artwork remains subject to the separate original rights described in `LICENSES/VALVE-ASSETS.md`; this project does not relicense it under GPL or MIT. Separately imported team, player and sponsor images retain their own rights. Preserve `THIRD-PARTY-NOTICES.md` and the files under `LICENSES/` when distributing the package.
