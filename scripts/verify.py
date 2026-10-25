"""Fail-closed product verification. Child output is transient, never a report."""

import argparse
from contextlib import contextmanager
from datetime import datetime, timezone
import importlib.util
import json
import math
import os
from pathlib import Path
import platform
import queue
import re
import shutil
import signal
import socket
import stat
import subprocess
import tempfile
import threading
import time
import uuid

if __package__:
    from scripts import check_dependencies, dev, runtime_support
else:
    import check_dependencies
    import dev
    import runtime_support


ROOT = Path(__file__).resolve().parents[1]
SUITES = (
    "quality", "unit", "integration", "recovery", "contract", "browser",
    "privacy", "startup", "failure-propagation",
)
REQUIRED_SCRIPTS = {
    ".": {"format:check", "contract:check", "dependencies:check"},
    "apps/web": {"build", "lint", "typecheck", "test:unit", "start"},
    "packages/bootstrap-contract": {"generate", "build", "lint", "typecheck", "test:unit"},
    "tests/contract": {"lint", "typecheck", "test"},
    "tests/e2e": {"lint", "typecheck", "test"},
}
# Missing implementations fail explicitly; existing sources still must execute.
UNIMPLEMENTED = {
    "recovery": "recovery_tests_unimplemented",
    "contract": "full_runtime_contract_tests_unimplemented",
    "privacy": "privacy_capture_tests_unimplemented",
}
REQUIRED_SUITE_FILES = {
    "recovery": ("tests/e2e/recovery.spec.ts",),
    "contract": ("tests/contract/bootstrap.test.ts", "tests/contract/adapter.test.ts"),
    "privacy": (
        "tests/contract/privacy.test.ts", "tests/e2e/privacy.spec.ts",
        "scripts/tests/test_dev_output.py",
    ),
}
PLATFORM_REQUIRED_FILES = (
    "src/Kofte.Platform/Kofte.Platform.csproj",
    "tests/unit/Kofte.Platform.Tests/Kofte.Platform.Tests.csproj",
    "tests/contract/Kofte.Platform.ContractTests/Kofte.Platform.ContractTests.csproj",
    "tests/contract/Kofte.Manifest.Consumer/Kofte.Manifest.Consumer.csproj",
    "tests/contract/Kofte.Manifest.Consumer/Program.cs",
    "tests/contract/module-manifest.test.ts",
    *(f"tests/unit/Kofte.Platform.Tests/Modules/Manifest/{name}.cs" for name in (
        "ValidationResultTests", "ModuleIdTests", "IdentityUpdateTests", "SemanticVersionTests",
        "VersionRangeTests", "DeclarationTests", "ManifestParsingTests", "ManifestBudgetTests",
    )),
    *(f"tests/contract/Kofte.Platform.ContractTests/Modules/Manifest/{name}.cs" for name in (
        "ManifestCorpus", "ConsumerHarness", "IdentityContractTests", "CompatibilityContractTests",
        "PublicConsumerTests", "DeterminismTests", "SafeOutputTests", "SideEffectTests",
        "RetryTests", "ManifestPerformanceTests",
    )),
)
HOST = "src/Kofte.Host/bin/Release/net10.0/Kofte.Host.dll"
INTEGRATION = "tests/integration/Kofte.Host.IntegrationTests"
STORAGE_INTEGRATION = "tests/integration/Kofte.Storage.IntegrationTests"
STORAGE_SCENARIOS = (
    "clean-install", "no-storage", "same-id-retry", "collision", "path-rejection",
    "interrupted-provisioning", "corrected-retry", "isolated-upgrade", "stopped-restore",
)
COMPOSITION_OBSERVATIONS = {
    "empty-boot": {"coreEndpoints": 2, "emptyInventoryComparisons": 2},
    "valid-dependency": {"publicServiceComparisons": 2, "phaseOrderComparisons": 1,
                         "ownerHistoryComparisons": 2, "prepublicationComparisons": 1},
    "disabled-missing": {"disabledCallbackComparisons": 1, "missingRefusals": 1, "unchangedInventories": 1,
                         "explicitDisabledObservations": 2, "omittedDisabledObservations": 2, "freshDisabledAttempts": 2},
    "invalid-composition": {"manifestRefusals": 3, "dependencyRefusals": 5, "settingsRefusals": 1,
                            "claimRefusals": 4, "limitRefusals": 1, "zeroEffectComparisons": 1},
    "deterministic-corpus": {"repetitionsPerPermutation": 100, "permutations": 10, "cultures": 2,
                             "publicConsumerPaths": 2, "exactDecisionComparisons": 4000},
    "ordered-migration": {"cleanHistoryComparisons": 2, "upgradeHistoryComparisons": 2,
                          "originalDataComparisons": 2, "wholeSetPreflightComparisons": 1,
                          "noStorageComparisons": 1, "unchangedServingRevisions": 2},
    "disable-reenable": {"completeCycles": 3, "distinctProcesses": 6, "distinctAttempts": 6,
                         "originalAssignmentComparisons": 12, "originalDataComparisons": 12,
                         "originalHistoryComparisons": 12, "unrelatedStateComparisons": 6,
                         "dependentDisableRefusals": 1},
    "failure-correction": {"configurationCorrections": 1, "initializeCorrections": 1,
                           "startCorrections": 1, "healthCorrections": 1, "migrationCorrections": 1,
                           "freshAttemptComparisons": 5, "verifiedExits": 5, "retainedCommitComparisons": 1,
                           "explicitRecoveryComparisons": 1},
    "health-impact": {"ownStateComparisons": 4, "freshnessComparisons": 1, "transitiveImpactComparisons": 1,
                      "independentRootComparisons": 1, "aggregateResponseComparisons": 2, "anonymousPrivacyComparisons": 1},
    "bounded-stop": {"ignoringTokenBoundaries": 7, "cancellationBoundaries": 2, "authorityRefusals": 2,
                     "reverseCleanupComparisons": 1, "verifiedRootExits": 1, "verifiedDescendantExits": 1,
                     "unverifiedTerminationRefusals": 1, "elapsedBudgetComparisons": 1},
    "private-diagnostics": {"capturedChannels": 6, "usefulRecordComparisons": 1, "zeroLeakComparisons": 1,
                            "invalidFrameRefusals": 3, "outputLimitRefusals": 2, "sinkFailureRefusals": 1,
                            "storageDurabilityComparisons": 1, "boundedLabelComparisons": 1},
}
COMPOSITION_REQUIRED_FILES = (
    "tests/contract/Kofte.Composition.Consumer/Kofte.Composition.Consumer.csproj",
    "tests/contract/Kofte.Composition.Consumer/Program.cs",
    "tests/contract/Kofte.Composition.TestModule/Kofte.Composition.TestModule.csproj",
    "tests/contract/Kofte.Composition.TestModule/StorageRecipes.cs",
    "tests/contract/Kofte.Composition.TestModule/FrozenCompositionCorpus.cs",
    "tests/integration/Kofte.TestSupport/CompositionScenarioCatalog.cs",
    "tests/integration/Kofte.TestSupport/CompositionConsumerHarness.cs",
    *(f"tests/contract/Kofte.Composition.Consumer/Scenarios/{name}.cs" for name in (
        "ValidationScenarios", "BootScenarios", "MigrationScenarios", "MigrationFailureScenarios",
        "PreservationScenarios", "HealthScenarios", "StopScenarios", "CorrectionScenarios",
        "DiagnosticScenarios", "CompositionCapture",
    )),
    *(f"tests/contract/Kofte.Platform.ContractTests/Composition/{name}.cs" for name in (
        "CompositionConsumerHarnessTests", "CompositionPublicContractTests", "CompositionBootContractTests",
        "CompositionMigrationContractTests", "CompositionHealthContractTests", "CompositionFailureContractTests",
        "CompositionPrivacyContractTests",
    )),
    *(f"{STORAGE_INTEGRATION}/Composition/{name}.cs" for name in (
        "CompositionPreflightTests", "CompositionMigrationTests", "CompositionProviderTests", "CompositionAuthorityTests",
        "CompositionRecoveryTests", "CompositionPreservationTests", "CompositionSelectionTests",
        "CompositionUpgradePreservationTests", "CompositionDeadlineTests", "CompositionPrivacyTests",
    )),
    "tests/integration/Kofte.Host.IntegrationTests/CompositionHealthEndpointTests.cs",
    "tests/unit/Kofte.Host.Tests/Features/ModuleComposition/CompositionHealthTests.cs",
    "tests/unit/Kofte.Host.Tests/Features/ModuleComposition/ModuleLifecycleTests.cs",
    "tests/unit/Kofte.Platform.Tests/Modules/Composition/CompositionResolverTests.cs",
)
STORAGE_REQUIRED_FILES = (
    "src/Kofte.Infrastructure/Kofte.Infrastructure.csproj",
    "src/Kofte.Platform/Modules/Storage/StorageContracts.cs",
    f"{STORAGE_INTEGRATION}/Kofte.Storage.IntegrationTests.csproj",
    "tests/integration/Kofte.TestSupport/StorageScenarioCatalog.cs",
    "tests/integration/Kofte.TestSupport/StorageCrashWorker.cs",
    "tests/integration/Kofte.TestSupport/StorageResultChecks.cs",
    "tests/contract/Kofte.Storage.Consumer/Kofte.Storage.Consumer.csproj",
    "tests/contract/Kofte.Storage.Consumer/Program.cs",
    *(f"{STORAGE_INTEGRATION}/{name}.cs" for name in (
        "Registry/RegistrationTests", "Registry/RestartTests", "Registry/RegistrationConcurrencyTests",
        "Registry/AllocationCollisionTests", "Registry/RegistryConstraintTests", "Registry/RegistryFormatTests",
        "Registry/PostgresRegistryFormatTests", "Providers/ModuleMigrationTests", "Providers/AssignedModelTests",
        "Providers/StorageBoundaryTests", "Providers/StoragePrivacyTests", "Providers/StorageMaintenanceDiagnosticsTests",
        "Recovery/ProvisioningCrashTests",
        "Recovery/MigrationCrashTests", "Recovery/MigrationRecoveryRefusalTests", "Recovery/OperationDeadlineTests",
        "Recovery/PostgresLeaseLossTests", "Recovery/RegistryUpgradeTests", "Recovery/RecoverySetTests",
        "Recovery/AbandonedLockRefusalTests", "Recovery/RecoveryDiagnosticsTests",
        "Recovery/MaintenanceObserverFailureTests",
        "Recovery/CorrectedRetryTests",
    )),
    *(f"tests/contract/Kofte.Platform.ContractTests/Modules/Storage/{name}.cs" for name in (
        "StorageConsumerHarness", "StorageConsumerHarnessTests", "RegistrationContractTests",
        "BindingContractTests", "ModuleContractTests", "RecoveryContractTests",
        "StorageGuideTests", "StorageDiagnosticsTests",
    )),
    *(f"tests/contract/Kofte.Storage.Consumer/Scenarios/{name}.cs" for name in (
        "RegistrationScenarios", "CollisionScenario", "ModuleScenarios", "RecoveryScenarios", "CorrectedRetryScenario",
    )),
)
FIXTURE = f"{INTEGRATION}/bin/Release/net10.0/Kofte.Host.IntegrationTests.dll"
QUALITY_COMMANDS = (
    ("repository", ("python3", "scripts/check_repository.py")),
    ("python-tests", ("python3", "-m", "unittest", "discover", "-s", "scripts/tests", "-v")),
    ("whitespace", ("git", "diff", "--check")),
    ("restore", ("dotnet", "restore", "Kofte.slnx", "--locked-mode")),
    ("dotnet-format", ("dotnet", "format", "Kofte.slnx", "--verify-no-changes", "--no-restore")),
    ("dotnet-build", ("dotnet", "build", "Kofte.slnx", "--no-restore", "-c", "Release", "-warnaserror")),
    ("npm-restore", ("npm", "ci")),
    ("prettier", ("npm", "run", "format:check")),
    ("eslint", ("npm", "run", "lint", "--workspaces", "--if-present")),
    ("typescript", ("npm", "run", "typecheck", "--workspaces", "--if-present")),
    ("contract-generation", ("npm", "run", "contract:check")),
    ("workspace-builds", ("npm", "run", "build", "--workspaces", "--if-present")),
    ("dependency-review", ("npm", "run", "dependencies:check")),
)


