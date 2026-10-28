"""Offline regression tests; no real keys, accounts or network requests."""
import hashlib
import importlib.util
import json
import os
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch
import zipfile

SPEC = importlib.util.spec_from_file_location("upload_sou", Path(__file__).parents[2] / "scripts/upload_sou.py")
uploader = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(uploader)


class FakeApi:
    def __init__(self, responses):
        self.responses = iter(responses)
        self.calls = []

    def request(self, method, path, package=None):
        self.calls.append((method, path, package))
        response = next(self.responses)
        if isinstance(response, Exception):
            raise response
        status, body = response
        return status, json.dumps(body).encode()


class UploadTests(unittest.TestCase):
    def setUp(self):
        self.scratch = tempfile.TemporaryDirectory()
        self.addCleanup(self.scratch.cleanup)
        self.directory = Path(self.scratch.name)
        self.manifest = {"id": uploader.PACKAGE, "version": "0.4.9", "apiVersion": 2,
                         "runtime": "blazor-wasm", "entryPoint": "index.html"}
        self.package = self.directory / "sou-basic-wasm-0.4.9.souhud"
        self.write_package()
        self.hud = {"id": uploader.HUD, "organizationId": uploader.ORGANIZATION,
                    "packageId": uploader.PACKAGE}
        self.release = {"id": "11111111-1111-4111-8111-111111111111", "hudId": uploader.HUD,
                        "version": "0.4.9", "hash": self.digest, "state": "pending"}

    def write_package(self):
        with zipfile.ZipFile(self.package, "w") as archive:
            archive.writestr("hud.json", json.dumps(self.manifest))
            archive.writestr("wwwroot/index.html", "Synthetic test")
        self.digest = hashlib.sha256(self.package.read_bytes()).hexdigest()
        self.package.with_suffix(".souhud.sha256").write_text(f"{self.digest}  {self.package.name}\n", "utf-8")

    def run_upload(self, responses):
        api = FakeApi(responses)
        result = uploader.upload(self.directory, "v0.4.9", api)
        return result, api.calls

    def test_new_upload_uses_existing_hud_and_exact_archive(self):
        (release, outcome), calls = self.run_upload([(200, self.hud), (200, []), (200, self.release)])
        self.assertEqual((release, outcome), (self.release, "uploaded"))
        self.assertEqual(calls, [("GET", uploader.TARGET, None),
                                ("GET", uploader.TARGET + "/releases", None),
                                ("POST", uploader.TARGET + "/releases", self.package)])

    def test_matching_pending_or_approved_version_never_posts(self):
        for state in ("pending", "approved"):
            with self.subTest(state=state):
                release = {**self.release, "state": state}
                (_, outcome), calls = self.run_upload([(200, self.hud), (200, [release])])
                self.assertEqual(outcome, "already present")
                self.assertEqual([call[0] for call in calls], ["GET", "GET"])

    def test_different_hash_rejected_before_any_upload(self):
        api = FakeApi([(200, self.hud), (200, [{**self.release, "hash": "0" * 64}])])
        with self.assertRaisesRegex(uploader.UploadError, "never overwrite"):
            uploader.upload(self.directory, "v0.4.9", api)
        self.assertFalse(any(call[0] == "POST" for call in api.calls))

    def test_foreign_hud_organization_or_package_cannot_receive_upload(self):
        for field in ("id", "organizationId", "packageId"):
            with self.subTest(field=field):
                api = FakeApi([(200, {**self.hud, field: "foreign"})])
                with self.assertRaises(uploader.UploadError):
                    uploader.upload(self.directory, "v0.4.9", api)
                self.assertEqual(len(api.calls), 1)

    def test_response_loss_or_conflict_reads_once_without_second_post(self):
        for response in (uploader.UploadError("Response lost"), (409, {}), (500, {})):
            with self.subTest(response=type(response).__name__):
                (_, outcome), calls = self.run_upload([(200, self.hud), (200, []), response, (200, [self.release])])
                self.assertEqual(outcome, "confirmed after uncertain upload response")
                self.assertEqual([call[0] for call in calls], ["GET", "GET", "POST", "GET"])

    def test_unconfirmed_failure_does_not_repeat_upload(self):
        api = FakeApi([(200, self.hud), (200, []), (403, {}), (200, [])])
        with self.assertRaisesRegex(uploader.UploadError, "HTTP 403"):
            uploader.upload(self.directory, "v0.4.9", api)
        self.assertEqual(sum(call[0] == "POST" for call in api.calls), 1)

    def test_authentication_and_rate_errors_do_not_expose_response_body(self):
        for status in (401, 403, 404, 429, 500):
            with self.subTest(status=status):
                with self.assertRaises(uploader.UploadError) as error:
                    self.run_upload([(status, {"sensitive": "never print response text"})])
                self.assertNotIn("never print", str(error.exception))
                self.assertIn(str(status), str(error.exception))

    def test_rejected_release_is_not_silently_reported_as_success(self):
        with self.assertRaisesRegex(uploader.UploadError, "rejected"):
            self.run_upload([(200, self.hud), (200, [{**self.release, "state": "rejected"}])])

    def test_bad_tag_or_checksum_never_contacts_api(self):
        for tag in ("main", "v01.4.9", "v0.4.9; echo unsafe", "v0.4.9-preview.1"):
            with self.subTest(tag=tag), self.assertRaises(uploader.UploadError):
                uploader.upload(self.directory, tag, FakeApi([]))
        self.package.write_bytes(self.package.read_bytes() + b"different")
        with self.assertRaisesRegex(uploader.UploadError, "SHA-256"):
            uploader.upload(self.directory, "v0.4.9", FakeApi([]))

    def test_wrong_manifest_identity_version_or_runtime_never_contacts_api(self):
        for key, value in (("id", "other"), ("version", "0.4.8"), ("runtime", "web"), ("apiVersion", 1)):
            with self.subTest(key=key):
                original = self.manifest[key]
                self.manifest[key] = value
                self.write_package()
                with self.assertRaisesRegex(uploader.UploadError, "does not match"):
                    uploader.upload(self.directory, "v0.4.9", FakeApi([]))
                self.manifest[key] = original

    def test_supported_protocol_versions_preserve_historical_uploads(self):
        for version in (2, 3):
            with self.subTest(version=version):
                self.manifest["apiVersion"] = version
                self.write_package()
                self.assertEqual(uploader.package_for(self.directory, "v0.4.9")[1], "0.4.9")
        for version in (True, 2.0, "3", 4, None):
            with self.subTest(version=version):
                self.manifest["apiVersion"] = version
                self.write_package()
                with self.assertRaises(uploader.UploadError):
                    uploader.package_for(self.directory, "v0.4.9")

    def test_transport_uses_fixed_https_and_raw_octet_stream_without_redirects(self):
        with patch.object(uploader.http.client, "HTTPSConnection") as factory:
            connection = factory.return_value
            connection.getresponse.return_value.status = 302
            connection.getresponse.return_value.read.return_value = b""
            status, _ = uploader.CreatorApi("sou_hud_synthetic_not_a_real_key").request("POST", uploader.TARGET + "/releases", self.package)
            factory.assert_called_once_with("www.getsou.app", timeout=120)
            args, kwargs = connection.request.call_args
            self.assertEqual(args, ("POST", uploader.TARGET + "/releases"))
            self.assertEqual(kwargs["headers"]["Content-Type"], "application/octet-stream")
            self.assertEqual(kwargs["headers"]["Content-Length"], str(self.package.stat().st_size))
            self.assertEqual(kwargs["body"].name, str(self.package))
            self.assertEqual(status, 302)
            connection.request.assert_called_once()
            connection.close.assert_called_once()

    def test_main_rejects_untrusted_context_before_using_key(self):
        with patch.dict(os.environ, {"GITHUB_REPOSITORY": "foreign/fork", "GITHUB_EVENT_NAME": "pull_request"}, clear=True):
            with patch("sys.argv", ["upload_sou.py", "--tag", "v0.4.9"]), self.assertRaisesRegex(uploader.UploadError, "requires this repository"):
                uploader.main()

    def test_missing_secret_fails_before_any_network_request(self):
        with patch.dict(os.environ, {"GITHUB_REPOSITORY": uploader.REPOSITORY,
                                     "GITHUB_EVENT_NAME": "workflow_dispatch",
                                     "GITHUB_REF": "refs/heads/main"}, clear=True):
            with patch("sys.argv", ["upload_sou.py", "--tag", "v0.4.9"]):
                with patch.object(uploader.CreatorApi, "request") as request:
                    with self.assertRaisesRegex(uploader.UploadError, "SOU_CREATOR_API_KEY"):
                        uploader.main()
                    request.assert_not_called()


if __name__ == "__main__":
    unittest.main()
