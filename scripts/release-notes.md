# 0.4.17

This release updates the scoreboard heading, roster headings and radar presentation.

- Split the scoreboard heading like NYXIA: tournament and season on the left, the local sOU wordmark in white at the center, and division/group/section path plus competition round on the right. Keep the logo centered independently of text length and use the saved caption when competition data is absent.
- Remove the live roster economy strips and their obsolete panel toggle. Show team names as separate side-colored badges with condensed text and a clear gap above the first player card, preserving the card positions.
- Remove only the Boltobserv/Simple Radar branding overlay from radar rendering. Map terrain, buyzones, player markers and effects remain unchanged; original source assets and license notices remain included.

Validation: locked restore, formatting, 268 model/component/asset checks, Release publish, package validation, 13 uploader tests and release guards. Browser checks at 1600, 1920 and 2560 pixel widths verify the centered white logo, non-overlapping competition fields, separate team badges, card placement and absence of radar branding.

Import **0.4.17** and reopen outputs. The package ID remains `sou-basic-wasm`.