class VerificationError(Exception):
    def __init__(self, code):
        super().__init__(code)
        self.code = code


def utcnow():
    return datetime.now(timezone.utc).isoformat()


def child_environment(environment):
    return {
        key: value for key, value in environment.items()
        if not key.startswith(("KOFTE_", "OTEL_", "NEXT_PUBLIC_", "BOOTSTRAP_", "COMPOSITION_TEST_"))
        and key not in ("NODE_OPTIONS", "ASPNETCORE_URLS", "DOTNET_STARTUP_HOOKS")
    } | {"NEXT_TELEMETRY_DISABLED": "1", "DOTNET_CLI_TELEMETRY_OPTOUT": "1"}


def workspace_inventory(root):
    manifest = json.loads((root / "package.json").read_text())
    if set(manifest.get("workspaces", [])) != set(REQUIRED_SCRIPTS) - {"."}:
        raise VerificationError("workspace_inventory_drift")
    for workspace, required in REQUIRED_SCRIPTS.items():
        scripts = json.loads((root / workspace / "package.json").read_text()).get("scripts", {})
        if any(not isinstance(scripts.get(name), str) or not scripts[name].strip() for name in required):
            raise VerificationError("required_workspace_script_missing")

def storage_inventory(root):
    if any(not (root / path).is_file() for path in STORAGE_REQUIRED_FILES):
        raise VerificationError("storage_target_missing")
    catalog = (root / "tests/integration/Kofte.TestSupport/StorageScenarioCatalog.cs").read_text()
    if not set(STORAGE_SCENARIOS).issubset(set(re.findall(r'"([a-z-]+)"', catalog))):
        raise VerificationError("storage_scenario_missing")

def composition_inventory(root):
    if any(not (root / path).is_file() for path in COMPOSITION_REQUIRED_FILES):
        raise VerificationError("composition_target_missing")
    catalog = (root / "tests/integration/Kofte.TestSupport/CompositionScenarioCatalog.cs").read_text()
    if set(re.findall(r'\["([a-z-]+)"\] = Required', catalog)) != set(COMPOSITION_OBSERVATIONS):
        raise VerificationError("composition_scenario_missing")
    solution = (root / "Kofte.slnx").read_text()
    if any(path not in solution for path in COMPOSITION_REQUIRED_FILES[:4] if path.endswith(".csproj")):
        raise VerificationError("composition_build_target_missing")


