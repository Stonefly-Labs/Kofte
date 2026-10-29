"""Private test-runtime control and transient output; never an HTTP surface."""

import json
import os
from pathlib import Path
import re
import stat
import threading
import time
from urllib.parse import quote
import xml.etree.ElementTree as ET


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


class CommandDiagnostics:
    storage_report_codes = frozenset(("unavailable", "invalid", "too_large", "no_known_failure"))
    storage_operations = frozenset(("acquire", "register", "setup"))
    http_statuses = {
        "OK": "200", "NoContent": "204", "MovedPermanently": "301", "Found": "302", "SeeOther": "303",
        "TemporaryRedirect": "307", "PermanentRedirect": "308",
        "BadRequest": "400", "Unauthorized": "401", "Forbidden": "403", "NotFound": "404",
        "MethodNotAllowed": "405", "Conflict": "409", "RequestEntityTooLarge": "413",
        "TooManyRequests": "429", "RequestHeaderFieldsTooLarge": "431",
        "InternalServerError": "500", "BadGateway": "502", "ServiceUnavailable": "503", "GatewayTimeout": "504",
    }
    http_assertion = re.compile(
        r"Assert\.Equal\(\) Failure: Values differ\r?\n"
        r"Expected: *([A-Za-z]{2,32}|[0-9]{3})\r?\n"
        r"Actual: *([A-Za-z]{2,32}|[0-9]{3})\s*",
    )
    locations = re.compile(
        rb"(?<![A-Za-z0-9_.-])("
        rb"accessibility\.spec\.ts|bootstrap\.spec\.ts|recovery\.spec\.ts|"
        rb"privacy\.spec\.ts|startup\.spec\.ts|identity\.spec\.ts):"
        rb"([1-9][0-9]{0,8}):([1-9][0-9]{0,8})(?![0-9])",
    )

    storage_test = re.compile(
        rb"^\s*failed (Kofte\.(?:Host\.IntegrationTests|Storage\.IntegrationTests|Platform\.ContractTests)\."
        rb"[A-Za-z0-9_.+]{1,240})(?=[( ])",
    )
    completed_contract = re.compile(
        rb"^\s*passed (Kofte\.Platform\.ContractTests\.[A-Za-z0-9_.+]{1,240})(?=[( ])",
    )
    storage_code = re.compile(rb"\b(?:StorageException|XunitException)\s*:\s*([A-Za-z_]{1,64})\b")
    native_location = re.compile(
        r"(?<![A-Za-z0-9_.-])(tests/(?:[A-Za-z0-9_.-]+/)*[A-Za-z0-9_.-]+\.cs)"
        r":line ([1-9][0-9]{0,5})(?![0-9])",
    )
    browser_phase = re.compile(
        r"(?m)^[ \t]*(identity_browser_profile_[a-z_]{1,100}):([12]):"
        r"(0|[1-9][0-9]{0,4})[ \t]*\r?$",
    )

    def __init__(self, kind, codes=(), tests=(), *, scenarios=(), source_lines=None):
        self.kind, self.codes = kind, frozenset(codes)
        self.tests = frozenset(tests)
        self.scenarios = frozenset(scenarios)
        self.source_lines = dict(source_lines or {})
        self.failures = Failures()
        self.sources, self.dependency_codes = set(), set()
        self.failed_tests, self.storage_codes = set(), set()
        self.completed_methods = set()
        self.failed_operations = set()
        self.failed_scenarios = set()
        self.failed_statuses = set()
        self.browser_phases = []
        self.storage_report_code = None
        self.lock = threading.Lock()
        self.closed = False

    def inspect(self, line):
        if self.kind == "consumer":
            try:
                self.read_composition_output(line.decode("utf-8"))
            except UnicodeError:
                return
        elif self.kind == "browser":
            if not re.match(rb"^\s*(?:[1-9][0-9]*\)|at\b)", line):
                return
            found = [b":".join(match.groups()).decode("ascii")
                     for match in self.locations.finditer(line)]
            with self.lock:
                if not self.closed:
                    for location in found:
                        if len(self.sources) < 32:
                            self.sources.add(location)
        elif self.kind == "dependency":
            try:
                event = json.loads(line.decode("utf-8"))
            except (UnicodeError, ValueError):
                return
            if (isinstance(event, dict) and set(event) == {"eventCode"}
                    and isinstance(event["eventCode"], str) and event["eventCode"] in self.codes):
                with self.lock:
                    if not self.closed and len(self.dependency_codes) < 2:
                        self.dependency_codes.add(event["eventCode"])
        elif self.kind in ("host", "storage", "contract"):
            test = self.storage_test.match(line)
            code = self.storage_code.search(line)
            completed = self.completed_contract.match(line) if self.kind == "contract" else None
            with self.lock:
                if self.closed:
                    return
                if completed and completed[1].decode("ascii") in self.tests and len(self.completed_methods) < 256:
                    self.completed_methods.add(completed[1].decode("ascii"))
                if test and test[1].decode("ascii") in self.tests and len(self.failed_tests) < 32:
                    self.failed_tests.add(test[1].decode("ascii"))
                if code and code[1].decode("ascii") in self.codes and len(self.storage_codes) < 32:
                    self.storage_codes.add(code[1].decode("ascii"))

    def read(self, stream):
        pending = b""
        try:
            while chunk := stream.read(4096):
                pending += chunk
                while b"\n" in pending:
                    line, pending = pending.split(b"\n", 1)
                    self.inspect(line[-8192:])
                pending = pending[-8192:]
            self.inspect(pending)
        except Exception:
            self.failures.fail("command_diagnostic_capture_failed")
        finally:
            pending = b""
            try:
                stream.close()
            except Exception:
                self.failures.fail("command_diagnostic_capture_failed")

    def read_storage_report(self, path):
        try:
            with path.open("rb") as stream:
                content = stream.read(16 * 1024 * 1024 + 1)
            if len(content) > 16 * 1024 * 1024:
                self.storage_report_code = "too_large"
                return
            report = ET.fromstring(content)
        except OSError:
            self.storage_report_code = "unavailable"
            return
        except ET.ParseError:
            self.storage_report_code = "invalid"
            return
        for test in report.iter("test"):
            name = test.get("type", "") + "." + test.get("method", "")
            if test.get("result") != "Fail" or name not in self.tests:
                continue
            if len(self.failed_tests) < 32:
                self.failed_tests.add(name)
            for scenario in self.scenarios:
                if test.get("name") == name + f'(scenario: \\"{scenario}\\")':
                    self.failed_scenarios.add(scenario)
            for message in test.findall("failure/message"):
                text = message.text or ""
                if self.kind == "host" and len(text) <= 256:
                    assertion = self.http_assertion.fullmatch(text.strip())
                    if assertion and len(self.failed_statuses) < 32:
                        expected, actual = (
                            self.http_statuses.get(value, value) for value in assertion.groups()
                        )
                        if expected in self.http_statuses.values() and actual in self.http_statuses.values():
                            self.failed_statuses.add(f"expected_{expected}_actual_{actual}")
                found = {
                    code for code in re.findall(r"\b(?:storage|composition|identity|postgres)_[A-Za-z0-9_]{1,120}\b", text)
                    if code in self.codes
                }
                if text.strip() in self.codes:
                    found.add(text.strip())
                self.storage_codes.update(sorted(found)[:max(0, 32 - len(self.storage_codes))])
                if self.kind == "host":
                    for phase, launch, elapsed in self.browser_phase.findall(text):
                        value = {"phase": phase, "launch": int(launch), "elapsedMilliseconds": int(elapsed)}
                        if self.valid_browser_phase(value):
                            if len(self.browser_phases) == 64:
                                self.browser_phases.pop(16)
                            self.browser_phases.append(value)
                self.failed_operations.update(
                    re.findall(r"\boperation=(acquire|register|setup)\b", text)
                )
                self.inspect(text.encode())
            for output in test.findall("output"):
                self.read_composition_output(output.text or "")
            for stack in test.findall("failure/stack-trace"):
                for match in self.native_location.finditer((stack.text or "").replace("\\", "/")):
                    location = ":".join(match.groups())
                    if self.valid_native_location(location) and len(self.sources) < 32:
                        self.sources.add(location)
        if not self.failed_tests:
            self.storage_report_code = "no_known_failure"

    def native_success(self, path, minimum, required_classes=()):
        try:
            with path.open("rb") as stream:
                content = stream.read(16 * 1024 * 1024 + 1)
            if len(content) > 16 * 1024 * 1024:
                raise RuntimeFailure("native_evidence_too_large")
            report = ET.fromstring(content)
        except OSError as error:
            raise RuntimeFailure("native_evidence_missing") from error
        except ET.ParseError as error:
            raise RuntimeFailure("native_evidence_invalid") from error
        tests = list(report.iter("test"))
        if len(tests) < minimum:
            raise RuntimeFailure("native_evidence_cases_missing")
        if any(test.get("result") == "Skip" for test in tests):
            raise RuntimeFailure("native_evidence_skipped")
        if any(test.get("result") != "Pass" for test in tests) or next(report.iter("error"), None) is not None:
            raise RuntimeFailure("native_evidence_not_passed")
        observed_classes = {test.get("type", "").rsplit(".", 1)[-1] for test in tests}
        if not set(required_classes).issubset(observed_classes):
            raise RuntimeFailure("native_evidence_required_case_missing")
        return {
            "passed": len(tests), "skipped": 0,
            **({"requiredClasses": len(required_classes)} if required_classes else {}),
        }

    def valid_native_location(self, value):
        if not isinstance(value, str):
            return False
        path, _, line = value.rpartition(":")
        return (path in self.source_lines and re.fullmatch(r"[1-9][0-9]{0,5}", line) is not None
                and int(line) <= self.source_lines[path])

    def read_composition_output(self, output):
        for line in output.splitlines():
            if len(line) > 4096:
                continue
            try:
                event = json.loads(line)
            except ValueError:
                continue
            if (not isinstance(event, dict) or set(event) != {
                    "schemaVersion", "eventCode", "attempt", "provider", "scenario", "status", "assertions",
                } or type(event["schemaVersion"]) is not int or event["schemaVersion"] != 1
                    or event["status"] != "failed"
                    or type(event["assertions"]) is not int or event["assertions"] != 0
                    or event["provider"] not in ("sqlite", "postgres")
                    or not isinstance(event["scenario"], str) or event["scenario"] not in self.scenarios
                    or not isinstance(event["attempt"], str)
                    or re.fullmatch(r"[a-f0-9]{32}", event["attempt"]) is None
                    or not isinstance(event["eventCode"], str) or len(event["eventCode"]) > 512):
                continue
            self.failed_scenarios.add(event["scenario"])
            # Composite assertions retain fixed source symbols, never dynamic values.
            candidates = [event["eventCode"]] if event["eventCode"] in self.codes else sorted(self.codes)
            for code in candidates:
                if len(self.storage_codes) >= 32:
                    break
                if code.startswith("composition_") and (
                        code.endswith("_") and event["eventCode"].startswith(code)
                        or re.search(r"(?<![a-z0-9])" + re.escape(code) + r"(?![a-z0-9])", event["eventCode"])):
                    self.storage_codes.add(code)

    def valid_browser_phase(self, value):
        return (
            type(value) is dict and set(value) == {"phase", "launch", "elapsedMilliseconds"}
            and isinstance(value["phase"], str) and value["phase"] in self.codes
            and value["phase"].startswith("identity_browser_profile_")
            and type(value["launch"]) is int and value["launch"] in (1, 2)
            and type(value["elapsedMilliseconds"]) is int and 0 <= value["elapsedMilliseconds"] <= 60000
        )

    def safe_fields(self):
        with self.lock:
            if self.kind == "browser" and self.sources:
                return {"sourceLocations": sorted(self.sources)}
            if self.kind == "dependency" and len(self.dependency_codes) == 1:
                return {"dependencyFailureCode": next(iter(self.dependency_codes))}
            if self.kind in ("host", "storage", "contract", "consumer"):
                prefix = self.kind
                return {
                    **({f"failed{prefix.title()}Tests": sorted(self.failed_tests)} if self.failed_tests else {}),
                    **({"contractCompletedMethods": sorted(self.completed_methods)} if self.kind == "contract"
                       and self.completed_methods else {}),
                    **({f"{prefix}FailureCodes": sorted(self.storage_codes)} if self.storage_codes else {}),
                    **({f"{prefix}FailureOperations": sorted(self.failed_operations)} if self.failed_operations else {}),
                    **({f"{prefix}FailureScenarios": sorted(self.failed_scenarios)} if self.failed_scenarios else {}),
                    **({f"{prefix}FailureLocations": sorted(self.sources)} if self.sources else {}),
                    **({"hostFailureStatuses": sorted(self.failed_statuses)} if self.kind == "host"
                       and self.failed_statuses else {}),
                    **({"hostBrowserPhases": list(self.browser_phases)} if self.kind == "host"
                       and self.browser_phases else {}),
                    **({f"{prefix}DiagnosticCode": self.storage_report_code} if self.storage_report_code else {}),
                }
            return {}

    def clear(self):
        with self.lock:
            self.closed = True
            self.sources.clear()
            self.dependency_codes.clear()
            self.failed_tests.clear()
            self.completed_methods.clear()
            self.storage_codes.clear()
            self.failed_operations.clear()
            self.failed_scenarios.clear()
            self.failed_statuses.clear()
            self.browser_phases.clear()
            self.storage_report_code = None


