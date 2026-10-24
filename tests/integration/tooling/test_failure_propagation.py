"""Exercise real verifier failures in a disposable source-isolated checkout."""

from contextlib import contextmanager
import argparse
import hashlib
import json
import os
from pathlib import Path
import re
import shutil
import signal
import socket
import subprocess
import sys
import time


ROOT = Path(__file__).resolve().parents[3]
BUDGET_SECONDS = 1800
SOURCE_SUFFIXES = {
    ".cs", ".csproj", ".props", ".targets", ".slnx", ".json", ".py", ".mjs",
    ".js", ".ts", ".tsx", ".css", ".md", ".yml", ".yaml", ".svg",
}
ROOT_FILES = {
    ".editorconfig", ".gitignore", ".npmrc", ".prettierignore", "LICENSE",
    "LICENSE.md", "README.md", "CONTRIBUTING.md", "eslint.config.mjs",
    "package.json", "package-lock.json", "global.json", "Kofte.slnx",
    "Directory.Build.props", "Directory.Packages.props", "NuGet.Config", "nuget.config",
}
SOURCE_PREFIXES = (
    "src/", "apps/", "packages/", "tests/", "scripts/", "deploy/",
    "docs/", "specs/", ".specify/", ".github/", ".vscode/",
)
PRIVATE_PARTS = {
    ".git", "node_modules", "bin", "obj", ".next", "artifacts", "data",
    "test-results", "playwright-report", "__pycache__", ".venv",
}
QUALITY = ["--suite", "quality"]
POSTGRES = ["--suite", "integration", "--provider", "postgres", "--disposable-postgres"]
BROWSER = ["--suite", "browser"]


def source_allowed(relative):
    path = Path(relative)
    if (path.is_absolute() or ".." in path.parts or any(part in PRIVATE_PARTS for part in path.parts)
            or any(part.startswith(".env") for part in path.parts)
            or ".local." in path.name or path.name.lower().startswith(("credentials", "secrets", "appsettings."))
            or relative.endswith(("settings.local.json", ".pem", ".key", ".pfx", ".p12"))):
        return False
    return relative in ROOT_FILES or (
        relative.startswith(SOURCE_PREFIXES) and path.suffix in SOURCE_SUFFIXES
    )


def overlay(source, destination, names, error):
    digest = hashlib.sha256()
    count = 0
    for relative in sorted(set(names)):
        if not source_allowed(relative):
            continue
        original, copied = source / relative, destination / relative
        if (original.is_symlink() or not original.resolve().is_relative_to(source.resolve())
                or copied.is_symlink() or not copied.resolve().is_relative_to(destination.resolve())):
            raise error("failure_overlay_path_unsafe")
        digest.update(relative.encode() + b"\0")
        if not original.exists():
            if copied.exists():
                copied.unlink()
            digest.update(b"deleted\0")
            continue
        if not original.is_file() or original.stat().st_size > 5_000_000:
            raise error("failure_overlay_source_invalid")
        before = original.stat()
        content = original.read_bytes()
        after = original.stat()
        if (before.st_mtime_ns, before.st_size) != (after.st_mtime_ns, after.st_size):
            raise error("failure_overlay_source_changed")
        copied.parent.mkdir(parents=True, exist_ok=True)
        copied.write_bytes(content)
        os.chmod(copied, 0o644)
        digest.update(hashlib.sha256(content).digest())
        count += 1
    return {"overlayFiles": count, "overlaySha256": digest.hexdigest()}


@contextmanager
def injected(checkout, relative, content, error):
    if not source_allowed(relative):
        raise error("failure_injection_path_invalid")
    path = checkout / relative
    if path.exists() or path.is_symlink() or not path.resolve().is_relative_to(checkout.resolve()):
        raise error("failure_injection_target_preexists")
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(content)
    try:
        yield
    finally:
        path.unlink()


def command_capture(command, cwd, environment, error, timeout=30):
    try:
        result = subprocess.run(
            command, cwd=cwd, env=environment, capture_output=True, timeout=timeout,
        )
    except (OSError, subprocess.TimeoutExpired) as failure:
        raise error("failure_prerequisite_unavailable") from failure
    if result.returncode:
        raise error("failure_prerequisite_failed")
    return result.stdout