def composition_result(stdout, stderr, provider, scenario):
    def unique(pairs):
        result = {}
        for key, value in pairs:
            if key in result:
                raise ValueError
            result[key] = value
        return result

    def keys(value, expected):
        if type(value) is not dict or set(value) != set(expected):
            raise ValueError

    try:
        if provider not in ("sqlite", "postgres") or scenario not in (*COMPOSITION_OBSERVATIONS, "all"):
            raise ValueError
        if len(stdout) > 65536 or stderr.strip():
            raise ValueError
        root = json.loads(stdout, object_pairs_hook=unique)
        keys(root, ("schemaVersion", "eventCode", "attempt", "provider", "scenario", "status", "assertions", "scenarios"))
        if (type(root["schemaVersion"]) is not int or root["schemaVersion"] != 1
                or root["eventCode"] != "composition_scenarios_passed" or root["provider"] != provider
                or root["scenario"] != scenario or root["status"] != "passed"
                or not isinstance(root["attempt"], str) or not re.fullmatch("[a-f0-9]{32}", root["attempt"])):
            raise ValueError
        expected = COMPOSITION_OBSERVATIONS if scenario == "all" else {scenario: COMPOSITION_OBSERVATIONS[scenario]}
        keys(root["scenarios"], expected)
        cases = {}
        for name, required in expected.items():
            case = root["scenarios"][name]
            keys(case, ("assertions", "observations"))
            if type(case["assertions"]) is not int or not 0 < case["assertions"] <= 2_147_483_647:
                raise ValueError
            keys(case["observations"], required)
            if any(type(case["observations"][field]) is not int or not minimum <= case["observations"][field] <= 2_147_483_647
                   for field, minimum in required.items()):
                raise ValueError
            cases[name] = {"assertions": case["assertions"], "observations": dict(case["observations"])}
        if (type(root["assertions"]) is not int or not 0 < root["assertions"] <= 2_147_483_647
                or root["assertions"] != sum(case["assertions"] for case in cases.values())):
            raise ValueError
        return {"assertions": root["assertions"], "scenarios": cases}
    except (ValueError, TypeError, KeyError, UnicodeError, RecursionError) as error:
        raise VerificationError("composition_result_invalid") from error


class CompositionGuideCapture:
    def __init__(self):
        self.channels = {"stdout": bytearray(), "stderr": bytearray()}
        self.failures = runtime_support.Failures()

    def read_channel(self, stream, channel):
        try:
            while block := stream.read(4096):
                if len(self.channels[channel]) + len(block) > 65536:
                    self.failures.fail("composition_output_limit")
                elif self.failures.code is None:
                    self.channels[channel].extend(block)
        finally:
            stream.close()

    def result(self, provider, scenario):
        self.failures.check()
        return composition_result(bytes(self.channels["stdout"]), bytes(self.channels["stderr"]), provider, scenario)

    def clear(self):
        for channel in self.channels.values():
            channel.clear()


def groups(suite, provider=None):
    if suite == "all":
        return [
            ("quality", None), ("unit", None), ("integration", "sqlite"),
            ("integration", "postgres"), ("recovery", "both"), ("contract", "both"),
            ("browser", None), ("privacy", "both"), ("startup", None),
            ("failure-propagation", None),
        ]
    if suite == "integration" and provider == "both":
        return [("integration", "sqlite"), ("integration", "postgres")]
    return [(suite, provider or (
        "both" if suite in ("recovery", "contract", "privacy")
        else "sqlite" if suite == "integration" else None
    ))]


def safe_command(command):
    """Only repository-relative arguments and plain options may enter reports."""
    result = []
    for item in map(str, command):
        query = re.fullmatch(
            r"/\[\(Suite=(recovery|privacy)\)&\(Provider=(sqlite|postgres)\)(?:&\(Feature=composition\))?\]", item)
        if (item.startswith("/") and not query or ".." in Path(item).parts or len(item) > 180
                or not re.fullmatch(r"[-a-zA-Z0-9_./:=*]+", item)
                and not query
                or re.search(r"(?i)password|secret|canary|connection|string=|token=", item)):
            result.append("<private-argument>")
        else:
            result.append(item)
    return result


def owned_directory(root, relative):
    root = root.absolute()
    if root.resolve() != root:
        raise VerificationError("artifact_root_unsafe")
    current = root
    for part in Path(relative).parts:
        if part in (".", "..") or Path(part).is_absolute():
            raise VerificationError("artifact_directory_unsafe")
        current /= part
        if current.is_symlink():
            raise VerificationError("artifact_directory_unsafe")
        current.mkdir(exist_ok=True, mode=0o700)
        if not current.is_dir() or current.resolve() != current:
            raise VerificationError("artifact_directory_unsafe")
    return current


def discard(stream):
    try:
        while stream.read(65536):
            pass
    finally:
        stream.close()


def command_diagnostics(command):
    if list(command[:5]) == ["npm", "run", "test", "--workspace", "tests/e2e"]:
        return runtime_support.CommandDiagnostics("browser")
    if list(command) in (
        ["npm", "run", "dependencies:check"], ["python3", "scripts/check_dependencies.py"],
    ):
        return runtime_support.CommandDiagnostics("dependency", check_dependencies.FAILURE_CODES)
    native_projects = {
        STORAGE_INTEGRATION: ("storage", "Kofte.Storage.IntegrationTests"),
        "tests/contract/Kofte.Platform.ContractTests": ("contract", "Kofte.Platform.ContractTests"),
    }
    if (len(command) > 3 and list(command[:3]) == ["dotnet", "test", "--project"]
            and command[3] in native_projects):
        project = command[3]
        kind, prefix = native_projects[project]
        tests = set()
        for path in (ROOT / project).rglob("*.cs"):
            if {"bin", "obj"}.intersection(path.relative_to(ROOT).parts):
                continue
            source = path.read_text()
            namespace = re.search(r"^namespace (" + re.escape(prefix) + r"(?:\.[A-Za-z0-9_]+)*);",
                                  source, re.MULTILINE)
            if namespace:
                classes = re.findall(r"public (?:(?:abstract|sealed|partial|static) )*class ([A-Za-z0-9_]+)", source)
                owners = (*classes, *(f"{outer}+{inner}" for outer in classes for inner in classes if outer != inner))
                methods = re.findall(r"public (?:async )?(?:Task(?:<[^<>]+>)?|void) ([A-Za-z0-9_]+)\(", source)
                tests.update(f"{namespace[1]}.{owner}.{method}" for owner in owners for method in methods)
        codes = (
            "Busy", "Conflict", "TargetReserved", "PathRejected", "OwnerMismatch", "Missing", "Corrupt",
            "Incomplete", "ReconciliationRequired", "VersionUnsupported", "PrivilegeDenied",
            "Unavailable", "DeadlineExceeded", "Cancelled", "RequestInvalid",
        )
        return runtime_support.CommandDiagnostics(kind, (
            *codes, *("storage_" + re.sub(r"(?<!^)(?=[A-Z])", "_", code).lower() for code in codes),
            *(("composition_consumer_failed", "composition_consumer_output_limit",
               "composition_consumer_result_invalid") if kind == "contract" else ()),
        ), tests)
    return None


