# Project preferences

- Write patch notes and release notes in English as a bulleted list. Apply this to `CHANGELOG.md`, `scripts/release-notes.md`, and the `releaseNotes` field in `src/BasicHud/hud.json`.
- Publish releases exclusively through the official tag-triggered GitHub Actions workflow. Wait for dependency installation and all required checks; do not create releases, upload release assets manually, or bypass workflow gates as an alternative to a slow run.