def isolated_environment(original, directory, api):
    environment = api.child_environment(original)
    original_home = Path(original.get("HOME", str(Path.home())))
    home, config, scratch = directory / "home", directory / "config", directory / "scratch"
    xdg = {
        "XDG_CONFIG_HOME": config / "xdg", "XDG_CACHE_HOME": directory / "cache",
        "XDG_DATA_HOME": directory / "share", "XDG_STATE_HOME": directory / "state",
        "XDG_RUNTIME_DIR": directory / "runtime",
    }
    for path in (home, config, scratch, config / "docker", config / "empty-plugins", *xdg.values()):
        path.mkdir(parents=True, exist_ok=True, mode=0o700)
    empty = config / "empty"
    empty.write_text("")
    npm_user, npm_global = config / "npm-user", config / "npm-global"
    npm_user.write_text("")
    npm_global.write_text("")
    npm_cache = original.get("npm_config_cache", str(original_home / ".npm"))
    nuget_cache = original.get("NUGET_PACKAGES", str(original_home / ".nuget/packages"))
    browsers = original.get("PLAYWRIGHT_BROWSERS_PATH", str(
        original_home / ("Library/Caches/ms-playwright" if sys.platform == "darwin" else ".cache/ms-playwright")
    ))
    for key in tuple(environment):
        if (key.startswith(("NUGET_", "npm_config_", "NPM_CONFIG_", "DOCKER_", "GIT_"))
                or key in ("PYTHONPATH", "PYTHONHOME", "NODE_PATH", "VIRTUAL_ENV", "MSBuildSDKsPath")):
            del environment[key]
    environment.update({
        "HOME": str(home), "DOTNET_CLI_HOME": str(home),
        "TMPDIR": str(scratch), "TMP": str(scratch), "TEMP": str(scratch),
        "npm_config_cache": npm_cache, "npm_config_userconfig": str(npm_user),
        "npm_config_globalconfig": str(npm_global),
        "npm_config_logs_max": "0", "npm_config_logs_dir": str(config / "npm-logs"),
        "NUGET_PACKAGES": nuget_cache, "NUGET_HTTP_CACHE_PATH": str(config / "nuget-http"),
        "NUGET_CREDENTIALPROVIDERS_PATH": str(config / "empty-plugins"),
        "PLAYWRIGHT_BROWSERS_PATH": browsers, "DOCKER_CONFIG": str(config / "docker"),
        "GIT_CONFIG_GLOBAL": str(empty), "GIT_CONFIG_SYSTEM": str(empty),
        "GIT_CONFIG_NOSYSTEM": "1", "GIT_TERMINAL_PROMPT": "0",
        "DOTNET_SKIP_FIRST_TIME_EXPERIENCE": "1", "DOTNET_NOLOGO": "1",
    })
    environment.update({key: str(path) for key, path in xdg.items()})
    compose = shutil.which("docker-compose", path=original.get("PATH"))
    if compose:
        plugins = config / "docker/cli-plugins"
        plugins.mkdir()
        (plugins / "docker-compose").symlink_to(Path(compose).resolve())
    return environment