class OwnedProcesses:
    def __init__(self, root, environment, capture=None):
        self.root, self.environment = root, environment
        self.capture = capture
        self.children, self.readers = [], []
        self.stopped = False
        self.read_failures = runtime_support.Failures()

    def read(self, stream, channel="stdout"):
        try:
            if self.capture and (reader := getattr(self.capture, "read_channel", None)):
                reader(stream, channel)
            else:
                (self.capture.read if self.capture else discard)(stream)
        except Exception:
            self.read_failures.fail("owned_output_read_failed")

    def start(self, command, *, stdin=None):
        if self.stopped:
            raise VerificationError("owned_group_already_stopped")
        child = subprocess.Popen(
            command, cwd=self.root, env=self.environment, stdin=stdin,
            stdout=subprocess.PIPE, stderr=subprocess.PIPE, start_new_session=True,
        )
        self.children.append(child)
        for channel, stream in (("stdout", child.stdout), ("stderr", child.stderr)):
            reader = threading.Thread(
                target=self.read, args=(stream, channel), daemon=True,
            )
            reader.start()
            self.readers.append(reader)
        return child

    def check(self):
        self.read_failures.check()
        if self.capture:
            self.capture.failures.check()
        if any(child.poll() is not None for child in self.children):
            raise VerificationError("owned_runtime_exited")

    def stop(self):
        if self.stopped:
            return
        for child in reversed(self.children):
            try:
                os.killpg(child.pid, signal.SIGTERM)
            except ProcessLookupError:
                pass
        deadline = time.monotonic() + 5
        for child in self.children:
            try:
                child.wait(timeout=max(0.01, deadline - time.monotonic()))
            except subprocess.TimeoutExpired:
                try:
                    os.killpg(child.pid, signal.SIGKILL)
                except ProcessLookupError:
                    pass
                try:
                    child.wait(timeout=5)
                except subprocess.TimeoutExpired as error:
                    raise VerificationError("owned_process_cleanup_timeout") from error
        for reader in self.readers:
            reader.join(timeout=5)
            if reader.is_alive():
                raise VerificationError("owned_output_cleanup_timeout")
        self.stopped = True
        self.read_failures.check()


class Report:
    def __init__(self, root, suite, provider, invocation):
        self.root = root
        self.directory = owned_directory(root, "artifacts/verification")
        revision = subprocess.run(
            ["git", "rev-parse", "HEAD"], cwd=root, capture_output=True, timeout=10,
        )
        revision_text = revision.stdout.decode(errors="replace").strip()
        if revision.returncode or not re.fullmatch(r"[a-f0-9]{40,64}", revision_text):
            raise VerificationError("revision_unavailable")
        try:
            pages = os.sysconf("SC_PHYS_PAGES")
            page_size = os.sysconf("SC_PAGE_SIZE")
        except (AttributeError, OSError, ValueError) as error:
            raise VerificationError("physical_memory_unavailable") from error
        if any(type(value) is not int or value <= 0 for value in (pages, page_size)):
            raise VerificationError("physical_memory_unavailable")
        self.path = self.directory / f"{suite}-{provider or 'none'}-{uuid.uuid4().hex}.json"
        self.began = time.monotonic()
        self.data = {
            "schemaVersion": 1, "revision": revision_text, "suite": suite,
            "provider": provider, "scope": "all" if invocation == "all" else "targeted",
            "completeGate": False, "start": utcnow(), "end": None, "durationSeconds": None,
            "environment": {
                "os": platform.system(), "osRelease": platform.release(),
                "architecture": platform.machine(), "logicalCpus": os.cpu_count(),
                "memoryBytes": pages * page_size,
                "python": platform.python_version(),
            },
            "toolVersions": {"python": platform.python_version(), **{
                tool: None for tool in dev.VERSIONS
            }},
            "steps": [], "outcome": "running",
        }
        self.save()

    def save(self):
        # Unique filenames plus O_NOFOLLOW prevent replacing a local symlink.
        flags = os.O_WRONLY | os.O_CREAT | os.O_TRUNC | getattr(os, "O_NOFOLLOW", 0)
        with os.fdopen(os.open(self.path, flags, 0o600), "w") as stream:
            json.dump(self.data, stream, indent=2)
            stream.write("\n")

    def finish(self, passed):
        self.data.update(
            end=utcnow(), durationSeconds=round(time.monotonic() - self.began, 3),
            outcome="passed" if passed else "failed",
        )
        self.save()


class Runner:
    def __init__(self, root, report, environment):
        self.root, self.report, self.environment = root, report, environment

    def operation(self, name, action, command=()):
        began = time.monotonic()
        step = {
            "name": name, "command": safe_command(command), "start": utcnow(),
            "outcome": "running", "exitCode": None,
        }
        self.report.data["steps"].append(step)
        failure = None
        try:
            action(step)
        except VerificationError as error:
            failure = error
        except dev.DevError as error:
            failure = VerificationError(error.code)
        except runtime_support.RuntimeFailure as error:
            failure = VerificationError(error.code)
        except FileNotFoundError:
            failure = VerificationError("tool_or_input_missing")
        except (OSError, ValueError, KeyError, TypeError):
            failure = VerificationError("verification_input_invalid")
        except (KeyboardInterrupt, SystemExit):
            failure = VerificationError("verification_interrupted")
        except Exception:
            failure = VerificationError("verification_internal_error")
        finally:
            step.update(
                end=utcnow(), durationSeconds=round(time.monotonic() - began, 3),
                outcome="failed" if failure else "passed",
            )
            if failure:
                step.update(failureCode=failure.code, failureCorrelation=uuid.uuid4().hex)
            self.report.save()
            print(json.dumps({
                "suite": self.report.data["suite"], "step": name,
                "outcome": step["outcome"],
                **({"failureCode": failure.code} if failure else {}),
            }), flush=True)
        if failure:
            raise failure

    def command(self, name, command, *, environment=None, timeout=900, health_check=None, capture=None,
                diagnostics=None):
        def run(step, diagnostic, actual_command, storage_report=None):
            processes = OwnedProcesses(self.root, environment or self.environment, capture=capture or diagnostic)
            try:
                child = processes.start(actual_command, stdin=subprocess.DEVNULL)
                try:
                    if health_check is None:
                        step["exitCode"] = child.wait(timeout=timeout)
                    else:
                        deadline = time.monotonic() + timeout
                        while True:
                            health_check()
                            remaining = deadline - time.monotonic()
                            if remaining <= 0:
                                raise subprocess.TimeoutExpired(command, timeout)
                            try:
                                step["exitCode"] = child.wait(timeout=min(0.05, remaining))
                                break
                            except subprocess.TimeoutExpired:
                                continue
                        health_check()
                except subprocess.TimeoutExpired as error:
                    raise VerificationError("command_timeout") from error
                if step["exitCode"]:
                    raise VerificationError("command_failed")
            finally:
                try:
                    processes.stop()
                finally:
                    if diagnostic:
                        if step["exitCode"] not in (None, 0):
                            if storage_report is not None:
                                diagnostic.read_storage_report(storage_report)
                            step.update(diagnostic.safe_fields())
                        diagnostic.clear()
            if diagnostic:
                diagnostic.failures.check()

        def execute(step):
            diagnostic = diagnostics or command_diagnostics(command)
            if diagnostic and diagnostic.kind in ("storage", "contract"):
                with private_root(self.root) as directory:
                    step["command"] = safe_command([
                        *command, "--report-xunit-xml",
                        "--report-xunit-xml-filename", "storage.xml",
                        "--results-directory", "<private-results>",
                    ])
                    run(step, diagnostic, [
                        *command, "--report-xunit-xml",
                        "--report-xunit-xml-filename", "storage.xml",
                        "--results-directory", str(directory),
                    ], directory / "storage.xml")
            else:
                run(step, diagnostic, command)
        self.operation(name, execute, command)

    def versions(self):
        for tool, expected in dev.VERSIONS.items():
            def probe(step, tool=tool, expected=expected):
                try:
                    result = subprocess.run(
                        [tool, "--version"], cwd=self.root, env=self.environment,
                        capture_output=True, timeout=30,
                    )
                except subprocess.TimeoutExpired as error:
                    raise VerificationError("tool_version_timeout") from error
                step["exitCode"] = result.returncode
                if result.returncode or result.stdout.decode(errors="replace").strip() != expected:
                    raise VerificationError("tool_version_invalid")
                self.report.data["toolVersions"][tool] = expected
            self.operation(f"{tool}-version", probe, [tool, "--version"])

    def composition_guide(self, provider, scenario, environment):
        capture = CompositionGuideCapture()
        name = f"composition-guide-{provider}-{scenario}"
        try:
            self.command(name, [
                "dotnet", "run", "--project", "tests/contract/Kofte.Composition.Consumer",
                "--no-build", "-c", "Release", "--", "--provider", provider, "--scenario", scenario,
            ], environment=environment, timeout=600, capture=capture)
            self.operation(name + "-observations", lambda step: step.update(
                composition=capture.result(provider, scenario)))
        finally:
            capture.clear()

    def build_runtime(self):
        self.command("runtime-host-build", [
            "dotnet", "build", "Kofte.slnx", "--no-restore", "-c", "Release", "-warnaserror",
        ])
        self.command("runtime-web-build", ["npm", "run", "build", "--workspace", "apps/web"])


