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
