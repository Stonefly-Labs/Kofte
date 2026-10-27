"""Attached, local-only bootstrap supervisor. No installation or store creation."""

from dataclasses import dataclass
import ipaddress
import json
import os
from pathlib import Path
import platform
import re
import shutil
import signal
import socket
import subprocess
import sys
import threading
import time
from urllib.error import HTTPError, URLError
from urllib.parse import urlsplit
from urllib.request import ProxyHandler, Request, build_opener
import uuid


ROOT = Path(__file__).resolve().parents[1]
VERSIONS = {"dotnet": "10.0.402", "node": "v24.22.0", "npm": "11.19.1"}
STARTUP_BUDGET_SECONDS = 285
KEYS = {
    "KOFTE_PROFILE", "KOFTE_HOST_URL", "KOFTE_WEB_URL", "KOFTE_BACKEND_ORIGIN",
    "KOFTE_DATA_ROOT", "KOFTE_POSTGRES_CONNECTION", "KOFTE_ALLOW_UNSAFE_EXPOSURE",
    "KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE", "KOFTE_AUTH_CONFIG_FILE",
}
ENV_FILE_KEYS = KEYS | {"ASPNETCORE_ENVIRONMENT", "OTEL_EXPORTER_OTLP_ENDPOINT"}


class DevError(Exception):
    def __init__(self, code, component="supervisor"):
        super().__init__(code)
        self.code, self.component = code, component


def emit(code, component="supervisor", *, error=False):
    print(json.dumps({
        "eventCode": code, "component": component,
        "correlationId": uuid.uuid4().hex,
    }), file=sys.stderr if error else sys.stdout, flush=True)


def load_environment(path, explicit):
    values = {}
    if path.exists():
        for line in path.read_text().splitlines():
            line = line.strip()
            if not line or line.startswith("#"):
                continue
            key, separator, value = line.partition("=")
            key, value = key.strip(), value.strip()
            if not separator or key not in ENV_FILE_KEYS or key in values:
                raise DevError("configuration_invalid")
            if value.startswith(("'", '"')):
                if len(value) < 2 or value[-1] != value[0]:
                    raise DevError("configuration_invalid")
                value = value[1:-1]
            if "\x00" in value:
                raise DevError("configuration_invalid")
            values[key] = value
    values.update(explicit)
    if any(key.startswith("KOFTE_") and key not in KEYS for key in values):
        raise DevError("configuration_invalid")
    return values


def loopback(hostname):
    if hostname == "localhost":
        return True
    try:
        return ipaddress.ip_address(hostname).is_loopback
    except ValueError:
        try:
            return ipaddress.IPv4Address(socket.inet_aton(hostname)).is_loopback
        except OSError:
            return False


def address(value, unsafe=False):
    try:
        url = urlsplit(value)
        port = url.port or 80
        if (not re.fullmatch(r"http://[^/?#@\s]+/?", value) or not url.hostname or
                url.username is not None or url.password is not None or url.port == 0):
            raise DevError("configuration_invalid")
        if not loopback(url.hostname) and not unsafe:
            raise DevError("configuration_invalid")
        return url.hostname, port
    except ValueError as error:
        raise DevError("configuration_invalid") from error


@dataclass(frozen=True)
class Configuration:
    environment: dict
    host: str
    web: str
    backend: str


def configuration(environment):
    env = dict(environment)
    if env.get("KOFTE_PROFILE", "local-sqlite") != "local-sqlite" or "KOFTE_POSTGRES_CONNECTION" in env:
        raise DevError("configuration_invalid")
    for key in ("KOFTE_ALLOW_UNSAFE_EXPOSURE", "KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE"):
        if env.get(key, "false") not in ("true", "false"):
            raise DevError("configuration_invalid")
    unsafe = env.get("KOFTE_ALLOW_UNSAFE_EXPOSURE") == "true"
    host = env.get("KOFTE_HOST_URL", "http://127.0.0.1:5080")
    web = env.get("KOFTE_WEB_URL", "http://127.0.0.1:3000")
    host_address, web_address = address(host, unsafe), address(web, unsafe)
    if host_address[1] == web_address[1]:
        raise DevError("configuration_invalid")
    host_is_loopback = loopback(host_address[0])
    backend = env.get("KOFTE_BACKEND_ORIGIN",
                      host if host_is_loopback else f"http://127.0.0.1:{host_address[1]}")
    backend_address = address(backend)
    if (backend_address[1] != host_address[1] or
            (host_is_loopback and backend_address[0] != host_address[0] and not (
                host_address[0] == "localhost" and backend_address[0] in ("127.0.0.1", "::1"))) or
            (host_address[0] == "0.0.0.0" and ":" in backend_address[0])):
        raise DevError("configuration_invalid")
    root = Path(env.get("KOFTE_DATA_ROOT", str(ROOT / "data")))
    if not root.is_absolute() or ".." in root.parts:
        raise DevError("configuration_invalid")
    env.update({
        "KOFTE_PROFILE": "local-sqlite", "KOFTE_HOST_URL": host,
        "KOFTE_WEB_URL": web, "KOFTE_BACKEND_ORIGIN": backend,
        "KOFTE_DATA_ROOT": str(root), "ASPNETCORE_ENVIRONMENT": "Development",
        "NEXT_TELEMETRY_DISABLED": "1",
    })
    return Configuration(env, host, web, backend)


