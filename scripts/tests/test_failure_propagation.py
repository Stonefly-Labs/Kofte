import copy
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

from scripts import verify


SPEC = importlib.util.spec_from_file_location(
    "failure_exercises_under_test",
    verify.ROOT / "tests/integration/tooling/test_failure_propagation.py",
)
exercises = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(exercises)


class FailurePropagationTests(unittest.TestCase):
    def setUp(self):
        base = verify.ROOT / "artifacts/python-tests"
        base.mkdir(parents=True, exist_ok=True)
        self.directory = tempfile.TemporaryDirectory(dir=base)
        self.addCleanup(self.directory.cleanup)
        self.root = Path(self.directory.name).resolve()
        self.source, self.checkout = self.root / "source", self.root / "checkout"
        self.source.mkdir()
        self.checkout.mkdir()

    def evidence(self):
        return {
            "revision": "a" * 40, "suite": "quality", "provider": None, "scope": "targeted",
            "completeGate": False, "start": "2026-10-21T00:00:00+00:00",
            "end": "2026-10-21T00:00:01+00:00", "durationSeconds": 1,
            "toolVersions": {"python": "3.11.0"}, "environment": {"os": "Linux"},
            "outcome": "failed", "rawOutput": "credential-canary",
            "steps": [{
                "name": "python-tests",
                "command": ["python3", "-m", "unittest", "discover", "-s", "scripts/tests", "-v"],
                "start": "2026-10-21T00:00:00+00:00", "end": "2026-10-21T00:00:01+00:00",
                "durationSeconds": 1, "outcome": "failed", "exitCode": 1,
                "failureCode": "command_failed", "failureCorrelation": "b" * 32,
                "stderr": "credential-canary",
            }],
        }

    def checked(self, report, exit_code=1):
        return exercises.checked_evidence(
            report, "quality", None, "python-tests", self.evidence()["steps"][0]["command"],
            exit_code, verify,
        )

    def test_overlay_is_a_separate_copy_and_never_copies_private_or_runtime_files(self):
        source = self.source / "scripts/probe.py"
        source.parent.mkdir()
        source.write_text("candidate_source = True\n")
        for name in (".env", "artifacts/private.json", "apps/web/.next/cache.json",
                     "src/Example/obj/generated.cs", "src/Example/appsettings.local.json"):
            path = self.source / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text("credential-canary")
        result = exercises.overlay(self.source, self.checkout, [
            "scripts/probe.py", ".env", "artifacts/private.json", "apps/web/.next/cache.json",
            "src/Example/obj/generated.cs", "src/Example/appsettings.local.json",
        ], verify.VerificationError)
        copied = self.checkout / "scripts/probe.py"
        self.assertEqual(source.read_bytes(), copied.read_bytes())
        self.assertNotEqual(source.stat().st_ino, copied.stat().st_ino)
        copied.write_text("fault_only_in_clone = True\n")
        self.assertEqual("candidate_source = True\n", source.read_text())
        self.assertEqual(1, result["overlayFiles"])
        self.assertFalse((self.checkout / ".env").exists())
        self.assertFalse((self.checkout / "src").exists())

    def test_overlay_refuses_a_source_symlink_instead_of_reading_its_target(self):
        (self.source / "scripts").mkdir()
        sentinel = self.root / "outside"
        sentinel.write_text("credential-canary")
        (self.source / "scripts/probe.py").symlink_to(sentinel)
        with self.assertRaisesRegex(verify.VerificationError, "failure_overlay_path_unsafe"):
            exercises.overlay(self.source, self.checkout, ["scripts/probe.py"], verify.VerificationError)
        self.assertEqual("credential-canary", sentinel.read_text())

    def test_injected_fault_is_undone_even_when_the_exercise_fails(self):
        with self.assertRaisesRegex(RuntimeError, "exercise failed"):
            with exercises.injected(
                self.checkout, "scripts/tests/test_probe.py", "assert False\n", verify.VerificationError,
            ):
                self.assertTrue((self.checkout / "scripts/tests/test_probe.py").is_file())
                raise RuntimeError("exercise failed")
        self.assertFalse((self.checkout / "scripts/tests/test_probe.py").exists())
        self.assertEqual([], list(self.source.iterdir()))

    def test_expected_failure_requires_the_exact_named_real_command_and_nonzero_wrapper(self):
        self.checked(self.evidence())
        for mutation in ("wrong-step", "wrong-command", "missing-tool", "wrapper-pass"):
            with self.subTest(mutation=mutation):
                report = copy.deepcopy(self.evidence())
                exit_code = 1
                if mutation == "wrong-step":
                    report["steps"][0]["name"] = "restore"
                elif mutation == "wrong-command":
                    report["steps"][0]["command"] = ["echo", "simulated"]
                elif mutation == "missing-tool":
                    report["steps"][0]["failureCode"] = "tool_or_input_missing"
                else:
                    exit_code = 0
                with self.assertRaisesRegex(verify.VerificationError, "failure_did_not_reach_expected_command"):
                    self.checked(report, exit_code)

    def test_expected_failure_keeps_safe_correlation_but_does_not_retain_raw_logs(self):
        evidence = self.evidence()
        evidence["steps"][0].update(
            dependencyFailureCode="notice_unreviewed",
            sourceLocations=["accessibility.spec.ts:76:15"],
        )
        result = self.checked(evidence)
        self.assertNotIn("credential-canary", json.dumps(result))
        self.assertEqual("b" * 32, result["steps"][0]["failureCorrelation"])
        self.assertEqual("notice_unreviewed", result["steps"][0]["dependencyFailureCode"])
        self.assertEqual(["accessibility.spec.ts:76:15"], result["steps"][0]["sourceLocations"])
        for key, value in (
            ("dependencyFailureCode", "credential-canary"),
            ("sourceLocations", ["/private/accessibility.spec.ts:76:15"]),
        ):
            evidence["steps"][0][key] = value
            with self.assertRaisesRegex(verify.VerificationError, "failure_child_report_unsafe"):
                self.checked(evidence)
            evidence["steps"][0][key] = result["steps"][0][key]

    def test_failing_baseline_is_not_accepted_as_failure_propagation_evidence(self):
        with self.assertRaisesRegex(verify.VerificationError, "failure_baseline_not_green"):
            exercises.checked_evidence(self.evidence(), "quality", None, None, None, 1, verify)

    def test_missing_chromium_requires_the_real_browser_failure_and_exact_capture_refusal(self):
        evidence = self.evidence()
        evidence["suite"] = "browser"
        command = ["npm", "run", "test", "--workspace", "tests/e2e", "--", "identity.spec.ts"]
        primary = evidence["steps"][0]
        primary.update(name="browser-tests", command=command)
        cleanup = copy.deepcopy(primary)
        cleanup.update(name="identity-browser-canaries", command=[], exitCode=None,
                       failureCode="identity_browser_transport_canaries_invalid")
        evidence["steps"].append(cleanup)

        def checked(report, browser_unavailable=True):
            return exercises.checked_evidence(
                report, "browser", None, "browser-tests", command, 1, verify,
                browser_unavailable=browser_unavailable,
            )

        result = checked(evidence)
        self.assertEqual(["browser-tests", "identity-browser-canaries"],
                         [step["name"] for step in result["steps"]])
        self.assertNotIn("credential-canary", json.dumps(result))
        with self.assertRaisesRegex(verify.VerificationError, "failure_did_not_reach_expected_command"):
            checked(evidence, browser_unavailable=False)
        for mutation in ("missing-capture-refusal", "capture-only", "wrong-order",
                         "wrong-refusal", "extra-failure", "successful-browser"):
            changed = copy.deepcopy(evidence)
            if mutation == "missing-capture-refusal":
                changed["steps"].pop()
            elif mutation == "capture-only":
                changed["steps"].pop(0)
            elif mutation == "wrong-order":
                changed["steps"].reverse()
            elif mutation == "wrong-refusal":
                changed["steps"][-1]["failureCode"] = "runtime_cleanup_process_failed"
            elif mutation == "extra-failure":
                changed["steps"].append(copy.deepcopy(cleanup))
            else:
                changed["steps"][0].update(outcome="passed", exitCode=0)
            with self.subTest(mutation=mutation), self.assertRaisesRegex(
                    verify.VerificationError, "failure_did_not_reach_expected_command"):
                checked(changed)

    def test_native_success_counts_are_revalidated_without_impossible_class_or_skip_claims(self):
        evidence = self.evidence()
        evidence["steps"][0]["nativeEvidence"] = {"passed": 2, "skipped": 0, "requiredClasses": 1}
        self.assertEqual(evidence["steps"][0]["nativeEvidence"],
                         self.checked(evidence)["steps"][0]["nativeEvidence"])
        for invalid in (
            {"passed": 0, "skipped": 0}, {"passed": True, "skipped": 0},
            {"passed": 2, "skipped": 1}, {"passed": 2, "skipped": False},
            {"passed": 1, "skipped": 0, "requiredClasses": 2},
            {"passed": 2, "skipped": 0, "requiredClasses": True},
            {"passed": 2, "skipped": 0, "extra": "credential-canary"},
        ):
            with self.subTest(evidence=invalid):
                evidence["steps"][0]["nativeEvidence"] = invalid
                with self.assertRaisesRegex(verify.VerificationError, "failure_child_report_unsafe"):
                    self.checked(evidence)

    def test_native_failure_evidence_is_allowlisted_again_inside_isolated_baselines(self):
        evidence = self.evidence()
        command = ["dotnet", "test", "--project", verify.STORAGE_INTEGRATION]
        name = sorted(verify.command_diagnostics(command).tests)[0]
        evidence["steps"][0].update(
            command=command, failedStorageTests=[name], storageFailureCodes=["Busy"],
            storageDiagnosticCode="unavailable",
            storageFailureOperations=["acquire"],
            storageFailureScenarios=["valid-dependency", "session-boundary"],
        )

        def checked():
            return exercises.checked_evidence(
                evidence, "quality", None, "python-tests", command, 1, verify,
            )

        result = checked()
        self.assertEqual([name], result["steps"][0]["failedStorageTests"])
        self.assertEqual(["Busy"], result["steps"][0]["storageFailureCodes"])
        self.assertEqual("unavailable", result["steps"][0]["storageDiagnosticCode"])
        self.assertEqual(["acquire"], result["steps"][0]["storageFailureOperations"])
        self.assertEqual(["valid-dependency", "session-boundary"],
                         result["steps"][0]["storageFailureScenarios"])
        self.assertNotIn("credential-canary", json.dumps(result))
        for key, value in (
            ("failedStorageTests", ["credential-canary"]),
            ("failedStorageTests", [name + "(credential-canary)"]),
            ("failedStorageTests", [name, name]),
            ("storageFailureCodes", ["credential-canary"]),
            ("storageFailureCodes", []),
            ("storageDiagnosticCode", "credential-canary"),
            ("storageFailureOperations", ["credential-canary"]),
            ("storageFailureScenarios", ["credential-canary"]),
            ("storageFailureScenarios", ["valid-dependency", "valid-dependency"]),
        ):
            with self.subTest(key=key, value=value):
                evidence["steps"][0][key] = value
                with self.assertRaisesRegex(verify.VerificationError, "failure_child_report_unsafe"):
                    checked()
                evidence["steps"][0][key] = result["steps"][0][key]
        evidence["steps"][0]["command"] = ["echo", "untrusted"]
        with self.assertRaisesRegex(verify.VerificationError, "failure_child_report_unsafe"):
            checked()

    def test_private_native_reporting_preserves_exact_failure_command_verification(self):
        evidence = self.evidence()
        command = [
            "dotnet", "test", "--project", verify.INTEGRATION, "--no-restore", "-c", "Release",
            "--filter-trait", "Provider=postgres", "--minimum-expected-tests", "1",
        ]
        reported = verify.safe_command([
            *command, "--report-xunit-xml", "--report-xunit-xml-filename", "storage.xml",
            "--results-directory", "<private-results>",
        ])
        evidence["steps"][0]["command"] = reported

        def checked():
            return exercises.checked_evidence(
                evidence, "quality", None, "python-tests", command, 1, verify,
            )

        self.assertEqual(reported, checked()["steps"][0]["command"])
        for original, replacement in (
            ("Provider=postgres", "Provider=sqlite"),
            ("storage.xml", "foreign.xml"),
            ("--report-xunit-xml", "--unrelated-option"),
        ):
            with self.subTest(original=original):
                evidence["steps"][0]["command"] = [
                    replacement if value == original else value for value in reported
                ]
                with self.assertRaisesRegex(verify.VerificationError, "failure_did_not_reach_expected_command"):
                    checked()

    def test_unavailable_postgres_requires_all_three_exact_serial_phase_failures(self):
        commands = verify.integration_commands("postgres")
        evidence = self.evidence()
        evidence.update(suite="integration", provider="postgres")
        evidence["steps"] = [
            dict(self.evidence()["steps"][0], name=name, command=command)
            for name, command in commands
        ]

        def checked(report):
            return exercises.checked_evidence(
                report, "integration", "postgres", commands[0][0], commands[0][1], 1, verify,
                expected_followups=commands[1:],
            )

        self.assertEqual(3, len(checked(evidence)["steps"]))
        reported = copy.deepcopy(evidence)
        for step in reported["steps"]:
            step["command"] = verify.safe_command([
                *step["command"], "--report-xunit-xml", "--report-xunit-xml-filename", "storage.xml",
                "--results-directory", "<private-results>",
            ])
        self.assertEqual(3, len(checked(reported)["steps"]))
        for mutation in ("missing", "extra", "reordered", "wrong-command", "missing-tool", "timeout"):
            with self.subTest(mutation=mutation):
                invalid = copy.deepcopy(evidence)
                if mutation == "missing":
                    invalid["steps"].pop()
                elif mutation == "extra":
                    invalid["steps"].append(copy.deepcopy(invalid["steps"][-1]))
                elif mutation == "reordered":
                    invalid["steps"].reverse()
                elif mutation == "wrong-command":
                    invalid["steps"][1]["command"] = verify.integration_command("sqlite")
                elif mutation == "missing-tool":
                    invalid["steps"][1]["failureCode"] = "tool_or_input_missing"
                else:
                    invalid["steps"][2]["failureCode"] = "command_timeout"
                with self.assertRaisesRegex(verify.VerificationError, "failure_did_not_reach_expected_command"):
                    checked(invalid)

    def test_host_failure_evidence_is_revalidated_inside_isolated_reports(self):
        evidence = self.evidence()
        command = ["dotnet", "test", "--project", verify.INTEGRATION]
        name = sorted(verify.command_diagnostics(command).tests)[0]
        location = "tests/integration/Kofte.Host.IntegrationTests/Identity/OidcBudgetTests.cs:133"
        evidence["steps"][0].update(
            command=command, failedHostTests=[name],
            hostFailureCodes=["Busy"], hostDiagnosticCode="unavailable",
            hostFailureLocations=[location],
            hostFailureStatuses=["expected_303_actual_503"],
            hostBrowserPhases=[{
                "phase": "identity_browser_profile_capture_request_failed",
                "launch": 2, "elapsedMilliseconds": 4300,
            }],
        )

        def checked():
            return exercises.checked_evidence(
                evidence, "quality", None, "python-tests", command, 1, verify,
            )

        result = checked()
        for key in ("failedHostTests", "hostFailureCodes", "hostDiagnosticCode",
                    "hostFailureLocations", "hostFailureStatuses", "hostBrowserPhases"):
            self.assertEqual(evidence["steps"][0][key], result["steps"][0][key])
        self.assertNotIn("credential-canary", json.dumps(result))
        for key, value in (
            ("failedHostTests", [name + "(credential-canary)"]),
            ("failedHostTests", [name, name]),
            ("hostFailureCodes", ["credential-canary"]),
            ("hostDiagnosticCode", "credential-canary"),
            ("hostFailureLocations", ["/private/credential-canary/" + location]),
            ("hostFailureLocations", [location, location]),
            ("hostFailureLocations", [location.rsplit(":", 1)[0] + ":999999"]),
            ("hostFailureLocations", ["tests/integration/Unknown.cs:1"]),
            ("hostFailureStatuses", ["expected_303_actual_123"]),
            ("hostFailureStatuses", ["expected_303_actual_credential_canary"]),
            ("hostFailureStatuses", ["expected_303_actual_503", "expected_303_actual_503"]),
            ("hostFailureStatuses", []),
            ("hostBrowserPhases", []),
            ("hostBrowserPhases", [{"phase": "credential-canary", "launch": 2, "elapsedMilliseconds": 1}]),
            ("hostBrowserPhases", [{"phase": "identity_browser_profile_capture_request_failed",
                                   "launch": True, "elapsedMilliseconds": 1}]),
            ("hostBrowserPhases", [{"phase": "identity_browser_profile_capture_request_failed",
                                   "launch": 2, "elapsedMilliseconds": 60001}]),
            ("hostBrowserPhases", [{"phase": "identity_browser_profile_capture_request_failed",
                                   "launch": 2, "elapsedMilliseconds": 1, "extra": "credential-canary"}]),
            ("hostBrowserPhases", evidence["steps"][0]["hostBrowserPhases"] * 65),
        ):
            with self.subTest(key=key):
                evidence["steps"][0][key] = value
                with self.assertRaisesRegex(verify.VerificationError, "failure_child_report_unsafe"):
                    checked()
                evidence["steps"][0][key] = result["steps"][0][key]
        evidence["steps"][0]["command"] = ["echo", "untrusted"]
        with self.assertRaisesRegex(verify.VerificationError, "failure_child_report_unsafe"):
            checked()

    def test_public_failure_evidence_is_revalidated_inside_isolated_reports(self):
        evidence = self.evidence()
        command = ["dotnet", "test", "--project", "tests/contract/Kofte.Platform.ContractTests"]
        name = ("Kofte.Platform.ContractTests.Composition.CompositionPublicContractTests"
                "+PostgresCompositionPublicContractTests."
                "AllRequiresEveryActualStandaloneScenarioAndItsCompleteSemanticEvidence")
        evidence["steps"][0].update(
            command=command, failedContractTests=[name],
            contractFailureCodes=["composition_consumer_failed"],
            contractDiagnosticCode="unavailable",
        )

        def checked():
            return exercises.checked_evidence(
                evidence, "quality", None, "python-tests", command, 1, verify,
            )

        result = checked()
        for key in ("failedContractTests", "contractFailureCodes", "contractDiagnosticCode"):
            self.assertEqual(evidence["steps"][0][key], result["steps"][0][key])
        for key, value in (
            ("failedContractTests", [name + "(credential-canary)"]),
            ("failedContractTests", [name, name]),
            ("contractFailureCodes", ["credential-canary"]),
            ("contractDiagnosticCode", "credential-canary"),
        ):
            with self.subTest(key=key):
                evidence["steps"][0][key] = value
                with self.assertRaisesRegex(verify.VerificationError, "failure_child_report_unsafe"):
                    checked()
                evidence["steps"][0][key] = result["steps"][0][key]
        evidence["steps"][0]["command"] = ["echo", "untrusted"]
        with self.assertRaisesRegex(verify.VerificationError, "failure_child_report_unsafe"):
            checked()

    def test_checkout_environment_uses_private_config_and_only_explicit_shared_caches(self):
        environment = exercises.isolated_environment({
            "HOME": "/operator-home", "PATH": "",
            "PYTHONPATH": "/root-checkout", "NODE_PATH": "/root-checkout/node_modules",
            "KOFTE_POSTGRES_CONNECTION": "credential-canary",
            "OTEL_EXPORTER_OTLP_HEADERS": "credential-canary",
            "npm_config_userconfig": "/operator-private-npmrc",
            "NUGET_PLUGIN_PATHS": "/operator-private-plugin",
            "DOCKER_CONTEXT": "operator-context",
            **{key: "/operator-private-config" for key in (
                "XDG_CONFIG_HOME", "XDG_CACHE_HOME", "XDG_DATA_HOME", "XDG_STATE_HOME", "XDG_RUNTIME_DIR",
            )},
        }, self.root / "isolation", verify)
        self.assertNotIn("PYTHONPATH", environment)
        self.assertNotIn("NODE_PATH", environment)
        self.assertNotIn("DOCKER_CONTEXT", environment)
        self.assertNotIn("KOFTE_POSTGRES_CONNECTION", environment)
        self.assertNotIn("NUGET_PLUGIN_PATHS", environment)
        self.assertTrue(Path(environment["HOME"]).is_relative_to(self.root))
        self.assertTrue(Path(environment["npm_config_userconfig"]).is_relative_to(self.root))
        self.assertNotEqual(environment["npm_config_userconfig"], environment["npm_config_globalconfig"])
        self.assertEqual("/operator-home/.nuget/packages", environment["NUGET_PACKAGES"])
        self.assertEqual("/operator-home/.npm", environment["npm_config_cache"])
        for key in ("XDG_CONFIG_HOME", "XDG_CACHE_HOME", "XDG_DATA_HOME", "XDG_STATE_HOME", "XDG_RUNTIME_DIR"):
            path = Path(environment[key])
            self.assertTrue(path.is_relative_to(self.root / "isolation"))
            self.assertEqual(0o700, path.stat().st_mode & 0o777)


if __name__ == "__main__":
    unittest.main()