def free_port():
    with socket.socket() as listener:
        listener.bind(("127.0.0.1", 0))
        return listener.getsockname()[1]


@contextmanager
def private_root(root):
    directory = owned_directory(root, "artifacts/verification/.private")
    os.chmod(directory, 0o700)
    path = Path(tempfile.mkdtemp(prefix="run-", dir=directory)).resolve()
    os.chmod(path, 0o700)
    retain = False
    try:
        yield path
    except VerificationError as error:
        retain = error.code.startswith(("postgres_cleanup", "runtime_cleanup"))
        raise
    finally:
        if not retain:
            shutil.rmtree(path)


class PostgresLease:
    """The C# fixture alone owns database/role/Compose teardown."""

    def __init__(self, runner, directory, external_connection=None):
        self.runner, self.path = runner, directory / "lease.json"
        self.external_connection = external_connection
        self.child, self.readers = None, []
        self.events = queue.Queue()
        self.event_failures = runtime_support.Failures()
        self.stopping = False
        self.events_closed = False

    def start(self):
        environment = dict(child_environment(self.runner.environment), KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE="true")
        if self.external_connection is not None:
            environment["KOFTE_POSTGRES_CONNECTION"] = self.external_connection
        self.child = subprocess.Popen(
            ["dotnet", FIXTURE, "--postgres-fixture", str(self.path)],
            cwd=self.runner.root, env=environment, stdin=subprocess.PIPE,
            stdout=subprocess.PIPE, stderr=subprocess.PIPE, start_new_session=True,
        )
        def events():
            try:
                for line in self.child.stdout:
                    event = json.loads(line)
                    code = event.get("eventCode")
                    if code not in {
                        "postgres_fixture_ready", "postgres_fixture_paused", "postgres_fixture_resumed",
                    }:
                        raise ValueError
                    self.events.put(code)
            except Exception:
                self.event_failures.fail("postgres_fixture_protocol_failed")
            finally:
                self.events_closed = True
                try:
                    self.child.stdout.close()
                except Exception:
                    self.event_failures.fail("postgres_fixture_protocol_failed")

        def errors():
            try:
                discard(self.child.stderr)
            except Exception:
                self.event_failures.fail("postgres_fixture_protocol_failed")

        for target, args in ((events, ()), (errors, ())):
            reader = threading.Thread(target=target, args=args, daemon=True)
            reader.start()
            self.readers.append(reader)
        self.wait_event("postgres_fixture_ready", 180)
        details = self.path.lstat()
        if (self.path.is_symlink() or not stat.S_ISREG(details.st_mode)
                or details.st_mode & 0o077 or details.st_uid != os.getuid()):
            raise VerificationError("postgres_lease_unsafe")
        lease = json.loads(self.path.read_text())
        if not lease.get("RunId") or not isinstance(lease.get("ConnectionString"), str):
            raise VerificationError("postgres_lease_invalid")
        return lease["ConnectionString"]

    def check(self):
        self.event_failures.check()
        if not self.stopping and (self.events_closed or self.child.poll() is not None):
            raise VerificationError("postgres_fixture_failed")

    def wait_event(self, expected, timeout):
        deadline = time.monotonic() + timeout
        while time.monotonic() < deadline:
            self.check()
            try:
                code = self.events.get(timeout=0.05)
            except queue.Empty:
                continue
            if code != expected:
                raise VerificationError("postgres_fixture_event_unexpected")
            return
        raise VerificationError("postgres_fixture_timeout")

    def command(self, action):
        if action not in ("pause", "resume"):
            raise VerificationError("postgres_fixture_command_invalid")
        self.check()
        try:
            self.child.stdin.write((action + "\n").encode())
            self.child.stdin.flush()
        except (OSError, ValueError) as error:
            raise VerificationError("postgres_fixture_command_failed") from error
        self.wait_event(
            "postgres_fixture_paused" if action == "pause" else "postgres_fixture_resumed", 10,
        )

    def stop(self):
        if self.child is None:
            return
        self.stopping = True
        try:
            self.child.stdin.close()
        except BrokenPipeError:
            pass
        try:
            self.child.wait(timeout=75)
        except subprocess.TimeoutExpired:
            try:
                os.killpg(self.child.pid, signal.SIGTERM)
            except ProcessLookupError:
                pass
            try:
                self.child.wait(timeout=10)
            except subprocess.TimeoutExpired as error:
                try:
                    os.killpg(self.child.pid, signal.SIGKILL)
                except ProcessLookupError:
                    pass
                self.child.wait(timeout=5)
                raise VerificationError("postgres_cleanup_timeout") from error
        for reader in self.readers:
            reader.join(timeout=5)
            if reader.is_alive():
                raise VerificationError("postgres_cleanup_reader_timeout")
        self.event_failures.check()
        if self.child.returncode:
            raise VerificationError("postgres_cleanup_failed")


