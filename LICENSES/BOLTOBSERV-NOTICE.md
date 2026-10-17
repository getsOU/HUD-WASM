# Boltobserv radar

This HUD includes original map artwork, map calibration metadata and radar marker
artwork from [boltgolt/boltobserv](https://github.com/boltgolt/boltobserv), revision
`0320883198e438cd6ae9fd490fa310027003727e` (Boltobserv 1.6.1).

The original map PNGs, `meta.json5` files and WebP artwork are unchanged. Their
original source paths, Git blob IDs, SHA-256 hashes and distributed asset paths
are recorded in `boltobserv-origins.json`. `assets/boltobserv/maps.json` and
`assets/boltobserv/catalog.json` are JSON conversions of the original metadata;
their hashes are recorded separately as generated files.

Boltobserv is distributed under GNU GPL version 3. The complete license is in
`BOLTOBSERV-GPL-3.0.txt`. The original Ancient and Anubis map directories contain
their own MIT notices, copyright 2021 Lexogrine; these files are preserved in the
respective distributed map directories. These exceptions do not label the other
Boltobserv maps or renderer as MIT.

The complete original source at the pinned revision is included without runtime
execution as `BOLTOBSERV-SOURCE.zip`; its SHA-256 hash is recorded in the provenance
manifest. The adapted renderer and integration source belong to this HUD's
tagged source release. Radar rendering uses the sOU public HUD contracts and
local package assets. The original Boltobserv Electron application, local server,
Socket.IO transport and GSI receiver are not used by this HUD.

Boltobserv's README credits the radar images' collaboration with SimpleRadar and
readtldr.gg. Original logo overlays are supplied unchanged. Credit is retained;
no endorsement of this HUD by those projects is claimed. Original Counter-Strike
game artwork and trademarks remain the property of Valve and their respective
creators; see the separate Valve asset notices.

This pinned artwork may depict an earlier map revision. It is not a statement
that every current or custom CS2 map has matching Boltobserv geometry.
