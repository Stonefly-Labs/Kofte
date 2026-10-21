"""Private test-runtime control and transient output; never an HTTP surface."""

import json
import os
from pathlib import Path
import re
import stat
import threading
import time


class RuntimeFailure(Exception):
    def __init__(self, code):
        super().__init__(code)
        self.code = code


class Failures:
    def __init__(self):
        self.code = None
        self.lock = threading.Lock()

    def fail(self, code):
        with self.lock:
            if self.code is None:
                self.code = code

    def check(self):
        with self.lock:
            code = self.code
        if code:
            raise RuntimeFailure(code)


class OutputCapture:
    def __init__(self, failures, forbidden, limit=4 * 1024 * 1024):
        self.failures, self.limit = failures, limit
        self.forbidden = [value.encode() for value in forbidden if value]
        self.buffers = []
        self.size = 0
        self.lock = threading.Lock()
        self.streams = 0

    def read(self, stream):
        try:
            with self.lock:
                self.streams += 1
                buffer = bytearray()
                self.buffers.append(buffer)
            while chunk := stream.read(65536):
                with self.lock:
                    if self.size + len(chunk) > self.limit:
                        self.failures.fail("runtime_capture_limit_exceeded")
                    elif self.failures.code != "runtime_capture_limit_exceeded":
                        buffer.extend(chunk)
                        self.size += len(chunk)
        except Exception:
            self.failures.fail("runtime_capture_failed")
        finally:
            try:
                stream.close()
            except Exception:
                self.failures.fail("runtime_capture_failed")

    def scan_and_clear(self):
        with self.lock:
            try:
                if not self.streams:
                    raise RuntimeFailure("runtime_capture_missing")
                buffers = [bytes(buffer) for buffer in self.buffers]
                buffers.append(b"".join(buffers))
                if any(value in buffer for value in self.forbidden for buffer in buffers):
                    raise RuntimeFailure("runtime_output_disclosed")
                text = "\n".join(buffer.decode(errors="replace") for buffer in buffers)
                if re.search(
                    r"(?i)Unhandled exception|Traceback \(most recent call last\)|"
                    r"\bat [^\n]+\.(?:cs|tsx?|js):(?:line )?\d+|"
                    r"(?:Npgsql|System)\.[A-Za-z0-9_.]*Exception|"
                    r"(?:Password|ConnectionString)\s*[:=]",
                    text,
                ):
                    raise RuntimeFailure("runtime_output_disclosed")
                self.failures.check()
                return {"capturedStreams": self.streams, "capturedBytes": self.size}
            finally:
                for buffer in self.buffers:
                    buffer.clear()
                self.buffers.clear()
                self.size = 0
                self.forbidden.clear()


def connection_password(connection):
    """Npgsql quoted values may contain semicolons and doubled quotes."""
    fields, part, quote, index = [], [], None, 0
    while index < len(connection):
        character = connection[index]
        if quote:
            if character == quote:
                if index + 1 < len(connection) and connection[index + 1] == quote:
                    part.append(character)
                    index += 1
                else:
                    quote = None
            else:
                part.append(character)
        elif character in ("'", '"'):
            quote = character
        elif character == ";":
            fields.append("".join(part))
            part = []
        else:
            part.append(character)
        index += 1
    if quote:
        raise RuntimeFailure("postgres_lease_connection_invalid")
    fields.append("".join(part))
    for field in fields:
        name, separator, value = field.partition("=")
        if separator and name.strip().lower() in ("password", "pwd") and value.strip():
            return value.strip()
    raise RuntimeFailure("postgres_lease_password_missing")


class SqliteAvailability:
    def __init__(self, root):
        self.root = root
        self.database = root / "data/platform/platform.db"
        self.held = root / "held-platform.db"
        self.identity = None

    def _identity(self, path):
        details = path.lstat()
        if (not stat.S_ISREG(details.st_mode) or details.st_uid != os.getuid()
                or path.resolve() != path.absolute() or not path.resolve().is_relative_to(self.root)):
            raise RuntimeFailure("runtime_store_path_unsafe")
        return details.st_dev, details.st_ino

    def unavailable(self):
        if self.identity is not None or self.held.exists() or self.held.is_symlink():
            raise RuntimeFailure("runtime_store_state_invalid")
        identity = self._identity(self.database)
        os.rename(self.database, self.held)
        self.identity = identity
        if self._identity(self.held) != identity:
            raise RuntimeFailure("runtime_store_identity_changed")

    def restore(self):
        if self.identity is None:
            raise RuntimeFailure("runtime_store_state_invalid")
        if (self._identity(self.held) != self.identity or self.database.exists()
                or self.database.is_symlink()):
            raise RuntimeFailure("runtime_cleanup_store_restore_failed")
        os.rename(self.held, self.database)
        if self._identity(self.database) != self.identity:
            raise RuntimeFailure("runtime_cleanup_store_restore_failed")
        self.identity = None

    def cleanup(self):
        if self.identity is not None:
            self.restore()