@contextmanager
def runtime(runner, provider, disposable, external_connection=None, privacy_canary=None):
    if provider == "postgres" and not disposable:
        raise VerificationError("disposable_postgres_opt_in_required")
    runner.build_runtime()
    with private_root(runner.root) as directory:
        host_port, web_port = free_port(), free_port()
        while host_port == web_port:
            web_port = free_port()
        host, web = f"http://127.0.0.1:{host_port}", f"http://127.0.0.1:{web_port}"
        environment = dict(
            child_environment(runner.environment), KOFTE_PROFILE="local-sqlite",
            KOFTE_HOST_URL=host, KOFTE_WEB_URL=web, KOFTE_BACKEND_ORIGIN=host,
            KOFTE_DATA_ROOT=str(directory / "data"), ASPNETCORE_ENVIRONMENT="Development",
        )
        (directory / "data").mkdir(mode=0o700)
        control = directory / "control"
        control.mkdir(mode=0o700)
        run_id = uuid.uuid4().hex
        lease = PostgresLease(runner, directory, external_connection) if provider == "postgres" else None
        failures = runtime_support.Failures()
        capture = None
        web_processes = None
        worker = None
        hosts = []
        host_state = {"current": None}
        sqlite = runtime_support.SqliteAvailability(directory) if provider == "sqlite" else None
        pg_unavailable = [False]
        try:
            if lease:
                connection = [None]

                def provision(_):
                    connection[0] = lease.start()
                runner.operation("postgres-fixture-start", provision, [
                    "dotnet", FIXTURE, "--postgres-fixture", "<private-lease>",
                ])
                environment.update(
                    KOFTE_PROFILE="test-postgres", KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE="true",
                    KOFTE_POSTGRES_CONNECTION=connection[0],
                )
            forbidden = [str(directory), str(directory / "data"), privacy_canary]
            if lease:
                forbidden += [
                    connection[0], runtime_support.connection_password(connection[0]),
                ]
            capture = runtime_support.OutputCapture(failures, forbidden)
            if provider == "sqlite":
                runner.command("isolated-store-init", ["dotnet", HOST, "--store", "init"],
                               environment=environment, timeout=30, capture=capture)
            web_processes = OwnedProcesses(runner.root, environment, capture=capture)

            def start_host(timeout=10):
                if host_state["current"] is not None:
                    raise runtime_support.RuntimeFailure("runtime_host_state_invalid")
                processes = OwnedProcesses(runner.root, environment, capture=capture)
                hosts.append(processes)
                host_state["current"] = processes
                processes.start(["dotnet", HOST], stdin=subprocess.DEVNULL)
                dev.wait_ready(f"{host}/health/ready", processes, time.monotonic() + timeout, "host")

            def stop_host():
                if host_state["current"] is None:
                    raise runtime_support.RuntimeFailure("runtime_host_state_invalid")
                processes = host_state["current"]
                host_state["current"] = None
                processes.stop()

            def unavailable_store():
                if sqlite:
                    sqlite.unavailable()
                else:
                    pg_unavailable[0] = True
                    lease.command("pause")

            def restore_store():
                if sqlite:
                    sqlite.restore()
                else:
                    lease.command("resume")
                    pg_unavailable[0] = False

            runner.operation("owned-host-start", lambda _: start_host(285), ["dotnet", HOST])
            web_command = [
                "node", "node_modules/next/dist/bin/next", "start", "apps/web",
                "--hostname", "127.0.0.1", "--port", str(web_port),
            ]

            def launch_web(_):
                web_processes.start(web_command, stdin=subprocess.DEVNULL)
                dev.wait_ready(
                    f"{web}/backend/health/ready", web_processes, time.monotonic() + 285, "web",
                )
            runner.operation("owned-web-start", launch_web, web_command)

            def health():
                failures.check()
                web_processes.check()
                if host_state["current"] is not None:
                    host_state["current"].check()
                if lease:
                    lease.check()

            worker = runtime_support.ControlWorker(control, run_id, {
                "unavailable-store": unavailable_store, "restore-store": restore_store,
                "stop-host": stop_host, "start-host": start_host,
            }, health, failures)
            worker.start()
            test_environment = dict(environment, BOOTSTRAP_TEST_CONTROL=str(control),
                                    BOOTSTRAP_TEST_RUN_ID=run_id)
            if privacy_canary:
                test_environment["BOOTSTRAP_PRIVACY_CANARY"] = privacy_canary
            yield runtime_support.Session(test_environment, directory, worker)
        finally:
            cleanup_failure = None

            def cleanup(name, action, retain_code=None):
                nonlocal cleanup_failure
                try:
                    runner.operation(name, action)
                except VerificationError as error:
                    retained = ("postgres_cleanup", "runtime_cleanup")
                    if retain_code and not error.code.startswith(retained):
                        error = VerificationError(retain_code)
                    if cleanup_failure is None or (
                        error.code.startswith(retained) and not cleanup_failure.code.startswith(retained)
                    ):
                        cleanup_failure = error

            if worker:
                def stop_worker(step):
                    worker.stop()
                    step["actions"] = worker.observations
                cleanup("runtime-control-stop", stop_worker, "runtime_cleanup_worker_failed")
            if sqlite:
                def restore_sqlite(_):
                    try:
                        sqlite.cleanup()
                    except Exception as error:
                        raise VerificationError("runtime_cleanup_store_restore_failed") from error
                cleanup("runtime-store-restore", restore_sqlite)
            if lease and pg_unavailable[0]:
                def resume_postgres(_):
                    try:
                        lease.command("resume")
                    except Exception as error:
                        raise VerificationError("postgres_cleanup_resume_failed") from error
                cleanup("postgres-fixture-resume", resume_postgres)
            for index, processes in enumerate(hosts):
                cleanup(f"owned-host-stop-{index + 1}", lambda _, owned=processes: owned.stop(),
                        "runtime_cleanup_process_failed")
            if web_processes:
                cleanup("owned-web-stop", lambda _: web_processes.stop(), "runtime_cleanup_process_failed")
            if lease:
                cleanup("postgres-fixture-stop", lambda _: lease.stop(), "postgres_cleanup_failed")
            if capture:
                def scan(step):
                    step.update(capture.scan_and_clear())
                cleanup("runtime-output-scan", scan)
            if cleanup_failure:
                raise cleanup_failure


def browser_specs(root):
    return sorted(
        path.name for path in (root / "tests/e2e").glob("*.spec.ts")
        if path.name not in ("startup.spec.ts", "privacy.spec.ts")
    )


def startup_observations(directory, cycles, *, composition=False):
    def unique_fields(pairs):
        result = {}
        for name, value in pairs:
            if name in result:
                raise ValueError
            result[name] = value
        return result

    try:
        if (type(cycles) is not int or not 2 <= cycles <= 10
                or directory.resolve() != directory.absolute() or not directory.is_dir()):
            raise ValueError
        candidates = list(directory.rglob(
            "composition-startup-observations.json" if composition else "startup-observations.json"))
        if not candidates:
            raise VerificationError("startup_observations_missing")
        if len(candidates) != 1:
            raise ValueError
        path = candidates[0]
        if path.resolve() != path.absolute() or not path.resolve().is_relative_to(directory):
            raise ValueError
        flags = os.O_RDONLY | getattr(os, "O_NOFOLLOW", 0) | getattr(os, "O_NONBLOCK", 0)
        with os.fdopen(os.open(path, flags), encoding="utf-8") as stream:
            details = os.fstat(stream.fileno())
            if (not stat.S_ISREG(details.st_mode) or details.st_uid != os.getuid()
                    or details.st_nlink != 1 or details.st_size > 16384):
                raise ValueError
            content = stream.read(16385)
            if len(content) > 16384:
                raise ValueError
            values = json.loads(content, object_pairs_hook=unique_fields)
        if not isinstance(values, list) or len(values) != cycles:
            raise ValueError
        fields = ("cycle", "readySeconds", "pageSeconds", "fixtureMatches") + (
            ("totalSeconds", "modulesPublished") if composition else ())
        samples = []
        for cycle, sample in enumerate(values, 1):
            if (not isinstance(sample, dict) or set(sample) != set(fields)
                    or type(sample["cycle"]) is not int or sample["cycle"] != cycle
                    or sample["fixtureMatches"] is not True):
                raise ValueError
            for name, limit in (("readySeconds", 285), ("pageSeconds", 2)) + (
                    (("totalSeconds", 300),) if composition else ()):
                duration = sample[name]
                if (isinstance(duration, bool) or not isinstance(duration, (int, float))
                        or not 0 <= duration < limit or not math.isfinite(duration)):
                    raise ValueError
            if composition and (sample["modulesPublished"] is not True or
                                sample["totalSeconds"] < sample["readySeconds"] + sample["pageSeconds"]):
                raise ValueError
            samples.append({name: sample[name] for name in fields})
        return samples
    except (OSError, ValueError, TypeError) as error:
        raise VerificationError("startup_observations_invalid") from error


