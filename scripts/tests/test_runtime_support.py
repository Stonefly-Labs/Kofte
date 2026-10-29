import io
import json
import os
from pathlib import Path
import subprocess
import tempfile
import time
import unittest
import uuid

from scripts import runtime_support as support
from scripts import verify


class RuntimeSupportTests(unittest.TestCase):
    def setUp(self):
        base = verify.ROOT / "artifacts/python-tests"
        base.mkdir(parents=True, exist_ok=True)
        self.temporary = tempfile.TemporaryDirectory(dir=base)
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name).resolve()
        os.chmod(self.root, 0o700)
        self.run_id = uuid.uuid4().hex
        self.failures = support.Failures()

    def request(self, action, run_id=None):
        nonce = uuid.uuid4().hex
        temporary = self.root / f".request-{nonce}.tmp"
        temporary.write_text(json.dumps({
            "runId": run_id or self.run_id, "nonce": nonce, "action": action,
        }))
        request = self.root / f"request-{nonce}.json"
        temporary.rename(request)
        return request, self.root / f"response-{nonce}.json"

    def worker(self, actions, health=lambda: None):
        return support.ControlWorker(self.root, self.run_id, actions, health, self.failures)

    def wait_for(self, predicate):
        deadline = time.monotonic() + 2
        while not predicate() and time.monotonic() < deadline:
            time.sleep(0.01)
        self.assertTrue(predicate())

    def test_atomic_owned_request_is_consumed_and_only_a_safe_response_is_written(self):
        calls = []
        worker = self.worker({"stop-host": lambda: calls.append("stopped")})
        worker.start()
        try:
            request, response = self.request("stop-host")
            self.wait_for(response.exists)
            worker.check()
            self.assertFalse(request.exists())
            value = json.loads(response.read_text())
            self.assertEqual({"runId", "nonce", "ok"}, set(value))
            self.assertEqual(self.run_id, value["runId"])
            self.assertTrue(value["ok"])
            self.assertEqual(["stopped"], calls)
            self.assertEqual(0o600, response.stat().st_mode & 0o777)
        finally:
            worker.stop()

    def test_foreign_run_is_never_consumed_or_executed(self):
        calls = []
        worker = self.worker({"stop-host": lambda: calls.append("stopped")})
        request, response = self.request("stop-host", uuid.uuid4().hex)
        with self.assertRaisesRegex(support.RuntimeFailure, "runtime_control_request_invalid"):
            worker.consume(request)
        self.assertTrue(request.exists())
        self.assertFalse(response.exists())
        self.assertEqual([], calls)

    def test_worker_action_error_is_acknowledged_safely_and_fails_the_suite(self):
        def fail():
            raise OSError("credential-canary /private/layout")
        worker = self.worker({"start-host": fail})
        worker.start()
        try:
            _, response = self.request("start-host")
            self.wait_for(response.exists)
            value = json.loads(response.read_text())
            self.assertFalse(value["ok"])
            self.assertEqual("runtime_control_action_failed", value["errorCode"])
            self.assertNotIn("credential-canary", response.read_text())
            with self.assertRaises(support.RuntimeFailure):
                worker.check()
        finally:
            worker.stop()

    def test_background_health_exception_cannot_turn_into_success(self):
        def health():
            raise RuntimeError("private child diagnostics")
        worker = self.worker({}, health)
        worker.start()
        try:
            self.wait_for(lambda: self.failures.code is not None)
            with self.assertRaisesRegex(support.RuntimeFailure, "runtime_control_worker_failed"):
                worker.check()
        finally:
            worker.stop()

    def test_control_refuses_paths_in_requests_and_symlinked_input(self):
        worker = self.worker({"stop-host": lambda: self.fail("must not execute")})
        request, _ = self.request("stop-host")
        data = json.loads(request.read_text())
        data["path"] = "/private/operator-data"
        request.write_text(json.dumps(data))
        with self.assertRaises(support.RuntimeFailure):
            worker.consume(request)
        request.unlink()
        outside = self.root / "sentinel"
        outside.write_text("unchanged")
        request.symlink_to(outside)
        with self.assertRaises(support.RuntimeFailure):
            worker.consume(request)
        self.assertEqual("unchanged", outside.read_text())

    def test_store_outage_and_cleanup_preserve_the_exact_original_file(self):
        store = support.SqliteAvailability(self.root)
        store.database.parent.mkdir(parents=True)
        store.database.write_bytes(b"private fixture")
        identity = store.database.stat().st_ino
        store.unavailable()
        self.assertFalse(store.database.exists())
        self.assertEqual(b"private fixture", store.held.read_bytes())
        store.cleanup()
        self.assertEqual(identity, store.database.stat().st_ino)
        self.assertEqual(b"private fixture", store.database.read_bytes())
        self.assertFalse(store.held.exists())

    def test_store_restore_never_overwrites_an_unexpected_destination(self):
        store = support.SqliteAvailability(self.root)
        store.database.parent.mkdir(parents=True)
        store.database.write_bytes(b"original")
        store.unavailable()
        store.database.write_bytes(b"unexpected sentinel")
        with self.assertRaisesRegex(support.RuntimeFailure, "runtime_cleanup_store_restore_failed"):
            store.cleanup()
        self.assertEqual(b"unexpected sentinel", store.database.read_bytes())
        self.assertEqual(b"original", store.held.read_bytes())

    def test_runtime_capture_aggregates_restarts_and_clears_after_disclosure_failure(self):
        capture = support.OutputCapture(self.failures, ["credential-canary"])
        capture.read(io.BytesIO(b"credential-canary"))
        capture.read(io.BytesIO(b"new host is healthy"))
        with self.assertRaisesRegex(support.RuntimeFailure, "runtime_output_disclosed"):
            capture.scan_and_clear()
        self.assertEqual([], capture.buffers)
        self.assertEqual([], capture.forbidden)
        self.assertEqual(0, capture.size)

    def test_interleaved_streams_cannot_hide_a_secret_split_across_chunks(self):
        capture = support.OutputCapture(self.failures, ["credential-canary"])

        class Interleaved(io.BytesIO):
            def __init__(self):
                super().__init__()
                self.parts = iter((b"credential-", b"canary", b""))

            def read(self, *_args):
                part = next(self.parts)
                if part == b"canary":
                    capture.read(io.BytesIO(b"another stream"))
                return part

        capture.read(Interleaved())
        with self.assertRaisesRegex(support.RuntimeFailure, "runtime_output_disclosed"):
            capture.scan_and_clear()
        self.assertEqual([], capture.buffers)

    def test_browser_canaries_are_consumed_once_and_scan_actual_prior_transport_output(self):
        values = ["session-canary-123456", "csrf-canary-123456789", "browser-canary-+/=123456"]
        path = self.root / "identity-browser-canaries.json"
        for disclosed in (values[0].encode(), b"browser-canary-%2B%2F%3D123456"):
            with self.subTest(disclosed=disclosed):
                capture = support.OutputCapture(self.failures, [])
                capture.read(io.BytesIO(disclosed))
                path.write_text(json.dumps({"version": 1, "canaries": values}))
                path.chmod(0o600)
                self.assertEqual({"identityCanaries": 3}, capture.consume_identity_canaries(self.root))
                self.assertFalse(path.exists())
                with self.assertRaisesRegex(support.RuntimeFailure, "identity_browser_transport_canaries_invalid"):
                    capture.consume_identity_canaries(self.root)
                with self.assertRaisesRegex(support.RuntimeFailure, "runtime_output_disclosed"):
                    capture.scan_and_clear()
                self.assertEqual([], capture.forbidden)

    def test_browser_canary_receipt_refuses_missing_malformed_duplicate_and_unbounded_input(self):
        capture = support.OutputCapture(self.failures, [])
        path = self.root / "identity-browser-canaries.json"
        valid = {"version": 1, "canaries": ["a" * 16, "b" * 16, "c" * 16]}
        cases = [
            None, b"", b"x" * 65537, b"not-json", b"\xff",
            json.dumps(valid).encode("utf-16"),
            b"[" * 2000 + b"]" * 2000,
            b'{"version":1,"version":1,"canaries":["aaaaaaaaaaaaaaaa","bbbbbbbbbbbbbbbb","cccccccccccccccc"]}',
            json.dumps(valid | {"version": True}).encode(),
            json.dumps(valid | {"canaries": ["a" * 16] * 3}).encode(),
            json.dumps(valid | {"canaries": ["a" * 16, "b" * 16, "short"]}).encode(),
            json.dumps(valid | {"canaries": ["a" * 16, "b" * 16, None]}).encode(),
            json.dumps(valid | {"extra": "not-admitted"}).encode(),
        ]
        for content in cases:
            with self.subTest(content=content), self.assertRaisesRegex(
                    support.RuntimeFailure, "identity_browser_transport_canaries_invalid"):
                if content is not None:
                    path.write_bytes(content)
                    path.chmod(0o600)
                capture.consume_identity_canaries(self.root)
            self.assertEqual([], capture.forbidden)

    def test_browser_canary_receipt_refuses_public_permissions_links_and_special_files(self):
        capture = support.OutputCapture(self.failures, [])
        target = self.root / "private-canaries.json"
        target.write_text(json.dumps({"version": 1, "canaries": ["a" * 16, "b" * 16, "c" * 16]}))
        target.chmod(0o600)
        path = self.root / "identity-browser-canaries.json"
        for kind in ("symlink", "hardlink", "fifo", "public-file", "public-root"):
            with self.subTest(kind=kind):
                if kind == "symlink":
                    path.symlink_to(target)
                elif kind == "hardlink":
                    os.link(target, path)
                elif kind == "fifo":
                    os.mkfifo(path, 0o600)
                else:
                    path.write_bytes(target.read_bytes())
                    path.chmod(0o644 if kind == "public-file" else 0o600)
                    if kind == "public-root":
                        self.root.chmod(0o755)
                try:
                    with self.assertRaisesRegex(support.RuntimeFailure, "identity_browser_transport_canaries_invalid"):
                        capture.consume_identity_canaries(self.root)
                finally:
                    self.root.chmod(0o700)
                    path.unlink()
                self.assertEqual([], capture.forbidden)

    def test_diagnostic_locations_split_across_chunks_are_complete_and_never_include_raw_text(self):
        diagnostic = support.CommandDiagnostics("browser")

        class Pieces(io.BytesIO):
            def __init__(self):
                super().__init__()
                self.parts = iter((
                    b" at /private/tests/e2e/accessibility.spec.ts:7",
                    b"6:1", b"5 credential-canary\n at /private/tests/e2e/startup.spec.ts:10:2", b"",
                ))

            def read(self, *_args):
                return next(self.parts)

        diagnostic.read(Pieces())
        self.assertEqual({
            "sourceLocations": ["accessibility.spec.ts:76:15", "startup.spec.ts:10:2"],
        }, diagnostic.safe_fields())
        self.assertNotIn("credential", json.dumps(vars(diagnostic), default=str))
        diagnostic.clear()
        self.assertEqual({}, diagnostic.safe_fields())

    def test_browser_diagnostics_do_not_blame_passed_test_declarations_for_identity_failure(self):
        diagnostic = support.CommandDiagnostics("browser")
        diagnostic.read(io.BytesIO(
            b"  \xe2\x9c\x93 1 bootstrap.spec.ts:3:1 public test (1s)\n"
            b"  \xe2\x9c\x93 2 recovery.spec.ts:4:1 public test (1s)\n"
            b"  1) identity.spec.ts:99:1 failed test\n"
            b"Error: credential-canary\n"
        ))
        self.assertEqual({"sourceLocations": ["identity.spec.ts:99:1"]}, diagnostic.safe_fields())
        self.assertNotIn("credential", json.dumps(vars(diagnostic), default=str))

    def test_storage_diagnostics_keep_only_known_symbols_and_codes_not_arguments_or_exceptions(self):
        name = "Kofte.Storage.IntegrationTests.Registry.PostgresRegistrationConcurrencyTests.KnownCase"
        diagnostic = support.CommandDiagnostics("storage", ["Busy", "storage_busy"], [name])
        diagnostic.read(io.BytesIO((
            f"failed {name}(owner: \"credential-canary\") (3s)\n"
            "failed Kofte.Storage.IntegrationTests.Registry.credential_canary.Unknown (1s)\n"
            "StorageException : Busy\nXunitException : storage_busy\nStorageException : credentialCanary\n"
            "/private/credential-canary Exception stack and arbitrary provider data\n"
        ).encode()))
        self.assertEqual({
            "failedStorageTests": [name], "storageFailureCodes": ["Busy", "storage_busy"],
        }, diagnostic.safe_fields())
        self.assertNotIn("credential", json.dumps(vars(diagnostic), default=str))
        diagnostic.clear()
        self.assertEqual({}, diagnostic.safe_fields())

    def test_storage_report_failures_are_bounded_and_explicit_without_retaining_content(self):
        path = self.root / "storage.xml"
        for content, code in ((None, "unavailable"), (b"credential-canary", "invalid"),
                              (b"x" * (16 * 1024 * 1024 + 1), "too_large"),
                              (b"<assemblies/>", "no_known_failure")):
            with self.subTest(code=code):
                if content is not None:
                    path.write_bytes(content)
                diagnostic = support.CommandDiagnostics("storage")
                diagnostic.read_storage_report(path)
                self.assertEqual({"storageDiagnosticCode": code}, diagnostic.safe_fields())
                self.assertNotIn("credential", json.dumps(vars(diagnostic), default=str))
                diagnostic.clear()
                self.assertEqual({}, diagnostic.safe_fields())

    def test_native_failure_locations_require_known_failed_tests_and_bounded_source_lines(self):
        name = "Kofte.Host.IntegrationTests.Identity.SqliteKnownTests.KnownCase"
        owner, method = name.rsplit(".", 1)
        source = "tests/integration/Kofte.Host.IntegrationTests/Identity/KnownTests.cs"
        diagnostic = support.CommandDiagnostics("host", tests=[name], source_lines={source: 100})
        path = self.root / "storage.xml"
        locations = "\n".join(f"/private/credential-canary/{source}:line {line}" for line in range(1, 41))
        invalid = "\n".join((
            f"/private/credential-canary/{source}:line 0",
            f"/private/credential-canary/{source}:line 101",
            f"/private/credential-canary/{source}:line 000042",
            "/private/credential-canary/Unknown.cs:line 42",
        ))
        path.write_text(
            f'<assemblies><test type="{owner}" method="{method}" result="Pass">'
            f'<failure><stack-trace>{source}:line 99</stack-trace></failure></test>'
            f'<test type="unknown" method="unknown" result="Fail">'
            f'<failure><stack-trace>{source}:line 98</stack-trace></failure></test>'
            f'<test type="{owner}" method="{method}" result="Fail">'
            '<failure><message>credential-canary</message><stack-trace>'
            + invalid + "\n" + locations + '</stack-trace></failure></test></assemblies>'
        )
        diagnostic.read_storage_report(path)
        self.assertEqual({
            "failedHostTests": [name],
            "hostFailureLocations": sorted(f"{source}:{line}" for line in range(1, 33)),
        }, diagnostic.safe_fields())
        self.assertNotIn("credential", json.dumps(vars(diagnostic), default=str))
        diagnostic.clear()
        self.assertEqual({}, diagnostic.safe_fields())

    def test_host_status_assertions_retain_only_finite_values_from_known_failed_tests(self):
        name = "Kofte.Host.IntegrationTests.Identity.SqliteKnownTests.KnownCase"
        owner, method = name.rsplit(".", 1)
        path = self.root / "storage.xml"
        message = "Assert.Equal() Failure: Values differ\nExpected: 303\nActual:   503"
        symbolic = "Assert.Equal() Failure: Values differ\nExpected: Unauthorized\nActual:   ServiceUnavailable"
        headers = symbolic.replace("Unauthorized", "RequestHeaderFieldsTooLarge")
        def case(result, text, selected_owner=owner):
            return (f'<test type="{selected_owner}" method="{method}" result="{result}">'
                    '<failure><message><![CDATA[' + text + ']]></message></failure></test>')
        path.write_text("<assemblies>" + "".join((
            case("Fail", message), case("Fail", symbolic), case("Fail", headers),
            case("Pass", message.replace("503", "400")),
            case("Fail", message.replace("503", "409"), "unknown"),
            case("Fail", message.replace("503", "123")),
            case("Fail", message.replace("503", "credential-canary")),
            case("Fail", "credential-canary\n" + message),
            case("Fail", message + "\n/private/credential-canary"),
        )) + "</assemblies>")
        diagnostic = support.CommandDiagnostics("host", tests=[name])
        diagnostic.read_storage_report(path)
        self.assertEqual({
            "failedHostTests": [name],
            "hostFailureStatuses": [
                "expected_303_actual_503", "expected_401_actual_503", "expected_431_actual_503",
            ],
        }, diagnostic.safe_fields())
        self.assertNotIn("credential", json.dumps(vars(diagnostic), default=str))
        diagnostic.clear()
        self.assertEqual({}, diagnostic.safe_fields())

    def test_failed_composition_output_retains_only_known_scenario_and_assertion_symbols(self):
        name = "Kofte.Storage.IntegrationTests.Composition.SqliteCompositionProviderTests.KnownCase"
        owner, method = name.rsplit(".", 1)
        codes = ["composition_consumer_failed", "composition_test_command_exit_expected_",
                 "composition_cleanup_incomplete", "composition_host_dispose_incomplete"]
        diagnostic = support.CommandDiagnostics(
            "storage", codes, [name], scenarios=["empty-boot", "valid-dependency", "disabled-missing"],
        )
        event = {
            "schemaVersion": 1, "eventCode": "composition_test_command_exit_expected_0_actual_1_"
            "composition_cleanup_incomplete_composition_host_dispose_incomplete",
            "attempt": "a" * 32, "provider": "sqlite", "scenario": "empty-boot",
            "status": "failed", "assertions": 0,
        }
        output = "\n".join((
            json.dumps(event | {"scenario": "credential-canary"}),
            json.dumps(event | {"eventCode": "credential-canary", "extra": "/private/credential-canary"}),
            json.dumps(event),
            "/private/credential-canary arbitrary exception and stack",
        ))
        path = self.root / "storage.xml"
        path.write_text(
            f'<assemblies><test type="{owner}" method="{method}" result="Pass">'
            '<output><![CDATA[' + json.dumps(event | {"scenario": "valid-dependency"}) + ']]></output></test>'
            '<test type="unknown" method="unknown" result="Fail"><output><![CDATA['
            + json.dumps(event | {"scenario": "disabled-missing"}) + ']]></output></test>'
            f'<test type="{owner}" method="{method}" result="Fail">'
            '<failure><message>composition_consumer_failed</message></failure>'
            '<output><![CDATA[' + output + ']]></output></test></assemblies>'
        )
        diagnostic.read_storage_report(path)
        self.assertEqual({
            "failedStorageTests": [name],
            "storageFailureCodes": sorted(codes),
            "storageFailureScenarios": ["empty-boot"],
        }, diagnostic.safe_fields())
        self.assertNotIn("credential", json.dumps(vars(diagnostic), default=str))
        diagnostic.clear()
        self.assertEqual({}, diagnostic.safe_fields())

        codes = [f"composition_test_case_{chr(97 + index // 26)}{chr(97 + index % 26)}" for index in range(40)]
        diagnostic = support.CommandDiagnostics("storage", codes, [name])
        path.write_text(
            f'<assemblies><test type="{owner}" method="{method}" result="Fail">'
            '<failure><message>' + "\n".join(codes) + '</message></failure></test></assemblies>'
        )
        diagnostic.read_storage_report(path)
        self.assertEqual(codes[:32], diagnostic.safe_fields()["storageFailureCodes"])

    def test_runtime_capture_overflow_and_reader_failure_fail_closed(self):
        capture = support.OutputCapture(self.failures, [], limit=3)
        capture.read(io.BytesIO(b"oversized output"))
        with self.assertRaisesRegex(support.RuntimeFailure, "runtime_capture_limit_exceeded"):
            capture.scan_and_clear()
        self.assertEqual(0, capture.size)
        class Broken(io.BytesIO):
            def read(self, *_):
                raise OSError("credential-canary")
        failures = support.Failures()
        capture = support.OutputCapture(failures, [])
        capture.read(Broken())
        with self.assertRaisesRegex(support.RuntimeFailure, "runtime_capture_failed"):
            capture.scan_and_clear()

    def test_quoted_postgres_password_is_scanned_without_logging_the_connection(self):
        connection = 'Host=127.0.0.1;Password="private;value""quoted";Username=test'
        password = support.connection_password(connection)
        self.assertEqual('private;value"quoted', password)
        capture = support.OutputCapture(self.failures, [connection, password])
        capture.read(io.BytesIO(password.encode()))
        with self.assertRaisesRegex(support.RuntimeFailure, "runtime_output_disclosed"):
            capture.scan_and_clear()

    def test_focused_python_runner_refuses_an_empty_or_skipped_capture_suite(self):
        command = [
            "python3", str(verify.ROOT / "scripts/run_python_tests.py"),
            "--directory", str(self.root), "--pattern", "test_probe.py",
        ]
        for content in (
            None,
            "import unittest\n@unittest.skip('unavailable')\nclass Probe(unittest.TestCase):\n"
            "    def test_missing_capture(self):\n        pass\n",
        ):
            if content:
                (self.root / "test_probe.py").write_text(content)
            result = subprocess.run(command, cwd=verify.ROOT, capture_output=True, timeout=10)
            self.assertNotEqual(0, result.returncode)


if __name__ == "__main__":
    unittest.main()