def checked_evidence(report, suite, provider, expected_step, expected_command, exit_code, api):
    error = api.VerificationError
    if (report.get("suite") != suite or report.get("provider") != provider
            or report.get("scope") != "targeted" or report.get("completeGate") is not False
            or not re.fullmatch(r"[a-f0-9]{40,64}", report.get("revision", ""))):
        raise error("failure_child_report_invalid")
    steps = report.get("steps", [])
    if not steps or any(step.get("outcome") not in ("passed", "failed") for step in steps):
        raise error("failure_child_report_incomplete")
    result = {key: report[key] for key in (
        "revision", "suite", "provider", "scope", "completeGate", "start", "end",
        "durationSeconds", "environment", "toolVersions", "outcome",
    )}
    result["steps"] = []
    for step in steps:
        selected = {key: step[key] for key in (
            "name", "command", "start", "end", "durationSeconds", "outcome", "exitCode",
        )}
        if selected["command"] != api.safe_command(selected["command"]):
            raise error("failure_child_report_unsafe")
        if step["outcome"] == "failed":
            selected["failureCode"] = step["failureCode"]
            selected["failureCorrelation"] = step["failureCorrelation"]
            if "sourceLocations" in step:
                locations = step["sourceLocations"]
                if (not isinstance(locations, list) or len(locations) > 32 or any(
                    not isinstance(location, str)
                    or api.runtime_support.CommandDiagnostics.locations.fullmatch(location.encode()) is None
                    for location in locations
                )):
                    raise error("failure_child_report_unsafe")
                selected["sourceLocations"] = locations
            if "dependencyFailureCode" in step:
                code = step["dependencyFailureCode"]
                if not isinstance(code, str) or code not in api.check_dependencies.FAILURE_CODES:
                    raise error("failure_child_report_unsafe")
                selected["dependencyFailureCode"] = code
            for kind in ("storage", "contract"):
                test_key = f"failed{kind.title()}Tests"
                code_key = f"{kind}FailureCodes"
                operation_key = f"{kind}FailureOperations"
                report_key = f"{kind}DiagnosticCode"
                if not any(key in step for key in (test_key, code_key, operation_key, report_key)):
                    continue
                diagnostic = api.command_diagnostics(step["command"])
                if diagnostic is None or diagnostic.kind != kind:
                    raise error("failure_child_report_unsafe")
                for key, allowed in (
                    (test_key, diagnostic.tests), (code_key, diagnostic.codes),
                    (operation_key, diagnostic.storage_operations),
                ):
                    if key not in step:
                        continue
                    values = step[key]
                    if (not isinstance(values, list) or not 1 <= len(values) <= 32
                            or any(not isinstance(value, str) or value not in allowed for value in values)
                            or len(set(values)) != len(values)):
                        raise error("failure_child_report_unsafe")
                    selected[key] = values
                if report_key in step:
                    code = step[report_key]
                    if not isinstance(code, str) or code not in diagnostic.storage_report_codes:
                        raise error("failure_child_report_unsafe")
                    selected[report_key] = code
        result["steps"].append(selected)
    failed = [step for step in steps if step["outcome"] == "failed"]
    code = None
    if expected_step is None:
        if exit_code or report.get("outcome") != "passed" or failed:
            code = "failure_baseline_not_green"
    elif (exit_code == 0 or report.get("outcome") != "failed" or len(failed) != 1
          or failed[0].get("name") != expected_step
          or failed[0].get("command") != expected_command
          or failed[0].get("failureCode") != "command_failed"
          or not isinstance(failed[0].get("exitCode"), int) or failed[0]["exitCode"] == 0
          or not re.fullmatch(r"[a-f0-9]{32}", failed[0].get("failureCorrelation", ""))):
        code = "failure_did_not_reach_expected_command"
    if code:
        failure = error(code)
        failure.evidence = result
        raise failure
    return result