def check_tools(environment, deadline):
    if sys.version_info < (3, 11):
        raise DevError("tool_version_invalid")
    system, machine = platform.system(), platform.machine().lower()
    if system == "Darwin":
        if machine != "arm64" or int(platform.mac_ver()[0].split(".")[0]) < 15:
            raise DevError("unsupported_platform")
    elif system == "Linux":
        release = platform.freedesktop_os_release()
        if machine != "x86_64" or release.get("ID") != "ubuntu" or release.get("VERSION_ID") != "24.04":
            raise DevError("unsupported_platform")
    else:
        raise DevError("unsupported_platform")
    for tool, expected in VERSIONS.items():
        if not shutil.which(tool, path=environment.get("PATH")):
            raise DevError("tool_missing", tool)
        result = subprocess.run([tool, "--version"], cwd=ROOT, env=environment,
                                capture_output=True, timeout=max(0.01, deadline - time.monotonic()))
        if result.returncode or result.stdout.decode(errors="replace").strip() != expected:
            raise DevError("tool_version_invalid", tool)


def check_ports(config):
    for url in (config.host, config.web, config.backend):
        hostname, port = address(url, config.environment.get("KOFTE_ALLOW_UNSAFE_EXPOSURE") == "true")
        try:
            for family, kind, protocol, _, endpoint in socket.getaddrinfo(
                    hostname, port, type=socket.SOCK_STREAM):
                with socket.socket(family, kind, protocol) as probe:
                    probe.setsockopt(socket.SOL_SOCKET, socket.SO_REUSEADDR, 1)
                    probe.bind(endpoint)
        except OSError as error:
            raise DevError("port_unavailable") from error


def run_step(command, component, environment, deadline):
    emit("building", component)
    children = Children()
    try:
        child = children.start(command, component, environment)
        if child.wait(timeout=max(0.01, deadline - time.monotonic())):
            raise DevError("build_failed", component)
    except (OSError, subprocess.TimeoutExpired) as error:
        raise DevError("build_failed", component) from error
    finally:
        children.stop()


def _discard(stream):
    while stream.read(4096):
        pass


class Children:
    def __init__(self):
        self.processes = []
        self.readers = []

    def start(self, command, component, environment):
        child = subprocess.Popen(command, cwd=ROOT, env=environment, start_new_session=True,
                                 stdout=subprocess.PIPE, stderr=subprocess.PIPE)
        self.processes.append((child, component))
        for stream in (child.stdout, child.stderr):
            reader = threading.Thread(target=_discard, args=(stream,), daemon=True)
            reader.start()
            self.readers.append((reader, child))
        return child

    def check(self):
        for child, component in self.processes:
            if child.poll() is not None:
                raise DevError("child_exited", component)

    def stop(self):
        deferred = []
        handlers = {}
        for name in ("SIGINT", "SIGTERM", "SIGALRM"):
            if hasattr(signal, name):
                number = getattr(signal, name)
                handlers[number] = signal.signal(number, lambda received, _: deferred.append(received))
        try:
            deadline = time.monotonic() + 5
            timed_out = set()
            for child, _ in reversed(self.processes):
                if child.returncode is not None and not any(
                        reader.is_alive() for reader, owner in self.readers if owner is child):
                    continue
                try:
                    os.killpg(child.pid, signal.SIGTERM)
                except ProcessLookupError:
                    pass
            for child, _ in self.processes:
                try:
                    child.wait(timeout=max(0.01, deadline - time.monotonic()))
                except subprocess.TimeoutExpired:
                    timed_out.add(child)
            for reader, _ in self.readers:
                reader.join(timeout=max(0, deadline - time.monotonic()))
            for child, _ in self.processes:
                if child not in timed_out and not any(
                        reader.is_alive() for reader, owner in self.readers if owner is child):
                    continue
                try:
                    os.killpg(child.pid, signal.SIGKILL)
                except ProcessLookupError:
                    pass
            deadline = time.monotonic() + 5
            for child, _ in self.processes:
                try:
                    child.wait(timeout=max(0.01, deadline - time.monotonic()))
                except subprocess.TimeoutExpired as error:
                    raise DevError("cleanup_failed") from error
            for reader, _ in self.readers:
                reader.join(timeout=max(0, deadline - time.monotonic()))
                if reader.is_alive():
                    raise DevError("cleanup_failed")
            for child, _ in self.processes:
                for stream in (child.stdout, child.stderr):
                    stream.close()
        finally:
            for number, handler in handlers.items():
                signal.signal(number, handler)
        if deferred:
            if hasattr(signal, "SIGALRM") and deferred[0] == signal.SIGALRM:
                raise DevError("startup_timeout")
            raise KeyboardInterrupt


