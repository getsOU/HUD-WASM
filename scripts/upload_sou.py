"""Upload an immutable GitHub release to the existing sOU Creator HUD."""
import argparse
import hashlib
import http.client
import json
import os
from pathlib import Path
import re
import sys
import uuid
import zipfile

REPOSITORY = "getsOU/sOU-HUD-WASM"
ORGANIZATION = "081f9efd-e2ec-4cc8-a83b-3faf56ba8fcb"
HUD = "dbd77df2-696a-429f-ac1b-d54e98ceabca"
PACKAGE = "sou-basic-wasm"
HOST = "www.getsou.app"
TARGET = f"/api/v1/integration/organizations/{ORGANIZATION}/huds/{HUD}"
LIMIT = 128 * 1024 * 1024


class UploadError(Exception):
    """Only controlled, secret-free messages may reach CI output."""


def version_for(tag):
    if not re.fullmatch(r"v(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)\.(0|[1-9][0-9]*)", tag):
        raise UploadError("Use an existing stable release tag such as v0.4.9.")
    return tag[1:]


def package_for(directory, tag):
    version = version_for(tag)
    package = Path(directory) / f"{PACKAGE}-{version}.souhud"
    if not package.is_file() or not 0 < package.stat().st_size <= LIMIT:
        raise UploadError("Package is missing, empty or exceeds 128 MiB.")
    with package.open("rb") as source:
        digest = hashlib.file_digest(source, "sha256").hexdigest()
    try:
        checksum = package.with_suffix(".souhud.sha256").read_text("utf-8").rstrip("\r\n")
        with zipfile.ZipFile(package) as archive:
            entries = archive.infolist()
            manifests = [entry for entry in entries if entry.filename == "hud.json"]
            if (len(entries) > 4096 or sum(entry.file_size for entry in entries) > LIMIT
                    or len(manifests) != 1 or manifests[0].file_size > 64000):
                raise UploadError("Package archive limits or manifest are invalid.")
            manifest = json.loads(archive.read(manifests[0]))
    except (OSError, ValueError, zipfile.BadZipFile, RuntimeError):
        raise UploadError("Cannot read package manifest or checksum.") from None
    if checksum != f"{digest}  {package.name}":
        raise UploadError("Package differs from the published SHA-256 file.")
    if not isinstance(manifest, dict) or any(manifest.get(key) != value for key, value in {
        "id": PACKAGE, "version": version, "apiVersion": 2,
        "runtime": "blazor-wasm", "entryPoint": "index.html",
    }.items()):
        raise UploadError("Package identity, version or runtime does not match this HUD release.")
    return package, version, digest


class CreatorApi:
    def __init__(self, token):
        self.token = token

    def request(self, method, path, package=None):
        # A fixed HTTPS host and direct connection never follow redirects with the key.
        connection = http.client.HTTPSConnection(HOST, timeout=120)
        headers = {"Authorization": f"Bearer {self.token}", "Accept": "application/json"}
        try:
            if package is None:
                connection.request(method, path, headers=headers)
            else:
                headers.update({"Content-Type": "application/octet-stream",
                                "Content-Length": str(package.stat().st_size)})
                with package.open("rb") as source:
                    connection.request(method, path, body=source, headers=headers)
            response = connection.getresponse()
            data = response.read(2 * 1024 * 1024 + 1)
            if len(data) > 2 * 1024 * 1024:
                raise UploadError("API response exceeds the allowed size.")
            return response.status, data
        except (OSError, http.client.HTTPException, ValueError):
            raise UploadError("API connection failed; no automatic upload retry.") from None
        finally:
            connection.close()


def read_json(status, data):
    if status != 200:
        raise UploadError(f"Creator API returned HTTP {status}; check key access, expiry and target.")
    try:
        return json.loads(data)
    except (ValueError, UnicodeError):
        raise UploadError("Creator API returned invalid JSON.") from None


def verify_release(release, version, digest):
    if (not isinstance(release, dict) or release.get("hudId") != HUD
            or release.get("version") != version or release.get("hash") != digest):
        raise UploadError("Existing or returned version differs from this package; never overwrite it.")
    try:
        uuid.UUID(release["id"])
    except (KeyError, ValueError, TypeError, AttributeError):
        raise UploadError("Creator API returned an invalid release identity.") from None
    if release.get("state") not in ("pending", "approved"):
        raise UploadError("This immutable release is rejected or has an unknown state; review it in sOU.")
    return release


def find_release(api, version, digest):
    releases = read_json(*api.request("GET", TARGET + "/releases"))
    if not isinstance(releases, list):
        raise UploadError("Creator API returned an invalid release list.")
    matches = [item for item in releases if isinstance(item, dict) and item.get("version") == version]
    if len(matches) > 1:
        raise UploadError("Creator API returned duplicate versions.")
    return verify_release(matches[0], version, digest) if matches else None


def upload(directory, tag, api):
    package, version, digest = package_for(directory, tag)
    hud = read_json(*api.request("GET", TARGET))
    if (not isinstance(hud, dict) or hud.get("id") != HUD
            or hud.get("organizationId") != ORGANIZATION or hud.get("packageId") != PACKAGE):
        raise UploadError("Creator HUD/organization/package identity does not match the configured target.")
    existing = find_release(api, version, digest)
    if existing:
        return existing, "already present"
    try:
        created = verify_release(read_json(*api.request("POST", TARGET + "/releases", package)), version, digest)
        if created["state"] != "pending":
            raise UploadError("A new upload must start pending.")
        return created, "uploaded"
    except UploadError:
        # Response loss or HTTP conflict may occur after commit. Read once; never POST again.
        recovered = find_release(api, version, digest)
        if recovered:
            return recovered, "confirmed after uncertain upload response"
        raise


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--tag", required=True)
    parser.add_argument("--directory", default="release-artifacts")
    args = parser.parse_args()
    version_for(args.tag)
    event, ref = os.environ.get("GITHUB_EVENT_NAME"), os.environ.get("GITHUB_REF")
    if os.environ.get("GITHUB_REPOSITORY") != REPOSITORY or not (
            event == "push" and ref == "refs/tags/" + args.tag
            or event == "workflow_dispatch" and ref == "refs/heads/main"):
        raise UploadError("Upload requires this repository's release tag or a manual run on main.")
    token = os.environ.get("SOU_CREATOR_API_KEY", "")
    if not token.startswith("sou_hud_") or any(char.isspace() for char in token):
        raise UploadError("Set the SOU_CREATOR_API_KEY GitHub secret to a read/write Creator key.")
    release, outcome = upload(args.directory, args.tag, CreatorApi(token))
    print(f"sOU {outcome}: {release['version']} / {release['state']} / {release['id']}")


if __name__ == "__main__":
    try:
        main()
    except UploadError as error:
        print(f"Upload failed: {error}", file=sys.stderr)
        sys.exit(1)
    except Exception:
        # Never print request objects, headers, response bodies or raw exception text.
        print("Upload failed: unexpected local error; no automatic retry.", file=sys.stderr)
        sys.exit(1)