def run(runner, api):
    error = api.VerificationError
    deadline = time.monotonic() + BUDGET_SECONDS
    with api.private_root(runner.root) as directory:
        checkout = directory / "checkout"
        environment = isolated_environment(runner.environment, directory, api)
        runner.command("isolated-clone", [
            "git", "clone", "--local", "--no-hardlinks", "--quiet",
            "--", str(runner.root), str(checkout),
        ], environment=environment, timeout=60)
        if (checkout / ".git/objects/info/alternates").exists():
            raise error("failure_clone_alternates_refused")
        changes = command_capture(
            ["git", "diff", "--name-only", "-z", "HEAD"], runner.root, environment, error,
        )
        untracked = command_capture(
            ["git", "ls-files", "--others", "--exclude-standard", "-z"],
            runner.root, environment, error,
        )

        def copy_sources(step):
            names = [name.decode() for name in (changes + untracked).split(b"\0") if name]
            step.update(overlay(runner.root, checkout, names, error))
        runner.operation("isolated-source-overlay", copy_sources)

        def docker_endpoint(_):
            endpoint = command_capture(
                ["docker", "context", "inspect", "--format", "{{.Endpoints.docker.Host}}"],
                runner.root, runner.environment, error,
            ).decode().strip()
            if runner.environment.get("DOCKER_HOST") and not runner.environment.get("DOCKER_CONTEXT"):
                endpoint = runner.environment["DOCKER_HOST"]
            if not endpoint.startswith("unix://") or not Path(endpoint[7:]).is_socket():
                raise error("failure_local_docker_required")
            environment["DOCKER_HOST"] = endpoint
        runner.operation("failure-docker-prerequisite", docker_endpoint)

        def invoke(name, arguments, expected_step=None, expected_command=None, extra=None):
            def exercise(step):
                reports = checkout / "artifacts/verification"
                before = set(reports.glob("*.json"))
                processes = api.OwnedProcesses(checkout, environment | (extra or {}))
                try:
                    child = processes.start(
                        ["python3", "scripts/verify.py", *arguments], stdin=subprocess.DEVNULL,
                    )
                    try:
                        exit_code = child.wait(timeout=max(0.01, min(900, deadline - time.monotonic())))
                    except subprocess.TimeoutExpired as failure:
                        raise error("failure_exercise_timeout") from failure
                finally:
                    processes.stop()
                created = set(reports.glob("*.json")) - before
                if len(created) != 1:
                    raise error("failure_child_report_missing")
                report = json.loads(created.pop().read_text())
                suite = arguments[arguments.index("--suite") + 1]
                provider = "postgres" if suite == "integration" else None
                step.update(executionRoot="isolated-checkout", exitCode=exit_code,
                            expectedOutcome="failed" if expected_step else "passed")
                try:
                    step["verification"] = checked_evidence(
                        report, suite, provider, expected_step, expected_command, exit_code, api,
                    )
                except error as failure:
                    if hasattr(failure, "evidence"):
                        step["verification"] = failure.evidence
                    raise
            runner.operation(name, exercise, ["python3", "scripts/verify.py", *arguments])

        invoke("baseline-quality", QUALITY)
        invoke("baseline-postgres", POSTGRES)
        invoke("baseline-browser", BROWSER)
        quality_commands = {name: list(command) for name, command in api.QUALITY_COMMANDS}
        with injected(checkout, "scripts/tests/test_injected_failure_probe.py",
                      "import unittest\n\n\nclass InjectedFailure(unittest.TestCase):\n"
                      "    def test_real_assertion_failure(self):\n"
                      "        self.assertEqual(1, 2)\n", error):
            invoke("test-failure-propagates", QUALITY, "python-tests", quality_commands["python-tests"])
        with injected(checkout, "tests/unit/Kofte.Host.Tests/ToolingFormatProbe.cs",
                      "internal static class ToolingFormatProbe{public static int Value=>1;}\n", error):
            invoke("formatter-failure-propagates", QUALITY, "dotnet-format", quality_commands["dotnet-format"])
        with injected(checkout, "apps/web/app/tooling-build-probe/page.tsx",
                      'export default function ToolingBuildProbe() {\n'
                      '  throw new Error("tooling_fixture_build_failure");\n}\n', error):
            invoke("build-failure-propagates", QUALITY, "workspace-builds", quality_commands["workspace-builds"])
        with socket.socket() as unavailable:
            unavailable.bind(("127.0.0.1", 0))
            port = unavailable.getsockname()[1]
            connection = (
                f"Host=127.0.0.1;Port={port};Database=postgres;Username=kofte_unavailable;"
                "Password=unavailable_fixture_only;Timeout=1;Command Timeout=1;SSL Mode=Disable"
            )
            invoke("postgres-unavailable-propagates", POSTGRES, "integration-postgres", [
                "dotnet", "test", "--project", api.INTEGRATION, "--no-restore", "-c", "Release",
                "--filter-trait", "Provider=postgres", "--minimum-expected-tests", "1",
            ], {"KOFTE_POSTGRES_CONNECTION": connection})
        missing_browser = directory / "unavailable-browser"
        missing_browser.mkdir(mode=0o700)
        specs = api.browser_specs(checkout)
        invoke("chromium-unavailable-propagates", BROWSER, "browser-tests", [
            "npm", "run", "test", "--workspace", "tests/e2e", "--", *specs,
        ], {"PLAYWRIGHT_BROWSERS_PATH": str(missing_browser)})


if __name__ == "__main__":
    sys.path.insert(0, str(ROOT))
    from scripts import verify

    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--disposable-postgres", action="store_true", required=True)
    parser.parse_args()

    def interrupt(_number, _frame):
        raise KeyboardInterrupt

    signal.signal(signal.SIGTERM, interrupt)
    try:
        raise SystemExit(verify.main([
            "--suite", "failure-propagation", "--disposable-postgres",
        ]))
    except (verify.VerificationError, OSError, subprocess.TimeoutExpired, KeyboardInterrupt):
        print(json.dumps({"eventCode": "failure_harness_setup_failed"}), flush=True)
        raise SystemExit(1)