def observed_ready(url):
    try:
        opener = build_opener(ProxyHandler({}))
        with opener.open(Request(url, headers={"Accept": "application/json"}), timeout=1) as response:
            raw = response.read(2049)
            if len(raw) > 2048 or response.status != 200:
                return False
            status = json.loads(raw)
            return (set(status) == {"version", "state", "component", "correlationId"} and
                    status["version"] == "1" and status["state"] == "ready" and
                    status["component"] == "storage" and
                    isinstance(status["correlationId"], str) and
                    re.fullmatch(r"[0-9a-f]{32}", status["correlationId"]) is not None and
                    response.headers.get("Cache-Control") == "no-store")
    except (URLError, HTTPError, TimeoutError, json.JSONDecodeError, UnicodeError, TypeError):
        return False


def wait_ready(url, children, deadline, component):
    while time.monotonic() < deadline:
        children.check()
        if observed_ready(url):
            return
        time.sleep(0.1)
    raise DevError("startup_timeout", component)


def supervise(config, *, startup_ready=None):
    deadline = time.monotonic() + STARTUP_BUDGET_SECONDS
    check_tools(config.environment, deadline)
    check_ports(config)
    if config.environment.get("KOFTE_ALLOW_UNSAFE_EXPOSURE") == "true":
        emit("unsafe_local_exposure", error=True)
    run_step(["dotnet", "build", "src/Kofte.Host", "--no-restore", "-c", "Release", "-warnaserror"],
             "host", config.environment, deadline)
    run_step(["npm", "run", "build", "--workspace", "apps/web"], "web", config.environment, deadline)
    children = Children()
    try:
        children.start(["dotnet", "src/Kofte.Host/bin/Release/net10.0/Kofte.Host.dll"],
                       "host", config.environment)
        wait_ready(config.backend.rstrip("/") + "/health/ready", children, deadline, "host")
        hostname, port = address(config.web, config.environment.get("KOFTE_ALLOW_UNSAFE_EXPOSURE") == "true")
        children.start(["npm", "run", "start", "--workspace", "apps/web", "--",
                        "--hostname", hostname, "--port", str(port)], "web", config.environment)
        web_probe = config.web
        if hostname in ("0.0.0.0", "::"):
            web_probe = f"http://127.0.0.1:{port}"
        wait_ready(web_probe.rstrip("/") + "/backend/health/ready", children, deadline, "web")
        if startup_ready is not None:
            startup_ready()
        emit("local_ready")
        while True:
            children.check()
            time.sleep(0.2)
    finally:
        children.stop()


def main():
    def interrupt(_signal, _frame):
        raise KeyboardInterrupt

    previous = signal.signal(signal.SIGTERM, interrupt)
    previous_alarm = None
    if hasattr(signal, "SIGALRM"):
        def expired(_signal, _frame):
            raise DevError("startup_timeout")
        previous_alarm = signal.signal(signal.SIGALRM, expired)
        signal.setitimer(signal.ITIMER_REAL, STARTUP_BUDGET_SECONDS)

    def ready():
        if previous_alarm is not None:
            signal.setitimer(signal.ITIMER_REAL, 0)

    try:
        if len(sys.argv) != 1:
            raise DevError("command_invalid")
        config = configuration(load_environment(ROOT / ".env", os.environ))
        supervise(config, startup_ready=ready)
    except KeyboardInterrupt:
        emit("local_stopped")
        return 0
    except DevError as error:
        emit(error.code, error.component, error=True)
        return 1
    except (OSError, subprocess.TimeoutExpired, UnicodeError):
        emit("startup_failed", error=True)
        return 1
    finally:
        if previous_alarm is not None:
            signal.setitimer(signal.ITIMER_REAL, 0)
            signal.signal(signal.SIGALRM, previous_alarm)
        signal.signal(signal.SIGTERM, previous)


if __name__ == "__main__":
    sys.exit(main())