class ControlWorker:
    def __init__(self, directory, run_id, actions, health, failures):
        self.directory, self.run_id = directory, run_id
        self.actions, self.health, self.failures = actions, health, failures
        self.stopping = threading.Event()
        self.thread = None
        self.observations = []
        details = directory.lstat()
        if (directory.resolve() != directory.absolute() or not stat.S_ISDIR(details.st_mode)
                or details.st_mode & 0o777 != 0o700 or details.st_uid != os.getuid()
                or re.fullmatch(r"[a-f0-9]{32}", run_id) is None):
            raise RuntimeFailure("runtime_control_directory_invalid")

    def consume(self, path):
        match = re.fullmatch(r"request-([a-f0-9]{32})\.json", path.name)
        if match is None:
            raise RuntimeFailure("runtime_control_request_invalid")
        details = path.lstat()
        if (not stat.S_ISREG(details.st_mode) or details.st_uid != os.getuid()
                or details.st_nlink != 1 or details.st_size > 1024):
            raise RuntimeFailure("runtime_control_request_invalid")
        request = json.loads(path.read_text())
        nonce = match[1]
        if (not isinstance(request, dict) or set(request) != {"runId", "nonce", "action"}
                or request["runId"] != self.run_id or request["nonce"] != nonce):
            raise RuntimeFailure("runtime_control_request_invalid")
        response = {"runId": self.run_id, "nonce": nonce, "ok": True}
        began = time.monotonic()
        supported = isinstance(request["action"], str) and request["action"] in self.actions
        try:
            if not supported:
                raise RuntimeFailure("runtime_control_action_unsupported")
            self.actions[request["action"]]()
        except Exception:
            response.update(ok=False, errorCode="runtime_control_action_failed")
            self.failures.fail("runtime_control_action_failed")
        current = path.lstat()
        if (details.st_dev, details.st_ino) != (current.st_dev, current.st_ino):
            raise RuntimeFailure("runtime_control_request_changed")
        temporary = self.directory / f".response-{nonce}.tmp"
        final = self.directory / f"response-{nonce}.json"
        if final.exists() or final.is_symlink():
            raise RuntimeFailure("runtime_control_response_preexists")
        with os.fdopen(os.open(
            temporary, os.O_WRONLY | os.O_CREAT | os.O_EXCL | getattr(os, "O_NOFOLLOW", 0), 0o600,
        ), "w") as stream:
            json.dump(response, stream)
        path.unlink()
        os.rename(temporary, final)
        self.observations.append({
            "action": request["action"] if supported else "unsupported",
            "ok": response["ok"],
            "durationSeconds": round(time.monotonic() - began, 3),
        })

    def _work(self):
        try:
            while not self.stopping.is_set():
                self.health()
                for path in sorted(self.directory.glob("request-*.json")):
                    self.consume(path)
                    self.failures.check()
                self.stopping.wait(0.025)
        except Exception:
            self.failures.fail("runtime_control_worker_failed")

    def start(self):
        self.thread = threading.Thread(target=self._work, daemon=True)
        self.thread.start()

    def check(self):
        self.failures.check()
        if self.thread is not None and not self.thread.is_alive() and not self.stopping.is_set():
            raise RuntimeFailure("runtime_control_worker_stopped")

    def stop(self):
        self.stopping.set()
        if self.thread is not None:
            self.thread.join(timeout=20)
            if self.thread.is_alive():
                raise RuntimeFailure("runtime_cleanup_worker_timeout")


class Session:
    def __init__(self, environment, directory, worker):
        self.environment, self.directory, self.worker = environment, directory, worker

    def check(self):
        self.worker.check()
