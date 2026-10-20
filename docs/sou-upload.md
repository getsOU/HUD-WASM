# Upload GitHub releases to sOU

This repository uploads published packages to the existing **sOU HUD WASM** entry:

| Setting | Fixed value |
| --- | --- |
| Organization | `081f9efd-e2ec-4cc8-a83b-3faf56ba8fcb` |
| HUD entry | `dbd77df2-696a-429f-ac1b-d54e98ceabca` |
| Package identity | `sou-basic-wasm` |
| API | `https://www.getsou.app/api/v1/integration` |

The workflow does not create another HUD, change its visibility/listing, approve
a release, or update a running output. New uploads start as `pending` and require
the regular sOU review before distribution.

## One-time GitHub secret

1. Use a sOU **Creator API** key with **read/write** access for the organization
   above. The owner needs current explicit Publisher permission. Creator keys
   start with `sou_hud_`; workspace keys (`sou_ws_`) cannot upload HUDs.
2. Open **Settings → Secrets and variables → Actions → New repository secret**.
3. Name: **`SOU_CREATOR_API_KEY`**. Paste the complete key into **Secret** and save.
4. Use a repository secret, not a plain Actions variable. No additional secrets,
   ID variables or new GitHub permissions are required. Never put the key in
   source files, workflow inputs, command arguments or issue text.

[Add repository secret](https://github.com/getsOU/sOU-HUD-WASM/settings/secrets/actions/new).
Expiry, revocation, account status and Publisher permissions remain enforced by
sOU. Rotate by replacing the same GitHub secret before revoking the old key.

## Upload an already published version

After saving the secret, open **Actions → Upload release to sOU → Run workflow**.
Select branch **main**, enter an existing stable tag such as **`v0.4.9`**, then run.
This downloads the original `.souhud` and SHA-256 file from that GitHub release;
it neither rebuilds nor creates a new HUD version.

The log reports only version, outcome, release ID and review state. Check the
version in sOU afterwards. `pending` confirms upload, not approval.

## Future releases and safe retries

The existing **Build and verify** tag workflow builds/tests the package and
publishes immutable GitHub assets first. Its dependent **upload-sou** job then
calls the reusable upload workflow. Main pushes and pull requests only build and
run offline uploader tests; they never access the sOU secret or upload.

The upload uses the exact published bytes, not another build. It checks tag,
manifest identity/version/runtime and SHA-256, then reads the existing HUD to
verify organization, HUD ID and package ID. Requests use a fixed HTTPS host and
`Authorization: Bearer …`; redirects are never followed. The request body is the
raw archive with `application/octet-stream`.

An existing identical version/hash in `pending` or `approved` is an idempotent
success without POST. A different hash, rejected release or unknown state fails
and is never overwritten. After an uncertain POST response or HTTP conflict,
the script reads the release list once and compares version/hash; it never
retries the POST automatically. Uploads of the same tag are serialized. Failure in
sOU leaves the already published GitHub release intact.

## Troubleshooting

- Missing secret: save `SOU_CREATOR_API_KEY` as described above.
- HTTP 401: check key expiry/revocation and account status.
- HTTP 403: check Creator read/write access and current Publisher permission.
- HTTP 404: check the HUD still belongs to the configured organization.
- HTTP 409 or uncertain reply: inspect sOU before manually running again. The
  workflow has already compared any existing version/hash; never replace bytes.
- HTTP 429: wait for the shared API rate limit to clear before a manual retry.
- Different hash or rejected version: inspect sOU and publish a genuinely new
  version if a corrected package is needed; never reuse the version/tag.

Response bodies and raw transport exceptions are excluded from logs. Isolated
uploader tests use Python 3.11+ and need neither credentials nor network:

```sh
python3 -m unittest discover -s tests/upload -v
```

The HUD SDK and package version are unchanged by this integration. Authenticated
production upload remains unverified until a run with the configured secret succeeds.

[Public Creator API reference](https://www.getsou.app/docs/workspace-api#creator-api).