def provider_environment(runner, arguments, provider):
    environment = dict(runner.environment)
    if provider == "postgres":
        def opt_in(_):
            if not arguments.disposable_postgres:
                raise VerificationError("disposable_postgres_opt_in_required")
        runner.operation("postgres-disposable-guard", opt_in)
        environment["KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE"] = "true"
        if arguments.postgres_connection is not None:
            environment["KOFTE_POSTGRES_CONNECTION"] = arguments.postgres_connection
    return environment


def integration_command(provider, suite=None, project=INTEGRATION, feature=None):
    feature_query = f"&(Feature={feature})" if feature else ""
    selector = (
        ["--filter-query", f"/[(Suite={suite})&(Provider={provider}){feature_query}]"] if suite
        else ["--filter-trait", f"Provider={provider}"]
    )
    return [
        "dotnet", "test", "--project", project, "--no-restore", "-c", "Release",
        *selector, "--minimum-expected-tests", "1",
    ]


def execute_suite(runner, suite, provider, arguments):
    runner.operation("workspace-inventory", lambda _: workspace_inventory(runner.root))
    if suite in ("quality", "unit", "integration", "recovery", "contract", "privacy"):
        runner.operation("storage-inventory", lambda _: storage_inventory(runner.root))
        runner.operation("composition-inventory", lambda _: composition_inventory(runner.root))
    if suite in ("quality", "unit", "contract"):
        def platform_inventory(_):
            if any(not (runner.root / path).is_file() for path in PLATFORM_REQUIRED_FILES):
                raise VerificationError("platform_target_missing")
        runner.operation("platform-inventory", platform_inventory)
    if suite in UNIMPLEMENTED and any(
        not (runner.root / path).is_file() for path in REQUIRED_SUITE_FILES[suite]
    ):
        def unavailable(_):
            raise VerificationError(UNIMPLEMENTED[suite])
        runner.operation(f"{suite}-implementation", unavailable)
    runner.versions()
    if suite == "quality":
        for name, command in QUALITY_COMMANDS:
            runner.command(name, command)
    elif suite == "unit":
        runner.command("host-unit", [
            "dotnet", "test", "--project", "tests/unit/Kofte.Host.Tests",
            "--no-restore", "-c", "Release", "--minimum-expected-tests", "1",
        ])
        runner.command("platform-unit", [
            "dotnet", "test", "--project", "tests/unit/Kofte.Platform.Tests",
            "--no-restore", "-c", "Release", "--minimum-expected-tests", "1",
        ])
        runner.command("composition-host-unit", [
            "dotnet", "test", "--project", "tests/unit/Kofte.Host.Tests", "--no-restore", "-c", "Release",
            "--filter-class", "*Composition*", "--filter-class", "*ModuleLifecycleTests",
            "--filter-class", "*ModuleConfigurationTests", "--filter-class", "*ModuleSelectionTests",
            "--minimum-expected-tests", "80",
        ])
        runner.command("composition-platform-unit", [
            "dotnet", "test", "--project", "tests/unit/Kofte.Platform.Tests", "--no-restore", "-c", "Release",
            "--filter-class", "*Composition*", "--filter-class", "*ContributionClaimTests",
            "--minimum-expected-tests", "83",
        ])
        for workspace in ("apps/web", "packages/bootstrap-contract"):
            runner.command(f"{workspace}-unit", ["npm", "run", "test:unit", "--workspace", workspace])
        runner.command("python-tests", [
            "python3", "-m", "unittest", "discover", "-s", "scripts/tests", "-v",
        ])
    elif suite == "integration":
        runner.command(
            f"integration-{provider}", integration_command(provider),
            environment=provider_environment(runner, arguments, provider),
        )
        runner.command(
            f"storage-integration-{provider}", integration_command(provider, project=STORAGE_INTEGRATION),
            environment=provider_environment(runner, arguments, provider),
        )
        runner.command(f"composition-integration-{provider}", [
            *integration_command(provider, project=STORAGE_INTEGRATION)[:-2],
            "--filter-class", "*Composition*", "--minimum-expected-tests", "50",
        ], environment=provider_environment(runner, arguments, provider))
    elif suite in ("recovery", "contract", "privacy"):
        if suite == "contract":
            public_environment = provider_environment(
                runner, arguments, "postgres" if provider in ("postgres", "both") else "sqlite",
            )
            public_selector = [] if provider == "both" else [
                "--filter-not-trait", f"Provider={'postgres' if provider == 'sqlite' else 'sqlite'}",
            ]
            runner.command("manifest-author-build", [
                "dotnet", "build", "tests/contract/Kofte.Manifest.Consumer",
                "--no-restore", "-c", "Release", "-warnaserror",
            ])
            runner.command("storage-author-build", [
                "dotnet", "build", "tests/contract/Kofte.Storage.Consumer",
                "--no-restore", "-c", "Release", "-warnaserror",
            ])
            for project in ("Kofte.Composition.TestModule", "Kofte.Composition.Consumer"):
                runner.command("composition-build-" + project, [
                    "dotnet", "build", "tests/contract/" + project, "--no-restore", "-c", "Release", "-warnaserror",
                ])
            runner.command("platform-public-contracts", [
                "dotnet", "test", "--project", "tests/contract/Kofte.Platform.ContractTests",
                "--no-restore", "-c", "Release", *public_selector, "--filter-not-class", "*Composition*",
                "--minimum-expected-tests", "1",
            ], environment=public_environment)
            runner.command("composition-public-contracts", [
                "dotnet", "test", "--project", "tests/contract/Kofte.Platform.ContractTests",
                "--no-restore", "-c", "Release", *public_selector, "--filter-class", "*Composition*",
                "--minimum-expected-tests", "84" if provider == "both" else "1",
            ], environment=public_environment)
            runner.command("module-manifest-schema", [
                "npm", "run", "test", "--workspace", "tests/contract", "--", "module-manifest.test.ts",
            ])
        providers = ("sqlite", "postgres") if provider == "both" else (provider,)
        if "postgres" in providers:
            provider_environment(runner, arguments, "postgres")
        for selected in providers:
            if suite == "contract":
                runner.command(f"native-openapi-policy-{selected}", [
                    "dotnet", "test", "--project", INTEGRATION, "--no-restore", "-c", "Release",
                    "--filter-class", "*OpenApiTests", "--filter-trait", f"Provider={selected}",
                    "--minimum-expected-tests", "1",
                ], environment=provider_environment(runner, arguments, selected))
            if suite == "recovery":
                runner.command(
                    f"recovery-{selected}-native", integration_command(selected, "recovery"),
                    environment=provider_environment(runner, arguments, selected),
                )
            if suite in ("recovery", "privacy"):
                environment = provider_environment(runner, arguments, selected)
                runner.command(
                    f"storage-{suite}-{selected}",
                    integration_command(selected, suite, project=STORAGE_INTEGRATION), environment=environment,
                )
                runner.command(f"storage-public-{suite}-{selected}", [
                    "dotnet", "test", "--project", "tests/contract/Kofte.Platform.ContractTests",
                    "--no-restore", "-c", "Release", "--filter-query",
                    f"/[(Suite={suite})&(Provider={selected})]", "--minimum-expected-tests", "1",
                ], environment=environment)
                runner.command(f"composition-{suite}-{selected}", [
                    *integration_command(selected, suite, project=STORAGE_INTEGRATION,
                                         feature="composition")[:-2],
                    "--minimum-expected-tests", "4" if suite == "recovery" else "1",
                ], environment=environment)
                runner.command(f"composition-public-{suite}-{selected}", [
                    *integration_command(selected, suite, project="tests/contract/Kofte.Platform.ContractTests",
                                         feature="composition")[:-2],
                    "--minimum-expected-tests", "4" if suite == "recovery" else "1",
                ], environment=environment)
            if suite == "contract":
                for scenario in (*COMPOSITION_OBSERVATIONS, "all"):
                    runner.composition_guide(selected, scenario, provider_environment(runner, arguments, selected))
            canary = f"bootstrap-privacy-canary-{uuid.uuid4().hex}" if suite == "privacy" else None
            with runtime(
                runner, selected, arguments.disposable_postgres,
                arguments.postgres_connection, privacy_canary=canary,
            ) as session:
                if suite in ("contract", "privacy"):
                    files = (
                        ["bootstrap.test.ts", "adapter.test.ts"] if suite == "contract"
                        else ["privacy.test.ts"]
                    )
                    runner.command(f"{suite}-{selected}-http", [
                        "npm", "run", "test", "--workspace", "tests/contract", "--",
                        "--no-file-parallelism", *files,
                    ], environment=session.environment, health_check=session.check)
                if suite in ("recovery", "privacy"):
                    runner.command(f"{suite}-{selected}-browser", [
                        "npm", "run", "test", "--workspace", "tests/e2e", "--",
                        f"{suite}.spec.ts", "--workers=1", "--output",
                        str(session.directory / "playwright"),
                    ], environment=session.environment, health_check=session.check)
        if suite == "privacy":
            runner.command("privacy-supervisor-capture", [
                "python3", "scripts/run_python_tests.py", "--pattern", "test_dev_output.py",
            ], environment=dict(
                runner.environment, BOOTSTRAP_PRIVACY_CANARY=f"bootstrap-privacy-canary-{uuid.uuid4().hex}",
            ))
    elif suite == "browser":
        specs = browser_specs(runner.root)
        if not specs:
            runner.operation("browser-tests", lambda _: fail("browser_tests_unimplemented"))
        with runtime(runner, "sqlite", False) as session:
            runner.command("browser-tests", [
                "npm", "run", "test", "--workspace", "tests/e2e", "--", *specs,
            ], environment=session.environment, health_check=session.check)
    elif suite == "startup":
        runner.build_runtime()
        runner.command("startup-storage-fixture-build", [
            "dotnet", "build", "tests/contract/Kofte.Storage.Consumer",
            "--no-restore", "-c", "Release", "-warnaserror",
        ])
        runner.command("startup-composition-fixture-build", [
            "dotnet", "build", "tests/contract/Kofte.Composition.Consumer",
            "--no-restore", "-c", "Release", "-warnaserror",
        ])
        output_root = owned_directory(runner.root, "tests/e2e/test-results")
        directory = output_root / f"verify-startup-{uuid.uuid4().hex}"
        directory.mkdir(mode=0o700)
        runner.command("startup-warm-restarts", [
            "npm", "run", "test", "--workspace", "tests/e2e", "--", "startup.spec.ts",
            "--grep-invert", "configured-modules", "--output", str(directory),
        ], environment=dict(runner.environment, BOOTSTRAP_WARM_STARTS=str(arguments.warm_starts)),
            timeout=arguments.warm_starts * 320 + 60)
        runner.operation("startup-observations", lambda step: step.update(
            configuredCycles=arguments.warm_starts,
            observations=startup_observations(directory, arguments.warm_starts),
        ))
        directory = output_root / f"verify-composition-startup-{uuid.uuid4().hex}"
        directory.mkdir(mode=0o700)
        runner.command("startup-composition-restarts", [
            "npm", "run", "test", "--workspace", "tests/e2e", "--", "startup.spec.ts",
            "--grep", "configured-modules", "--output", str(directory),
        ], environment=runner.environment, timeout=700)
        runner.operation("startup-composition-observations", lambda step: step.update(
            configuredCycles=2, observations=startup_observations(directory, 2, composition=True),
        ))
    elif suite == "failure-propagation":
        def failure_opt_in(_):
            if not arguments.disposable_postgres:
                fail("disposable_postgres_opt_in_required")
        runner.operation("failure-disposable-guard", failure_opt_in)
        path = runner.root / "tests/integration/tooling/test_failure_propagation.py"
        spec = importlib.util.spec_from_file_location("kofte_failure_exercises", path)
        module = importlib.util.module_from_spec(spec)
        runner.operation("failure-harness-load", lambda _: spec.loader.exec_module(module))
        runner.operation("failure-exercise-harness", lambda _: module.run(
            runner, __import__(__name__, fromlist=[""]),
        ))
    else:
        runner.operation("suite-handler", lambda _: fail("suite_handler_unimplemented"))