class OutputCapture:
    def __init__(self, failures, forbidden, limit=4 * 1024 * 1024):
        self.failures, self.limit = failures, limit
        self.forbidden = [value.encode() for value in forbidden if value]
        self.buffers = []
        self.size = 0
        self.lock = threading.Lock()
        self.streams = 0

    def consume_identity_canaries(self, root):
        def unique_fields(pairs):
            fields = {}
            for name, value in pairs:
                if name in fields:
                    raise ValueError
                fields[name] = value
            return fields

        content = bytearray()
        try:
            root = Path(root)
            directory = root.lstat()
            if (root.resolve() != root.absolute() or not stat.S_ISDIR(directory.st_mode)
                    or stat.S_IMODE(directory.st_mode) != 0o700 or directory.st_uid != os.getuid()):
                raise ValueError
            path = root / "identity-browser-canaries.json"
            flags = os.O_RDONLY | getattr(os, "O_NOFOLLOW", 0) | getattr(os, "O_NONBLOCK", 0)
            with os.fdopen(os.open(path, flags), "rb") as stream:
                metadata = os.fstat(stream.fileno())
                if (not stat.S_ISREG(metadata.st_mode) or stat.S_IMODE(metadata.st_mode) != 0o600
                        or metadata.st_uid != directory.st_uid or metadata.st_nlink != 1
                        or not 0 < metadata.st_size <= 65536):
                    raise ValueError
                content.extend(stream.read(65537))
                if len(content) != metadata.st_size:
                    raise ValueError
            values = json.loads(content.decode("utf-8"), object_pairs_hook=unique_fields)
            if (not isinstance(values, dict) or set(values) != {"version", "canaries"}
                    or type(values["version"]) is not int or values["version"] != 1
                    or not isinstance(values["canaries"], list)
                    or not 3 <= len(values["canaries"]) <= 256
                    or any(not isinstance(value, str) or not 16 <= len(value) <= 8192
                           for value in values["canaries"])
                    or len(set(values["canaries"])) != len(values["canaries"])):
                raise ValueError
            current = path.lstat()
            if (current.st_dev, current.st_ino) != (metadata.st_dev, metadata.st_ino):
                raise ValueError
            with self.lock:
                for value in values["canaries"]:
                    self.forbidden.extend((value.encode(), quote(value, safe="-_.!~*'()").encode()))
            path.unlink()
            return {"identityCanaries": len(values["canaries"])}
        except (OSError, ValueError, TypeError, RecursionError) as error:
            raise RuntimeFailure("identity_browser_transport_canaries_invalid") from error
        finally:
            content[:] = b"\0" * len(content)

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