def fail(code):
    raise VerificationError(code)


def main(argv=None, root=ROOT, environment=None):
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--suite", choices=("all", *SUITES), required=True)
    parser.add_argument("--provider", choices=("sqlite", "postgres", "both"))
    parser.add_argument("--disposable-postgres", action="store_true")
    parser.add_argument("--warm-starts", type=int, choices=range(2, 11), default=10)
    arguments = parser.parse_args(argv)
    if arguments.provider and arguments.suite not in ("integration", "recovery", "contract", "privacy", "all"):
        parser.error("--provider applies only to provider suites")
    if arguments.suite == "all" and arguments.provider:
        parser.error("--suite all always covers both providers")
    source_environment = dict(os.environ if environment is None else environment)
    arguments.postgres_connection = source_environment.get("KOFTE_POSTGRES_CONNECTION")
    reports = []
    for suite, provider in groups(arguments.suite, arguments.provider):
        report = Report(root, suite, provider, arguments.suite)
        passed = False
        interrupted = False
        try:
            with private_root(root) as scratch:
                environment = child_environment(source_environment) | {
                    "TMPDIR": str(scratch), "TMP": str(scratch), "TEMP": str(scratch),
                }
                runner = Runner(root, report, environment)
                execute_suite(runner, suite, provider, arguments)
            passed = True
        except VerificationError as error:
            interrupted = error.code == "verification_interrupted"
        except (Exception, KeyboardInterrupt, SystemExit) as error:
            def unexpected(_):
                raise error
            try:
                Runner(root, report, {}).operation("suite-runtime", unexpected)
            except VerificationError as failure:
                interrupted = failure.code == "verification_interrupted"
        finally:
            report.finish(passed)
            reports.append(report)
        if interrupted:
            break
    complete = arguments.suite == "all" and all(r.data["outcome"] == "passed" for r in reports)
    for report in reports:
        report.data["completeGate"] = complete
        report.save()
    return 0 if all(r.data["outcome"] == "passed" for r in reports) else 1


if __name__ == "__main__":
    def interrupted(_number, _frame):
        raise KeyboardInterrupt

    signal.signal(signal.SIGTERM, interrupted)
    try:
        raise SystemExit(main())
    except (VerificationError, OSError, subprocess.TimeoutExpired, KeyboardInterrupt):
        print(json.dumps({"eventCode": "verification_setup_failed"}), flush=True)
        raise SystemExit(1)
