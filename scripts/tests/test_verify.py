from contextlib import contextmanager, redirect_stdout
import copy
import io
import json
from pathlib import Path
import shutil
import signal
import subprocess
import tempfile
import unittest
from unittest.mock import Mock, patch
import xml.etree.ElementTree as ET

from scripts import verify


class VerifyTests(unittest.TestCase):
    def setUp(self):
        base = verify.ROOT / "artifacts/python-tests"
        base.mkdir(parents=True, exist_ok=True)
        self.temporary = tempfile.TemporaryDirectory(dir=base)
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name).resolve()
        for name in (*verify.PLATFORM_REQUIRED_FILES, *verify.STORAGE_REQUIRED_FILES,
                     *verify.COMPOSITION_REQUIRED_FILES, *verify.IDENTITY_REQUIRED_FILES):
            path = self.root / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text("required platform fixture")
        (self.root / "tests/integration/Kofte.TestSupport/StorageScenarioCatalog.cs").write_text(
            " ".join(json.dumps(name) for name in verify.STORAGE_SCENARIOS))
        (self.root / "tests/integration/Kofte.TestSupport/CompositionScenarioCatalog.cs").write_text(
            "\n".join(f'["{name}"] = Required()' for name in verify.COMPOSITION_OBSERVATIONS))
        (self.root / "tests/integration/Kofte.TestSupport/Identity/IdentityScenarioCatalog.cs").write_text(
            " ".join(json.dumps(name) for name in verify.IDENTITY_SCENARIOS))
        (self.root / "Kofte.slnx").write_text(
            "\n".join((*verify.COMPOSITION_REQUIRED_FILES, *verify.IDENTITY_REQUIRED_FILES)))
        for workspace, scripts in verify.REQUIRED_SCRIPTS.items():
            path = self.root / workspace / "package.json"
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text(json.dumps({
                "workspaces": list(verify.REQUIRED_SCRIPTS)[1:],
                "scripts": {name: "real-tool run" for name in scripts},
            }))
        self.output = io.StringIO()
        output = redirect_stdout(self.output)
        output.__enter__()
        self.addCleanup(output.__exit__, None, None, None)

    def report(self, suite="unit", invocation="unit"):
        with patch.object(verify.subprocess, "run", return_value=subprocess.CompletedProcess(
            [], 0, b"a" * 40 + b"\n", b"",
        )):
            return verify.Report(self.root, suite, None, invocation)

    def test_if_present_does_not_hide_a_missing_mandatory_workspace_script(self):
        verify.workspace_inventory(self.root)
        path = self.root / "tests/e2e/package.json"
        manifest = json.loads(path.read_text())
        del manifest["scripts"]["typecheck"]
        path.write_text(json.dumps(manifest))
        with self.assertRaisesRegex(verify.VerificationError, "required_workspace_script_missing"):
            verify.workspace_inventory(self.root)
        root_manifest = json.loads((self.root / "package.json").read_text())
        root_manifest["workspaces"].remove("tests/e2e")
        (self.root / "package.json").write_text(json.dumps(root_manifest))
        with self.assertRaisesRegex(verify.VerificationError, "workspace_inventory_drift"):
            verify.workspace_inventory(self.root)

    def test_missing_storage_project_or_public_scenario_fails_before_tool_invocation(self):
        runner = verify.Runner(self.root, self.report("integration"), {})
        project = self.root / verify.STORAGE_INTEGRATION / "Kofte.Storage.IntegrationTests.csproj"
        project.unlink()
        with patch.object(runner, "versions") as versions, \
                self.assertRaisesRegex(verify.VerificationError, "storage_target_missing"):
            verify.execute_suite(runner, "integration", "sqlite", Mock())
        versions.assert_not_called()
        project.write_text("restored required fixture")
        for name in ("Providers/StorageMaintenanceDiagnosticsTests.cs", "Recovery/RecoveryDiagnosticsTests.cs"):
            diagnostics = self.root / verify.STORAGE_INTEGRATION / name
            diagnostics.unlink()
            with self.assertRaisesRegex(verify.VerificationError, "storage_target_missing"):
                verify.storage_inventory(self.root)
            diagnostics.write_text("restored required capture regressions")
        catalog = self.root / "tests/integration/Kofte.TestSupport/StorageScenarioCatalog.cs"
        catalog.write_text(catalog.read_text().replace('"stopped-restore"', '"omitted"'))
        with self.assertRaisesRegex(verify.VerificationError, "storage_scenario_missing"):
            verify.storage_inventory(self.root)

    def test_storage_recovery_privacy_and_zero_case_child_failures_cannot_publish_green(self):
        for name, exit_code in (("storage-recovery-sqlite", 2), ("storage-privacy-postgres", 2),
                                ("storage-integration-sqlite", 8)):
            runner = verify.Runner(self.root, self.report("integration"), {})
            with self.subTest(name=name), self.assertRaisesRegex(verify.VerificationError, "command_failed"):
                runner.command(name, ["python3", "-c", f"raise SystemExit({exit_code})"])
            self.assertEqual("failed", runner.report.data["steps"][-1]["outcome"])

    def test_native_success_requires_actual_complete_non_skipped_results_before_private_cleanup(self):
        diagnostic = verify.runtime_support.CommandDiagnostics("host")
        for content, code in (
            ("", "native_evidence_invalid"),
            ("<assemblies />", "native_evidence_cases_missing"),
            ('<assemblies><test result="Skip" /></assemblies>', "native_evidence_skipped"),
            ('<assemblies><test result="Fail" /></assemblies>', "native_evidence_not_passed"),
            ('<assemblies><test result="Pass" /><error /></assemblies>', "native_evidence_not_passed"),
        ):
            runner = verify.Runner(self.root, self.report(), {})
            script = (
                "import pathlib,sys; p=pathlib.Path(sys.argv[sys.argv.index('--results-directory')+1]);"
                f"(p/'storage.xml').write_text({content!r})"
            )
            with self.subTest(code=code), self.assertRaisesRegex(verify.VerificationError, f"^{code}$"):
                runner.command("native-evidence", ["python3", "-c", script], diagnostics=diagnostic)
            self.assertFalse(any((self.root / "artifacts/verification/.private").iterdir()))
        runner = verify.Runner(self.root, self.report(), {})
        script = (
            "import pathlib,sys; p=pathlib.Path(sys.argv[sys.argv.index('--results-directory')+1]);"
            "(p/'storage.xml').write_text('<assemblies><test result=\"Pass\" /></assemblies>')"
        )
        runner.command("native-evidence", ["python3", "-c", script], diagnostics=diagnostic)
        self.assertEqual({"passed": 1, "skipped": 0}, runner.report.data["steps"][-1]["nativeEvidence"])
        self.assertFalse(any((self.root / "artifacts/verification/.private").iterdir()))

    def test_native_success_cannot_satisfy_a_larger_required_case_inventory(self):
        path = self.root / "storage.xml"
        path.write_text('<assemblies><test result="Pass" /></assemblies>')
        with self.assertRaisesRegex(verify.runtime_support.RuntimeFailure, "^native_evidence_cases_missing$"):
            verify.runtime_support.CommandDiagnostics("host").native_success(path, 2)

    def test_native_timings_aggregate_only_source_selected_class_names_before_discarding_xml(self):
        path = self.root / "storage.xml"
        path.write_text(
            '<assemblies><test type="Kofte.Host.IdentityFailureTests" result="Pass" time="1.25" />'
            '<test type="Kofte.Host.IdentityFailureTests" result="Pass" time="2.5" />'
            '<test type="credential-canary" method="private-canary" result="Pass" time="0.75" />'
            '</assemblies>'
        )
        diagnostic = verify.runtime_support.CommandDiagnostics("host")
        diagnostic.native_success(path, 3, ("IdentityFailureTests",))
        self.assertEqual({
            "status": "measured", "totalCaseSeconds": 4.5,
            "requiredClassSeconds": {"IdentityFailureTests": 3.75}, "otherCaseSeconds": 0.75,
        }, diagnostic.native_timing)
        self.assertNotIn("canary", json.dumps(diagnostic.native_timing))

    def test_missing_or_invalid_native_times_are_explicitly_unavailable_not_fabricated_zeroes(self):
        path = self.root / "storage.xml"
        for value in (None, "private-canary", "NaN", "Infinity", "-1", "86401"):
            attribute = "" if value is None else f' time="{value}"'
            path.write_text(f'<assemblies><test result="Pass"{attribute} /></assemblies>')
            diagnostic = verify.runtime_support.CommandDiagnostics("host")
            with self.subTest(value=value):
                self.assertEqual({"passed": 1, "skipped": 0}, diagnostic.native_success(path, 1))
                self.assertEqual({
                    "status": "unavailable",
                    "code": "native_timing_missing" if value is None else "native_timing_invalid",
                }, diagnostic.native_timing)

    def test_runner_retains_safe_native_timing_after_private_xml_and_diagnostic_cleanup(self):
        runner = verify.Runner(self.root, self.report(), {})
        diagnostic = verify.runtime_support.CommandDiagnostics("host")
        script = (
            "import pathlib,sys; p=pathlib.Path(sys.argv[sys.argv.index('--results-directory')+1]);"
            "(p/'storage.xml').write_text('<assemblies><test result=\"Pass\" time=\"1.25\" /></assemblies>')"
        )
        runner.command("native-timing", ["python3", "-c", script], diagnostics=diagnostic)
        self.assertEqual({
            "status": "measured", "totalCaseSeconds": 1.25,
            "requiredClassSeconds": {}, "otherCaseSeconds": 1.25,
        }, runner.report.data["steps"][-1]["nativeTiming"])
        self.assertIsNone(diagnostic.native_timing)
        self.assertFalse(any((self.root / "artifacts/verification/.private").iterdir()))

    def test_native_timeout_retains_known_prior_failure_without_disclosing_private_xml(self):
        name = sorted(verify.command_diagnostics(
            ["dotnet", "test", "--project", verify.STORAGE_INTEGRATION]).tests)[0]
        owner, method = name.rsplit(".", 1)
        diagnostic = verify.runtime_support.CommandDiagnostics("storage", ["Unavailable"], [name])
        xml = (
            f'<assemblies><test type="{owner}" method="{method}" result="Fail">'
            '<failure><message>StorageException: Unavailable credential-canary</message></failure>'
            '</test></assemblies>'
        )
        script = (
            "import pathlib,sys,time; p=pathlib.Path(sys.argv[sys.argv.index('--results-directory')+1]);"
            f"(p/'storage.xml').write_text({xml!r}); time.sleep(30)"
        )
        runner = verify.Runner(self.root, self.report(), {})
        with self.assertRaisesRegex(verify.VerificationError, "^command_timeout$"):
            runner.command("native-timeout", ["python3", "-c", script],
                           diagnostics=diagnostic, timeout=1)
        step = runner.report.data["steps"][-1]
        self.assertEqual([name], step["failedStorageTests"])
        self.assertEqual(["Unavailable"], step["storageFailureCodes"])
        self.assertNotIn("credential-canary", runner.report.path.read_text())
        self.assertFalse(any((self.root / "artifacts/verification/.private").iterdir()))

    def test_native_success_cannot_omit_a_required_identity_class_behind_retained_passes(self):
        path = self.root / "storage.xml"
        diagnostic = verify.runtime_support.CommandDiagnostics("host")
        path.write_text('<assemblies><test type="RetainedTests" result="Pass" /></assemblies>')
        with self.assertRaisesRegex(verify.runtime_support.RuntimeFailure,
                                    "^native_evidence_required_case_missing$"):
            diagnostic.native_success(path, 1, ("IdentityFailureTests",))
        path.write_text('<assemblies><test type="Kofte.Host.IdentityFailureTests" result="Pass" /></assemblies>')
        self.assertEqual({"passed": 1, "skipped": 0, "requiredClasses": 1},
                         diagnostic.native_success(path, 1, ("IdentityFailureTests",)))

    def test_integration_and_recovery_cannot_omit_the_independent_restore_command_group(self):
        path = self.root / "storage.xml"
        for provider, prefix in (("sqlite", "Sqlite"), ("postgres", "Postgres")):
            for name, restore in (
                (name, prefix + group)
                for name in (f"integration-{provider}", f"recovery-{provider}-native")
                for group in ("IdentityRestoreCommandTests", "IdentityRestoreJournalTests")
            ):
                with self.subTest(name=name, group=restore):
                    required = verify.required_native_classes(name, [])
                    passed = [case for case in required if case != restore]
                    path.write_text("<assemblies>" + "".join(
                        f'<test type="Kofte.Host.Identity.{case}" result="Pass" />' for case in passed
                    ) + "</assemblies>")
                    diagnostic = verify.runtime_support.CommandDiagnostics("host")
                    with self.assertRaisesRegex(verify.runtime_support.RuntimeFailure,
                                                "^native_evidence_required_case_missing$"):
                        diagnostic.native_success(path, 1, required)
                    path.write_text(path.read_text().replace(
                        "</assemblies>", f'<test type="Kofte.Host.Identity.{restore}" result="Pass" /></assemblies>',
                    ))
                    self.assertEqual(len(required),
                                     diagnostic.native_success(path, 1, required)["requiredClasses"])

    def test_identity_inventory_requires_sources_scanners_scenarios_and_solution_build_edge(self):
        verify.identity_inventory(self.root)
        for name in verify.IDENTITY_REQUIRED_FILES:
            path = self.root / name
            original = path.read_text()
            path.unlink()
            runner = verify.Runner(self.root, self.report(), {})
            with self.subTest(name=name), patch.object(runner, "versions") as versions, \
                    self.assertRaisesRegex(verify.VerificationError, "^identity_target_missing$"):
                verify.execute_suite(runner, "unit", None, Mock())
            versions.assert_not_called()
            path.write_text(original)
        catalog = self.root / "tests/integration/Kofte.TestSupport/Identity/IdentityScenarioCatalog.cs"
        catalog.write_text(catalog.read_text().replace('"private-evidence"', '"omitted"'))
        with self.assertRaisesRegex(verify.VerificationError, "^identity_scenario_missing$"):
            verify.identity_inventory(self.root)
        for scenarios in (
            (*verify.IDENTITY_SCENARIOS, "unexpected-identity"),
            (*verify.IDENTITY_SCENARIOS, verify.IDENTITY_SCENARIOS[0]),
            tuple(reversed(verify.IDENTITY_SCENARIOS)),
        ):
            catalog.write_text(" ".join(json.dumps(name) for name in scenarios))
            with self.assertRaisesRegex(verify.VerificationError, "^identity_scenario_missing$"):
                verify.identity_inventory(self.root)
        catalog.write_text(" ".join(json.dumps(name) for name in verify.IDENTITY_SCENARIOS))
        solution = self.root / "Kofte.slnx"
        solution.write_text(solution.read_text().replace(
            "tests/contract/Kofte.Identity.Consumer/Kofte.Identity.Consumer.csproj", "omitted"))
        with self.assertRaisesRegex(verify.VerificationError, "^identity_build_target_missing$"):
            verify.identity_inventory(self.root)

    def test_independent_integration_projects_run_after_a_failed_child_but_the_suite_still_fails(self):
        for code in ("command_failed", "command_timeout"):
            with self.subTest(code=code):
                runner = verify.Runner(self.root, self.report("integration"), {})
                with patch.object(runner, "versions"), \
                        patch.object(runner, "command", side_effect=[
                            verify.VerificationError(code), None, None,
                        ]) as command, \
                        self.assertRaisesRegex(verify.VerificationError, f"^{code}$"):
                    verify.execute_suite(runner, "integration", "sqlite", Mock())
                self.assertEqual([
                    "integration-sqlite", "storage-integration-sqlite", "composition-integration-sqlite",
                ], [call.args[0] for call in command.call_args_list])

    def test_integration_cleanup_failure_stops_before_another_project_can_start(self):
        runner = verify.Runner(self.root, self.report("integration"), {})
        with patch.object(runner, "versions"), \
                patch.object(runner, "command", side_effect=verify.VerificationError(
                    "owned_process_cleanup_timeout")) as command, \
                self.assertRaisesRegex(verify.VerificationError, "^owned_process_cleanup_timeout$"):
            verify.execute_suite(runner, "integration", "sqlite", Mock())
        self.assertEqual(1, command.call_count)

    def test_composition_inventory_requires_every_project_capture_case_and_quality_build_edge(self):
        verify.composition_inventory(self.root)
        for name in verify.COMPOSITION_REQUIRED_FILES:
            path = self.root / name
            original = path.read_text()
            path.unlink()
            with self.subTest(name=name), self.assertRaisesRegex(
                    verify.VerificationError, "composition_target_missing"):
                verify.composition_inventory(self.root)
            path.write_text(original)
        catalog = self.root / "tests/integration/Kofte.TestSupport/CompositionScenarioCatalog.cs"
        catalog.write_text(catalog.read_text().replace('"private-diagnostics"', '"omitted"'))
        with self.assertRaisesRegex(verify.VerificationError, "composition_scenario_missing"):
            verify.composition_inventory(self.root)
        catalog.write_text("\n".join(f'["{name}"] = Required()' for name in verify.COMPOSITION_OBSERVATIONS))
        solution = self.root / "Kofte.slnx"
        solution.write_text(solution.read_text().replace(
            "tests/contract/Kofte.Composition.TestModule/Kofte.Composition.TestModule.csproj", "omitted"))
        with self.assertRaisesRegex(verify.VerificationError, "composition_build_target_missing"):
            verify.composition_inventory(self.root)

    @staticmethod
    def composition_payload(scenario="all"):
        selected = verify.COMPOSITION_OBSERVATIONS if scenario == "all" else {
            scenario: verify.COMPOSITION_OBSERVATIONS[scenario]}
        return {
            "schemaVersion": 1, "eventCode": "composition_scenarios_passed", "attempt": "a" * 32,
            "provider": "sqlite", "scenario": scenario, "status": "passed", "assertions": len(selected),
            "scenarios": {name: {"assertions": 1, "observations": dict(required)}
                          for name, required in selected.items()},
        }

    def test_composition_results_require_every_semantic_field_case_and_exact_assertion_sum(self):
        for scenario in (*verify.COMPOSITION_OBSERVATIONS, "all"):
            payload = self.composition_payload(scenario)
            self.assertEqual(payload["assertions"], verify.composition_result(
                json.dumps(payload).encode(), b"", "sqlite", scenario)["assertions"])
            for name, case in payload["scenarios"].items():
                omitted = json.loads(json.dumps(payload))
                del omitted["scenarios"][name]
                self.require_invalid_composition(omitted, scenario)
                for field, minimum in case["observations"].items():
                    for value in (None, minimum - 1, True, 2_147_483_648):
                        changed = json.loads(json.dumps(payload))
                        if value is None:
                            del changed["scenarios"][name]["observations"][field]
                        else:
                            changed["scenarios"][name]["observations"][field] = value
                        self.require_invalid_composition(changed, scenario)
            payload["assertions"] += 1
            self.require_invalid_composition(payload, scenario)
        payload = self.composition_payload()
        for case in payload["scenarios"].values():
            case["assertions"] = 2_147_483_647
        payload["assertions"] = sum(case["assertions"] for case in payload["scenarios"].values())
        self.require_invalid_composition(payload)

    def require_invalid_composition(self, payload, scenario="all"):
        with self.assertRaisesRegex(verify.VerificationError, "^composition_result_invalid$"):
            verify.composition_result(json.dumps(payload).encode(), b"", "sqlite", scenario)

    def test_composition_capture_rejects_other_provider_bad_envelopes_stderr_duplicate_and_oversize(self):
        for field, value in (("provider", "postgres"), ("schemaVersion", True), ("attempt", "private-canary"),
                             ("unknown", "private-canary"), ("status", "failed")):
            payload = self.composition_payload()
            payload[field] = value
            self.require_invalid_composition(payload)
        valid = json.dumps(self.composition_payload()).encode()
        for output, error in ((b"{}", b""), (valid, b"private-canary"),
                              (valid.replace(b'"schemaVersion": 1', b'"schemaVersion": 1,"schemaVersion": 1'), b""),
                              (valid + b" " * 65536, b"")):
            with self.assertRaisesRegex(verify.VerificationError, "^composition_result_invalid$"):
                verify.composition_result(output, error, "sqlite", "all")
        capture = verify.CompositionGuideCapture()
        capture.read_channel(io.BytesIO(b"x" * 65537), "stdout")
        with self.assertRaisesRegex(verify.runtime_support.RuntimeFailure, "composition_output_limit"):
            capture.result("sqlite", "all")
        capture.clear()
        self.assertFalse(any(capture.channels.values()))

    def test_composition_selectors_intersect_provider_and_suite_and_reject_zero_native_cases(self):
        for provider in ("sqlite", "postgres"):
            runner = verify.Runner(self.root, self.report("integration"), {})
            with patch.object(runner, "versions"), patch.object(runner, "command") as command:
                verify.execute_suite(runner, "integration", provider, Mock(
                    disposable_postgres=True, postgres_connection=None))
            selected = next(call for call in command.call_args_list
                            if call.args[0] == f"composition-integration-{provider}")
            self.assertIn("*Composition*", selected.args[1])
            self.assertIn(f"Provider={provider}", selected.args[1])
            self.assertEqual(["--minimum-expected-tests", "50"], selected.args[1][-2:])
            storage = next(call for call in command.call_args_list
                           if call.args[0] == f"storage-integration-{provider}")
            self.assertIn(f"Provider={provider}", storage.args[1])
            excluded = storage.args[1].index("--filter-not-class")
            self.assertEqual("*Composition*", storage.args[1][excluded + 1])
            self.assertNotIn("--filter-not-class", selected.args[1])
        runner = verify.Runner(self.root, self.report("contract"), {})
        with self.assertRaisesRegex(verify.VerificationError, "command_failed"):
            runner.command("composition-zero-cases", ["python3", "-c", "raise SystemExit(8)"])

    def test_storage_selectors_retain_provider_intersection_and_fail_on_zero_selected_cases(self):
        for provider in ("sqlite", "postgres"):
            for suite in ("recovery", "privacy"):
                command = verify.integration_command(provider, suite, project=verify.STORAGE_INTEGRATION)
                self.assertIn(verify.STORAGE_INTEGRATION, command)
                self.assertIn(f"/[(Suite={suite})&(Provider={provider})]", command)
                self.assertEqual(["--minimum-expected-tests", "1"], command[-2:])
                self.assertEqual(command, verify.safe_command(command))
                focused = verify.integration_command(
                    provider, suite, project=verify.STORAGE_INTEGRATION, feature="composition")
                self.assertIn(f"/[(Suite={suite})&(Provider={provider})&(Feature=composition)]", focused)
                self.assertNotIn("--filter-class", focused)
                self.assertNotIn("--filter-trait", focused)
                self.assertEqual(focused, verify.safe_command(focused))

    def test_native_fixture_parallelism_is_bounded_without_changing_provider_or_case_minimum(self):
        for cpus, threads in ((None, "1"), (1, "1"), (2, "2"), (18, "4")):
            with self.subTest(cpus=cpus), patch.object(verify.os, "cpu_count", return_value=cpus):
                command = verify.integration_command("postgres")
                index = command.index("--max-threads")
                self.assertEqual(threads, command[index + 1])
                self.assertIn("Provider=postgres", command)
                self.assertIn("Suite=browser", command)
                self.assertEqual(["--minimum-expected-tests", "1"], command[-2:])

    def test_actual_child_failure_is_named_nonzero_and_output_is_not_saved(self):
        memory_bytes = verify.os.sysconf("SC_PHYS_PAGES") * verify.os.sysconf("SC_PAGE_SIZE")
        report = self.report()
        runner = verify.Runner(self.root, report, {})
        with self.assertRaisesRegex(verify.VerificationError, "command_failed"):
            runner.command("failing-test", [
                "python3", "-c",
                "import sys; print('credential-canary'); print('/private/sensitive', file=sys.stderr); sys.exit(7)",
            ])
        report.finish(False)
        persisted = report.path.read_text()
        environment = json.loads(persisted)["environment"]
        self.assertIs(type(environment["memoryBytes"]), int)
        self.assertEqual(memory_bytes, environment["memoryBytes"])
        self.assertNotIn("credential", persisted)
        self.assertNotIn("/private/sensitive", persisted)
        self.assertEqual(7, report.data["steps"][0]["exitCode"])
        self.assertEqual("failing-test", report.data["steps"][0]["name"])
        self.assertEqual("failed", report.data["steps"][0]["outcome"])
        self.assertEqual("failed", report.data["outcome"])
        self.assertFalse(report.data["completeGate"])
        self.assertRegex(report.data["steps"][0]["failureCorrelation"], "^[a-f0-9]{32}$")

    def test_unavailable_or_invalid_physical_memory_cannot_publish_fabricated_report_metadata(self):
        for values in ((-1, 4096), (1024, 0), (True, 4096), (1024, 4096.0), (None, 4096)):
            with self.subTest(values=values), \
                    patch.object(verify.os, "sysconf", side_effect=values), \
                    self.assertRaisesRegex(verify.VerificationError, "physical_memory_unavailable"):
                self.report()
        for error in (OSError("private diagnostics"), ValueError("unsupported")):
            with self.subTest(error=type(error).__name__), \
                    patch.object(verify.os, "sysconf", side_effect=error), \
                    self.assertRaisesRegex(verify.VerificationError, "physical_memory_unavailable"):
                self.report()
        self.assertEqual([], list((self.root / "artifacts/verification").glob("*.json")))

    def test_failed_browser_command_keeps_only_whitelisted_locations_and_original_failure(self):
        report = self.report("browser")
        runner = verify.Runner(self.root, report, {})
        diagnostic = verify.command_diagnostics(["npm", "run", "test", "--workspace", "tests/e2e"])
        output = (
            "credential-canary expected secret received /private/owned-layout\n"
            " at /private/owned-layout/tests/e2e/accessibility.spec.ts:76:15\n"
            " at /private/owned-layout/tests/e2e/bootstrap.spec.ts:9:2\n"
            " at /private/owned-layout/tests/e2e/unreviewed.spec.ts:4:5\n"
            " at /private/owned-layout/tests/e2e/privacy.spec.ts:0:4\n"
            " at /private/owned-layout/tests/e2e/not-accessibility.spec.ts:5:6\n"
        )
        with self.assertRaisesRegex(verify.VerificationError, "command_failed"):
            runner.command("browser-tests", [
                "python3", "-c", f"import sys; print({output!r}, file=sys.stderr); sys.exit(7)",
            ], diagnostics=diagnostic)
        report.finish(False)
        step = json.loads(report.path.read_text())["steps"][-1]
        self.assertEqual(7, step["exitCode"])
        self.assertEqual("command_failed", step["failureCode"])
        self.assertEqual(["accessibility.spec.ts:76:15", "bootstrap.spec.ts:9:2"], step["sourceLocations"])
        for forbidden in ("credential-canary", "owned-layout", "unreviewed.spec.ts", "expected", "received"):
            self.assertNotIn(forbidden, report.path.read_text())
        self.assertEqual(set(), diagnostic.sources)

    def test_failed_dependency_command_exposes_only_its_allowlisted_checker_code(self):
        report = self.report("quality")
        runner = verify.Runner(self.root, report, {})
        diagnostic = verify.command_diagnostics(["npm", "run", "dependencies:check"])
        output = "\n".join((
            json.dumps({"eventCode": "credential_canary"}),
            json.dumps({"eventCode": "notice_drift", "message": "credential-canary /private/layout"}),
            json.dumps({"eventCode": ["credential-canary"]}),
            json.dumps({"eventCode": "notice_unreviewed"}),
        ))
        with self.assertRaisesRegex(verify.VerificationError, "command_failed"):
            runner.command("dependency-review", [
                "python3", "-c", f"import sys; print({output!r}, file=sys.stderr); sys.exit(3)",
            ], diagnostics=diagnostic)
        step = json.loads(report.path.read_text())["steps"][-1]
        self.assertEqual(3, step["exitCode"])
        self.assertEqual("command_failed", step["failureCode"])
        self.assertEqual("notice_unreviewed", step["dependencyFailureCode"])
        self.assertNotIn("credential", report.path.read_text())
        self.assertNotIn("/private/layout", report.path.read_text())
        self.assertEqual(set(), diagnostic.dependency_codes)

    def test_failed_storage_command_retains_only_source_known_symbols_and_fixed_codes(self):
        runner = verify.Runner(self.root, self.report("integration"), {})
        diagnostic = verify.command_diagnostics([
            "dotnet", "test", "--project", verify.STORAGE_INTEGRATION,
        ])
        name = ("Kofte.Storage.IntegrationTests.Registry.PostgresRegistrationConcurrencyTests."
                "TwentyRoundsOfTenSeparateProcessesKeepActualOwnershipUnique")
        owner, method = name.rsplit(".", 1)
        xml = (f'<assemblies><test type="{owner}" method="{method}" result="Fail" '
               'name="credential-canary"><failure><message>StorageException : DeadlineExceeded '
               '/private/credential-canary operation=acquire</message></failure></test>'
               '<test type="credential-canary" method="unknown" result="Fail"/></assemblies>')
        script = (
            "import sys; from pathlib import Path; "
            "directory=Path(sys.argv[sys.argv.index('--results-directory')+1]); "
            f"(directory/'storage.xml').write_text({xml!r}); "
            "print('/private/credential-canary'); sys.exit(2)"
        )
        with self.assertRaisesRegex(verify.VerificationError, "command_failed"):
            runner.command("storage-integration-postgres", [
                "python3", "-c", script,
            ], diagnostics=diagnostic)
        step = runner.report.data["steps"][-1]
        self.assertEqual([name], step["failedStorageTests"])
        self.assertEqual(["DeadlineExceeded"], step["storageFailureCodes"])
        self.assertEqual(["acquire"], step["storageFailureOperations"])
        self.assertEqual(["--results-directory", "<private-argument>"], step["command"][-2:])
        self.assertFalse(list((self.root / "artifacts/verification/.private").rglob("storage.xml")))
        self.assertNotIn("credential", runner.report.path.read_text())
        self.assertEqual(set(), diagnostic.failed_tests)
        self.assertEqual(set(), diagnostic.storage_codes)

    def test_failed_host_command_retains_known_case_without_private_framework_output(self):
        runner = verify.Runner(self.root, self.report("integration"), {})
        diagnostic = verify.command_diagnostics([
            "dotnet", "test", "--project", "tests/integration/Kofte.Host.IntegrationTests",
        ])
        self.assertIsNotNone(diagnostic)
        name = ("Kofte.Host.IntegrationTests.Identity.PostgresOidcBudgetTests."
                "DeliveredDnsChangesAreReadmittedBeforeTheRealTokenConnection")
        owner, method = name.rsplit(".", 1)
        location = "tests/integration/Kofte.Host.IntegrationTests/Identity/OidcBudgetTests.cs"
        fixture_location = "tests/integration/Kofte.TestSupport/Identity/LoginCommitFixture.cs"
        postgres_location = "tests/integration/Kofte.TestSupport/PostgresAssemblyFixture.cs"
        fixture_code = "identity_fixture_child_readiness_transport_ConnectionError_owner_opened"
        http_code = "identity_fixture_child_readiness_http_503_serving"
        postgres_code = "postgres_assembly_fixture_start_incomplete"
        xml = (f'<assemblies><test type="{owner}" method="{method}" result="Fail" '
               'name="credential-canary"><failure><message>StorageException : DeadlineExceeded '
               f'{fixture_code} {http_code} {postgres_code} postgres_credential_canary '
               'identity_fixture_child_readiness_credential_canary '
               f'/private/credential-canary</message><stack-trace>/private/credential-canary/{location}:line 133'
               f'\n/private/credential-canary/{fixture_location}:line 1300'
               f'\n/private/credential-canary/{postgres_location}:line 40'
               '</stack-trace>'
               '</failure></test><test type="credential-canary" method="unknown" result="Fail"/>'
               '</assemblies>')
        script = (
            "import sys; from pathlib import Path; "
            "directory=Path(sys.argv[sys.argv.index('--results-directory')+1]); "
            f"(directory/'storage.xml').write_text({xml!r}); "
            "print('/private/credential-canary'); sys.exit(2)"
        )
        with self.assertRaisesRegex(verify.VerificationError, "command_failed"):
            runner.command("host-integration-postgres", ["python3", "-c", script],
                           diagnostics=diagnostic)
        step = runner.report.data["steps"][-1]
        self.assertEqual([name], step["failedHostTests"])
        self.assertEqual(["DeadlineExceeded", http_code, fixture_code, postgres_code], step["hostFailureCodes"])
        self.assertEqual([location + ":133", fixture_location + ":1300", postgres_location + ":40"],
                         step["hostFailureLocations"])
        self.assertFalse(list((self.root / "artifacts/verification/.private").rglob("storage.xml")))
        self.assertNotIn("credential", runner.report.path.read_text())
        self.assertEqual(set(), diagnostic.failed_tests)
        self.assertEqual(set(), diagnostic.sources)

    def test_failed_identity_scenario_retains_only_exact_known_theory_arguments(self):
        name = ("Kofte.Platform.ContractTests.Identity.SqliteIdentityPrivacyGuideTests."
                "ActualIdentityJourneysRequireTheirPrivateCapturedEvidence")
        owner, method = name.rsplit(".", 1)
        diagnostic = verify.command_diagnostics([
            "dotnet", "test", "--project", "tests/contract/Kofte.Platform.ContractTests",
        ])
        root = ET.Element("assemblies")
        for result, selected_owner, display in (
            ("Fail", owner, name + '(scenario: \\"session-boundary\\")'),
            ("Pass", owner, name + '(scenario: \\"oidc-login\\")'),
            ("Fail", "unknown", name + '(scenario: \\"private-evidence\\")'),
            ("Fail", owner, name + '(scenario: \\"credential-canary\\")'),
            ("Fail", owner, name + '(scenario: \\"oidc-login\\", value: \\"credential-canary\\")'),
            ("Fail", owner, name + '(scenario: \\"oidc-login\\") credential-canary'),
        ):
            ET.SubElement(root, "test", type=selected_owner, method=method,
                          result=result, name=display)
        path = self.root / "storage.xml"
        ET.ElementTree(root).write(path)
        diagnostic.read_storage_report(path)
        self.assertEqual({
            "failedContractTests": [name],
            "contractFailureScenarios": ["session-boundary"],
        }, diagnostic.safe_fields())
        self.assertNotIn("credential-canary", json.dumps(vars(diagnostic), default=str))
        diagnostic.clear()
        self.assertEqual({}, diagnostic.safe_fields())

    def test_zero_exit_browser_without_actual_structured_evidence_cannot_pass(self):
        report = self.report("browser")
        runner = verify.Runner(self.root, report, {})
        diagnostic = verify.command_diagnostics(["npm", "run", "test", "--workspace", "tests/e2e"])
        with self.assertRaisesRegex(verify.VerificationError, "browser_evidence_missing"):
            runner.command("browser-tests", [
                "python3", "-c", "print('credential-canary /private/tests/e2e/recovery.spec.ts:7:8')",
            ], diagnostics=diagnostic)
        self.assertEqual("failed", report.data["steps"][-1]["outcome"])
        self.assertNotIn("sourceLocations", report.data["steps"][-1])
        self.assertNotIn("credential-canary", report.path.read_text())
        self.assertEqual(set(), diagnostic.sources)

    def browser_report(self, count=2, *, executed=False):
        return {
            "errors": [], "stats": {"expected": count if executed else 0,
                                   "skipped": 0 if executed else count, "unexpected": 0, "flaky": 0},
            "suites": [{"specs": [{
                "file": "identity.spec.ts", "id": f"case-{index}", "title": f"reviewed case {index}",
                "line": index + 1, "column": 1, "ok": True,
                "tests": [{"projectId": "", "expectedStatus": "passed",
                           "status": "expected" if executed else "skipped",
                           "results": [{"status": "passed", "retry": 0, "errors": [],
                                        "stdout": [{"text": "credential-canary"}]}] if executed else []}],
            } for index in range(count)]}],
        }

    def test_browser_wrapper_compares_real_child_reports_and_discards_all_private_json(self):
        runner = verify.Runner(self.root, self.report("browser"), {})
        discovery = self.browser_report(count=4)
        execution = self.browser_report(count=4, executed=True)
        script = (
            "import json, os, sys; from pathlib import Path; "
            f"report = {discovery!r} if '--list' in sys.argv else {execution!r}; "
            "Path(os.environ['PLAYWRIGHT_JSON_OUTPUT_FILE']).write_text(json.dumps(report))"
        )
        command = ["python3", "-c", script, "identity.spec.ts"]
        runner.command("browser-tests", command, diagnostics=verify.runtime_support.CommandDiagnostics("browser"))
        step = runner.report.data["steps"][-1]
        self.assertEqual({"passed": 4, "skipped": 0, "requiredFiles": 1}, step["browserEvidence"])
        self.assertEqual([], list((self.root / "artifacts/verification/.private").iterdir()))
        self.assertNotIn("credential-canary", runner.report.path.read_text())
        self.assertNotIn("reviewed case", runner.report.path.read_text())

    def test_browser_success_requires_every_discovered_case_and_no_skips_failures_or_retries(self):
        path = self.root / "browser.json"
        path.touch(mode=0o600)
        path.write_text(json.dumps(self.browser_report()))
        expected, _ = verify.playwright_evidence(path, {"identity.spec.ts": 2})
        actual = self.browser_report(executed=True)
        path.write_text(json.dumps(actual))
        _, evidence = verify.playwright_evidence(path, {"identity.spec.ts": 2}, expected)
        self.assertEqual({"passed": 2, "skipped": 0, "requiredFiles": 1}, evidence)
        self.assertNotIn("credential-canary", json.dumps(evidence))
        for mutation in ("missing", "duplicate", "changed-id", "changed-title", "skipped",
                         "expected-failure", "flaky", "retry", "missing-result", "global-error", "bad-count"):
            invalid = copy.deepcopy(actual)
            cases = invalid["suites"][0]["specs"]
            if mutation == "missing":
                cases.pop()
            elif mutation == "duplicate":
                cases.append(copy.deepcopy(cases[0]))
            elif mutation == "changed-id":
                cases[0]["id"] = "substitute"
            elif mutation == "changed-title":
                cases[0]["title"] = "substitute"
            elif mutation == "skipped":
                cases[0]["tests"][0]["results"][0]["status"] = "skipped"
            elif mutation == "expected-failure":
                cases[0]["tests"][0]["expectedStatus"] = "failed"
            elif mutation == "flaky":
                cases[0]["tests"][0]["status"] = "flaky"
            elif mutation == "retry":
                cases[0]["tests"][0]["results"][0]["retry"] = 1
            elif mutation == "missing-result":
                cases[0]["tests"][0]["results"] = []
            elif mutation == "global-error":
                invalid["errors"] = [{"message": "credential-canary"}]
            else:
                invalid["stats"]["expected"] = 1
            path.write_text(json.dumps(invalid))
            with self.subTest(mutation=mutation), self.assertRaisesRegex(
                    verify.runtime_support.RuntimeFailure, "browser_evidence_invalid"):
                verify.playwright_evidence(path, {"identity.spec.ts": 2}, expected)

    def test_browser_discovery_cannot_hide_a_missing_file_case_or_declared_skip(self):
        path = self.root / "browser.json"
        path.touch(mode=0o600)
        for mutation in ("missing-file", "too-few", "declared-skip", "duplicate-json", "malformed"):
            report = self.browser_report()
            required = {"identity.spec.ts": 2}
            if mutation == "missing-file":
                required["bootstrap.spec.ts"] = 1
            elif mutation == "too-few":
                report["suites"][0]["specs"].pop()
            elif mutation == "declared-skip":
                report["suites"][0]["specs"][0]["tests"][0]["expectedStatus"] = "skipped"
            content = json.dumps(report)
            if mutation == "duplicate-json":
                content = '{"errors": [],' + content[1:]
            elif mutation == "malformed":
                content = content[:-1]
            path.write_text(content)
            with self.subTest(mutation=mutation), self.assertRaisesRegex(
                    verify.runtime_support.RuntimeFailure, "browser_evidence_invalid"):
                verify.playwright_evidence(path, required)

    def test_browser_private_report_refuses_public_permissions_links_special_files_and_unbounded_input(self):
        source = self.root / "source.json"
        source.touch(mode=0o600)
        source.write_text(json.dumps(self.browser_report()))
        path = self.root / "browser.json"
        for kind in ("public", "symlink", "hardlink", "fifo", "oversized"):
            if kind == "symlink":
                path.symlink_to(source)
            elif kind == "hardlink":
                path.hardlink_to(source)
            elif kind == "fifo":
                verify.os.mkfifo(path, 0o600)
            else:
                path.touch(mode=0o644 if kind == "public" else 0o600)
                path.write_bytes(source.read_bytes() if kind == "public" else b"x" * (16 * 1024 * 1024 + 1))
            try:
                with self.subTest(kind=kind), self.assertRaisesRegex(
                        verify.runtime_support.RuntimeFailure, "browser_evidence_invalid"):
                    verify.playwright_evidence(path, {"identity.spec.ts": 2})
            finally:
                path.unlink()

    def test_failed_composition_theory_preserves_safe_assertion_and_scenario_before_private_cleanup(self):
        runner = verify.Runner(self.root, self.report("integration"), {})
        diagnostic = verify.command_diagnostics([
            "dotnet", "test", "--project", verify.STORAGE_INTEGRATION,
        ])
        name = ("Kofte.Storage.IntegrationTests.Composition.SqliteCompositionProviderTests."
                "OwnedProviderBootPreservesInventoryAndExecutesOnlyItsIntendedPublicModules")
        owner, method = name.rsplit(".", 1)
        event = json.dumps({
            "schemaVersion": 1, "eventCode": "composition_test_phase_barrier_wrong",
            "attempt": "a" * 32, "provider": "sqlite", "scenario": "valid-dependency",
            "status": "failed", "assertions": 0,
        })
        xml = (f'<assemblies><test type="{owner}" method="{method}" result="Fail" '
               'name="credential-canary"><failure><message>composition_consumer_failed</message>'
               '</failure><output><![CDATA[' + event + '\n/private/credential-canary'
               ']]></output></test></assemblies>')
        script = (
            "import sys; from pathlib import Path; "
            "directory=Path(sys.argv[sys.argv.index('--results-directory')+1]); "
            f"(directory/'storage.xml').write_text({xml!r}); sys.exit(2)"
        )
        with self.assertRaisesRegex(verify.VerificationError, "command_failed"):
            runner.command("storage-integration-sqlite", ["python3", "-c", script], diagnostics=diagnostic)
        step = runner.report.data["steps"][-1]
        self.assertEqual([name], step["failedStorageTests"])
        self.assertEqual(["composition_consumer_failed", "composition_test_phase_barrier_wrong"],
                         step["storageFailureCodes"])
        self.assertEqual(["valid-dependency"], step["storageFailureScenarios"])
        self.assertFalse(list((self.root / "artifacts/verification/.private").rglob("storage.xml")))
        self.assertNotIn("credential", runner.report.path.read_text())
        self.assertEqual(set(), diagnostic.storage_codes)

    def test_failed_public_contract_retains_nested_source_known_identity_without_private_output(self):
        runner = verify.Runner(self.root, self.report("contract"), {})
        diagnostic = verify.command_diagnostics([
            "dotnet", "test", "--project", "tests/contract/Kofte.Platform.ContractTests",
        ])
        self.assertIsNotNone(diagnostic)
        name = ("Kofte.Platform.ContractTests.Composition.CompositionPublicContractTests"
                "+PostgresCompositionPublicContractTests."
                "AllRequiresEveryActualStandaloneScenarioAndItsCompleteSemanticEvidence")
        owner, method = name.rsplit(".", 1)
        xml = (f'<assemblies><test type="{owner}" method="{method}" result="Fail" '
               'name="credential-canary"><failure><message>composition_consumer_failed</message>'
               '<stack-trace>/private/credential-canary</stack-trace></failure></test>'
               '<test type="credential-canary" method="unknown" result="Fail"/></assemblies>')
        script = (
            "import sys; from pathlib import Path; "
            "directory=Path(sys.argv[sys.argv.index('--results-directory')+1]); "
            f"(directory/'storage.xml').write_text({xml!r}); "
            "print('/private/credential-canary'); sys.exit(2)"
        )
        with self.assertRaisesRegex(verify.VerificationError, "command_failed"):
            runner.command("composition-public-contracts", ["python3", "-c", script],
                           diagnostics=diagnostic)
        step = runner.report.data["steps"][-1]
        self.assertEqual([name], step["failedContractTests"])
        self.assertEqual(["composition_consumer_failed"], step["contractFailureCodes"])
        self.assertFalse(list((self.root / "artifacts/verification/.private").rglob("storage.xml")))
        self.assertNotIn("credential", runner.report.path.read_text())
        self.assertEqual(set(), diagnostic.failed_tests)

    def test_contract_timeout_keeps_completed_source_methods_without_turning_progress_into_success(self):
        runner = verify.Runner(self.root, self.report("contract"), {})
        name = "Kofte.Platform.ContractTests.Composition.KnownTests.KnownCase"
        diagnostic = verify.runtime_support.CommandDiagnostics("contract", tests=[name])
        script = (
            "import time; "
            f"print(' passed {name}(secret: credential-canary) (10ms)', flush=True); "
            "print('private credential-canary', flush=True); time.sleep(10)"
        )
        with self.assertRaisesRegex(verify.VerificationError, "command_timeout"):
            runner.command("composition-public-contracts", ["python3", "-c", script],
                           diagnostics=diagnostic, timeout=1)
        step = runner.report.data["steps"][-1]
        self.assertEqual("failed", step["outcome"])
        self.assertEqual([name], step["contractCompletedMethods"])
        self.assertEqual("unavailable", step["contractDiagnosticCode"])
        self.assertNotIn("nativeEvidence", step)
        self.assertNotIn("credential", runner.report.path.read_text())
        self.assertEqual({}, diagnostic.safe_fields())

    def test_identity_recipe_failure_keeps_known_consumer_source_not_private_exception(self):
        runner = verify.Runner(self.root, self.report("contract"), {})
        diagnostic = verify.command_diagnostics([
            "dotnet", "test", "--project", "tests/contract/Kofte.Platform.ContractTests",
        ])
        owner = "Kofte.Platform.ContractTests.Identity.SqliteIdentityGuideTests"
        method = "DeliveredRecipeRequiresEveryExactOrderedObservation"
        source = "tests/contract/Kofte.Identity.Consumer/Scenarios/OperationsScenarios.cs"
        xml = (
            f'<assemblies><test type="{owner}" method="{method}" result="Fail">'
            '<failure><message>identity_consumer_failed credential-canary '
            'identity_fixture_operator_prepare_prepare_identity_keys_invalid '
            'identity_fixture_operator_private_actor_prepare_identity_keys_invalid '
            'identity_scenario_operations_line_1 identity_scenario_operations_line_999999</message>'
            f'<stack-trace>at Execute in /private/credential-canary/{source}:line 1'
            '</stack-trace></failure></test></assemblies>'
        )
        script = (
            "import sys; from pathlib import Path; "
            "directory=Path(sys.argv[sys.argv.index('--results-directory')+1]); "
            f"(directory/'storage.xml').write_text({xml!r}); sys.exit(2)"
        )
        with self.assertRaisesRegex(verify.VerificationError, "command_failed"):
            runner.command("identity-public-upgrade", ["python3", "-c", script],
                           diagnostics=diagnostic)
        step = runner.report.data["steps"][-1]
        self.assertEqual([f"{source}:1"], step["contractFailureLocations"])
        self.assertEqual(["identity_consumer_failed",
                          "identity_fixture_operator_prepare_prepare_identity_keys_invalid",
                          "identity_scenario_operations_line_1"], step["contractFailureCodes"])
        self.assertNotIn("credential", runner.report.path.read_text())
        self.assertNotIn("private_actor", runner.report.path.read_text())
        self.assertFalse(list((self.root / "artifacts/verification/.private").rglob("storage.xml")))

    def test_failed_public_recipe_retains_only_its_closed_assertion_and_scenario(self):
        runner = verify.Runner(self.root, self.report("contract"), {})
        diagnostic = verify.command_diagnostics([
            "dotnet", "run", "--project", "tests/contract/Kofte.Composition.Consumer",
        ])
        self.assertIsNotNone(diagnostic)
        event = {
            "schemaVersion": 1, "eventCode": "composition_test_timeout_changed_original_installation",
            "attempt": "a" * 32, "provider": "postgres", "scenario": "bounded-stop",
            "status": "failed", "assertions": 0,
        }
        unrelated = dict(event, eventCode="credential-canary", scenario="credential-canary")
        script = (
            f"print({json.dumps(event)!r}); print({json.dumps(unrelated)!r}); "
            "print('/private/credential-canary'); raise SystemExit(1)"
        )
        with self.assertRaisesRegex(verify.VerificationError, "command_failed"):
            runner.command("composition-guide-postgres-bounded-stop", ["python3", "-c", script],
                           diagnostics=diagnostic)
        step = runner.report.data["steps"][-1]
        self.assertEqual(["composition_test_timeout_changed_original_installation"],
                         step["consumerFailureCodes"])
        self.assertEqual(["bounded-stop"], step["consumerFailureScenarios"])
        self.assertNotIn("credential", runner.report.path.read_text())
        self.assertEqual(set(), diagnostic.storage_codes)

    def test_missing_tool_and_timeout_fail_instead_of_skipping(self):
        report = self.report()
        runner = verify.Runner(self.root, report, {})
        with self.assertRaisesRegex(verify.VerificationError, "tool_or_input_missing"):
            runner.command("missing-browser", ["kofte-tool-that-does-not-exist"])
        with self.assertRaisesRegex(verify.VerificationError, "command_timeout"):
            runner.command("hung-provider", ["python3", "-c", "import time; time.sleep(60)"], timeout=0.1)
        self.assertEqual(["failed", "failed"], [step["outcome"] for step in report.data["steps"]])

    def test_unexpected_or_supervisor_failure_cannot_be_reported_as_pass(self):
        report = self.report()
        runner = verify.Runner(self.root, report, {})
        for error in (RuntimeError("secret-canary"), verify.dev.DevError("startup_timeout")):
            with self.subTest(error=type(error).__name__):
                with self.assertRaises(verify.VerificationError):
                    runner.operation("runtime-start", Mock(side_effect=error))
                self.assertEqual("failed", report.data["steps"][-1]["outcome"])
                self.assertNotIn("secret-canary", report.path.read_text())

    def test_all_attempts_ten_groups_and_never_labels_a_targeted_pass_complete(self):
        reports = []

        def execute(runner, suite, provider, arguments):
            if suite == "privacy":
                raise RuntimeError("credential-canary /private/diagnostics")

        original_report = verify.Report

        def construct(root, suite, provider, invocation):
            with patch.object(verify.subprocess, "run", return_value=subprocess.CompletedProcess(
                [], 0, b"a" * 40 + b"\n", b"",
            )):
                report = original_report(root, suite, provider, invocation)
            reports.append(report)
            return report

        with patch.object(verify, "Report", side_effect=construct), \
                patch.object(verify, "execute_suite", side_effect=execute):
            self.assertEqual(0, verify.main(["--suite", "unit"], self.root, {}))
            self.assertEqual("targeted", reports[0].data["scope"])
            self.assertFalse(reports[0].data["completeGate"])
            reports.clear()
            self.assertEqual(1, verify.main(["--suite", "all"], self.root, {}))
        self.assertEqual(10, len(reports))
        self.assertEqual(["sqlite", "postgres"], [
            report.data["provider"] for report in reports if report.data["suite"] == "integration"
        ])
        self.assertEqual(9, sum(report.data["outcome"] == "passed" for report in reports))
        self.assertTrue(all(not report.data["completeGate"] for report in reports))
        privacy = next(report for report in reports if report.data["suite"] == "privacy")
        self.assertEqual("suite-runtime", privacy.data["steps"][-1]["name"])
        self.assertNotIn("credential-canary", privacy.path.read_text())

    def test_unimplemented_required_suites_fail_before_tool_invocation(self):
        arguments = Mock()
        for suite, code in verify.UNIMPLEMENTED.items():
            with self.subTest(suite=suite):
                report = self.report(suite)
                runner = verify.Runner(self.root, report, {})
                with patch.object(runner, "versions") as versions, \
                        self.assertRaisesRegex(verify.VerificationError, code):
                    verify.execute_suite(runner, suite, "both", arguments)
                versions.assert_not_called()
                self.assertEqual("failed", report.data["steps"][-1]["outcome"])

    def test_postgres_integration_requires_explicit_disposable_opt_in(self):
        arguments = Mock(disposable_postgres=False)
        report = self.report("integration")
        runner = verify.Runner(self.root, report, {})
        with patch.object(runner, "versions"), patch.object(runner, "command") as command, \
                self.assertRaisesRegex(verify.VerificationError, "disposable_postgres_opt_in_required"):
            verify.execute_suite(runner, "integration", "postgres", arguments)
        command.assert_not_called()

    def test_postgres_public_contract_requires_opt_in_before_any_child(self):
        for name in verify.REQUIRED_SUITE_FILES["contract"]:
            path = self.root / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text("isolated selector fixture")
        runner = verify.Runner(self.root, self.report("contract"), {})
        with patch.object(runner, "versions"), patch.object(runner, "command") as command, \
                self.assertRaisesRegex(verify.VerificationError, "disposable_postgres_opt_in_required"):
            verify.execute_suite(runner, "contract", "both", Mock(
                disposable_postgres=False, postgres_connection=None,
            ))
        command.assert_not_called()

    def test_missing_platform_target_fails_unit_quality_and_contract_before_tools(self):
        for name in verify.PLATFORM_REQUIRED_FILES:
            path = self.root / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text("required target fixture")
        for suite in ("unit", "quality", "contract"):
            for name in verify.PLATFORM_REQUIRED_FILES:
                with self.subTest(suite=suite, name=name):
                    path = self.root / name
                    path.unlink()
                    runner = verify.Runner(self.root, self.report(suite), {})
                    with patch.object(runner, "versions") as versions, \
                            self.assertRaisesRegex(verify.VerificationError, "platform_target_missing"):
                        verify.execute_suite(runner, suite, "sqlite", Mock())
                    versions.assert_not_called()
                    self.assertEqual("platform-inventory", runner.report.data["steps"][-1]["name"])
                    self.assertEqual("failed", runner.report.data["steps"][-1]["outcome"])
                    path.write_text("required target fixture")

    def test_platform_unit_commands_cannot_skip_or_allow_zero_tests(self):
        for name in verify.PLATFORM_REQUIRED_FILES:
            path = self.root / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text("required target fixture")
        runner = verify.Runner(self.root, self.report("unit"), {})
        with patch.object(runner, "versions"), patch.object(runner, "command") as command:
            verify.execute_suite(runner, "unit", None, Mock())
        platform = next(call for call in command.call_args_list if call.args[0] == "platform-unit")
        self.assertIn("tests/unit/Kofte.Platform.Tests", platform.args[1])
        self.assertEqual(["--minimum-expected-tests", "1"], platform.args[1][-2:])
        self.assertNotIn("--if-present", platform.args[1])
        self.assertTrue(any(call.args[0] == "host-unit" for call in command.call_args_list))
        capture = next(call for call in command.call_args_list
                       if call.args[0] == "identity-browser-capture-unit")
        self.assertEqual(["node", "--test", "tests/e2e/identityPrivacyCapture.unit.ts",
                          "tests/e2e/identityCanaryExport.unit.ts",
                          "tests/e2e/identityActionCapture.unit.ts",
                          "tests/e2e/identityBrowserOwnership.unit.ts"], capture.args[1])

    def test_platform_child_failure_and_timeout_preserve_safe_named_failure(self):
        for name, command, timeout, code in (
            ("platform-public-contracts", ["python3", "-c", "import sys; print('private-marker'); sys.exit(8)"],
             5, "command_failed"),
            ("platform-public-contracts", ["python3", "-c", "import time; time.sleep(60)"],
             0.1, "command_timeout"),
        ):
            with self.subTest(code=code):
                report = self.report("contract")
                runner = verify.Runner(self.root, report, {})
                with self.assertRaisesRegex(verify.VerificationError, code):
                    runner.command(name, command, timeout=timeout)
                self.assertEqual("failed", report.data["steps"][-1]["outcome"])
                self.assertEqual(name, report.data["steps"][-1]["name"])
                self.assertNotIn("private-marker", report.path.read_text())
                if code == "command_failed":
                    self.assertEqual(8, report.data["steps"][-1]["exitCode"])

    def test_environment_and_command_reports_never_copy_operator_secrets(self):
        clean = verify.child_environment({
            "PATH": "/tools", "KOFTE_POSTGRES_CONNECTION": "credential-canary",
            "KOFTE_DATA_ROOT": "/private/data", "OTEL_EXPORTER_OTLP_HEADERS": "token-canary",
            "NEXT_PUBLIC_ACCIDENT": "credential-canary", "NODE_OPTIONS": "--require private",
        })
        self.assertFalse(any(key.startswith(("KOFTE_", "OTEL_", "NEXT_PUBLIC_")) for key in clean))
        self.assertNotIn("NODE_OPTIONS", clean)
        self.assertEqual(
            ["dotnet", "<private-argument>", "--store", "init", "<private-argument>"],
            verify.safe_command(["dotnet", "/private/store", "--store", "init", "Password=credential-canary"]),
        )

    def test_cleanup_stops_owned_group_without_touching_an_unrelated_process(self):
        unrelated = subprocess.Popen(
            ["python3", "-c", "import time; time.sleep(60)"],
            stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL,
        )
        try:
            owned = verify.OwnedProcesses(self.root, {})
            child = owned.start(["python3", "-c", "import time; time.sleep(60)"])
            owned.stop()
            self.assertIsNotNone(child.poll())
            self.assertIsNone(unrelated.poll())
        finally:
            unrelated.terminate()
            unrelated.wait(timeout=5)

    def test_private_fixture_cleanup_is_confined_to_the_owned_root(self):
        sentinel = self.root / "outside-sentinel"
        sentinel.write_text("unchanged")
        with verify.private_root(self.root) as directory:
            self.assertEqual(0, directory.stat().st_mode & 0o077)
            (directory / "owned-fixture").write_text("private")
        self.assertFalse(directory.exists())
        self.assertEqual("unchanged", sentinel.read_text())

    def test_artifact_symlink_is_rejected_before_modifying_its_target(self):
        outside = self.root / "outside"
        outside.mkdir()
        (self.root / "artifacts").symlink_to(outside, target_is_directory=True)
        with self.assertRaisesRegex(verify.VerificationError, "artifact_directory_unsafe"):
            verify.owned_directory(self.root, "artifacts/verification")
        self.assertFalse((outside / "verification").exists())

    def test_unconfirmed_postgres_cleanup_retains_private_ownership_evidence(self):
        with self.assertRaisesRegex(verify.VerificationError, "postgres_cleanup_failed"):
            with verify.private_root(self.root) as directory:
                (directory / "lease.json").write_text("private ownership evidence")
                raise verify.VerificationError("postgres_cleanup_failed")
        self.assertTrue((directory / "lease.json").is_file())
        self.assertEqual(0, directory.stat().st_mode & 0o077)
        shutil.rmtree(directory)

    def test_postgres_lease_closes_only_owned_stdin_and_checks_cleanup_result(self):
        runner = verify.Runner(self.root, self.report(), {})
        lease = verify.PostgresLease(runner, self.root)
        lease.child = Mock(returncode=3)
        lease.child.stdin = io.BytesIO()
        with self.assertRaisesRegex(verify.VerificationError, "postgres_cleanup_failed"):
            lease.stop()
        self.assertTrue(lease.child.stdin.closed)
        lease.child.wait.assert_called_once_with(timeout=75)

    def test_runtime_worker_failure_interrupts_the_owned_test_command(self):
        report = self.report()
        runner = verify.Runner(self.root, report, {})
        health = Mock(side_effect=verify.runtime_support.RuntimeFailure("runtime_control_worker_failed"))
        with self.assertRaisesRegex(verify.VerificationError, "runtime_control_worker_failed"):
            runner.command(
                "contract-runtime-tests", ["python3", "-c", "import time; time.sleep(60)"],
                health_check=health,
            )
        self.assertEqual("failed", report.data["steps"][-1]["outcome"])
        self.assertEqual("contract-runtime-tests", report.data["steps"][-1]["name"])

    def test_recovery_selects_the_intersection_of_provider_and_suite_then_real_browser_tests(self):
        for name in verify.REQUIRED_SUITE_FILES["recovery"]:
            path = self.root / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text("isolated selector fixture")
        runner = verify.Runner(self.root, self.report("recovery"), {})
        arguments = Mock(disposable_postgres=True, postgres_connection=None)
        selected = []

        @contextmanager
        def owned_runtime(runner, provider, *_args, **_kwargs):
            selected.append(provider)
            yield Mock(environment={"SELECTED_PROVIDER": provider}, directory=self.root, check=lambda: None)

        with patch.object(runner, "versions"), patch.object(runner, "command") as command, \
                patch.object(runner, "composition_guide") as guide, \
                patch.object(verify, "runtime", side_effect=owned_runtime):
            verify.execute_suite(runner, "recovery", "both", arguments)
        guide.assert_not_called()
        self.assertEqual(["sqlite", "postgres"], selected)
        native = [call for call in command.call_args_list if call.args[0].endswith("-native")]
        self.assertEqual(2, len(native))
        for provider, call in zip(selected, native):
            arguments = call.args[1]
            expression = f"/[(Suite=recovery)&(Provider={provider})]"
            self.assertIn(expression, arguments)
            self.assertIn("--filter-query", arguments)
            self.assertNotIn("--filter-trait", arguments)
            self.assertEqual(arguments, verify.safe_command(arguments))
            self.assertEqual("1", arguments[-1])
        browser = [call for call in command.call_args_list if call.args[0].endswith("-browser")]
        self.assertEqual(2, len(browser))
        self.assertTrue(all("recovery.spec.ts" in call.args[1] for call in browser))
        self.assertTrue(all(call.kwargs["health_check"] is not None for call in browser))

    def test_contract_and_privacy_select_only_their_real_serial_test_files(self):
        for paths in verify.REQUIRED_SUITE_FILES.values():
            for name in paths:
                path = self.root / name
                path.parent.mkdir(parents=True, exist_ok=True)
                path.write_text("isolated selector fixture")
        arguments = Mock(disposable_postgres=True, postgres_connection=None)

        @contextmanager
        def owned_runtime(_runner, _provider, *_args, **kwargs):
            environment = {"BOOTSTRAP_PRIVACY_CANARY": kwargs.get("privacy_canary") or ""}
            yield Mock(environment=environment, directory=self.root, check=lambda: None)

        for suite in ("contract", "privacy"):
            runner = verify.Runner(self.root, self.report(suite), {})
            with patch.object(runner, "versions"), patch.object(runner, "command") as command, \
                    patch.object(runner, "composition_guide") as guide, \
                    patch.object(verify, "runtime", side_effect=owned_runtime):
                verify.execute_suite(runner, suite, "both", arguments)
            http = [call for call in command.call_args_list if call.args[0].endswith("-http")]
            self.assertEqual(2, len(http))
            for call in http:
                self.assertIn("--no-file-parallelism", call.args[1])
                if suite == "contract":
                    self.assertIn("adapter.test.ts", call.args[1])
                    self.assertIn("bootstrap.test.ts", call.args[1])
                    self.assertNotIn("privacy.test.ts", call.args[1])
                else:
                    self.assertIn("privacy.test.ts", call.args[1])
                    self.assertTrue(call.kwargs["environment"]["BOOTSTRAP_PRIVACY_CANARY"])
            if suite == "privacy":
                guide.assert_not_called()
                builds = [index for index, call in enumerate(command.call_args_list)
                          if call.args[1] == ["npm", "run", "build", "--workspace", "apps/web"]]
                self.assertEqual(1, len(builds))
                for provider, prefix in (("sqlite", "Sqlite"), ("postgres", "Postgres")):
                    browser = next(call for call in command.call_args_list
                                   if call.args[0] == f"identity-privacy-browser-native-{provider}")
                    self.assertIn(f"/[(Suite=browser)&(Provider={provider})]", browser.args[1])
                    self.assertEqual((prefix + "IdentityBrowserFailureTests",),
                                     verify.required_native_classes(browser.args[0], browser.args[1]))
                    recipes = next(call for call in command.call_args_list
                                   if call.args[0] == f"storage-public-privacy-{provider}")
                    self.assertLess(builds[0], command.call_args_list.index(recipes))
                    self.assertLess(builds[0], command.call_args_list.index(browser))
                    self.assertEqual((prefix + "IdentityPrivacyGuideTests",),
                                     verify.required_native_classes(recipes.args[0], recipes.args[1]))
                capture = next(call for call in command.call_args_list
                               if call.args[0] == "privacy-supervisor-capture")
                self.assertIn("scripts/run_python_tests.py", capture.args[1])
                self.assertIn("test_dev_output.py", capture.args[1])
            else:
                self.assertEqual(
                    [(provider, scenario) for provider in ("sqlite", "postgres")
                     for scenario in (*verify.COMPOSITION_OBSERVATIONS, "all")],
                    [call.args[:2] for call in guide.call_args_list])
                public = next(call for call in command.call_args_list
                              if call.args[0] == "platform-public-contracts")
                self.assertIn("*IdentityGuideTests", public.args[1])
                self.assertIn("*IdentityPrivacyGuideTests", public.args[1])
                for provider, prefix in (("sqlite", "Sqlite"), ("postgres", "Postgres")):
                    recipes = next(call for call in command.call_args_list
                                   if call.args[0] == f"identity-public-guides-{provider}")
                    self.assertIn("*" + prefix + "IdentityGuideTests", recipes.args[1])
                    self.assertEqual(["--minimum-expected-tests", "10"], recipes.args[1][-2:])
                    self.assertEqual((prefix + "IdentityGuideTests",),
                                     verify.required_native_classes(recipes.args[0], recipes.args[1]))
                self.assertEqual(["--minimum-expected-tests", "1"], public.args[1][-2:])
                self.assertEqual("true", public.kwargs["environment"]["KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE"])
                self.assertNotIn("--parallel", public.args[1])
                composition = next(call for call in command.call_args_list
                                   if call.args[0] == "composition-public-contracts").args[1]
                self.assertEqual("collections", composition[composition.index("--parallel") + 1])
                self.assertEqual(str(min(4, verify.os.cpu_count() or 1)),
                                 composition[composition.index("--max-threads") + 1])
                self.assertEqual(["--minimum-expected-tests", "86"], composition[-2:])
                author = next(i for i, call in enumerate(command.call_args_list)
                              if call.args[0] == "manifest-author-build")
                contracts = next(i for i, call in enumerate(command.call_args_list)
                                 if call.args[0] == "platform-public-contracts")
                self.assertLess(author, contracts)
                web = next(i for i, call in enumerate(command.call_args_list)
                           if call.args[1] == ["npm", "run", "build", "--workspace", "apps/web"])
                self.assertLess(web, contracts)
                schema = next(call for call in command.call_args_list
                              if call.args[0] == "module-manifest-schema")
                self.assertIn("module-manifest.test.ts", schema.args[1])
                native = [call for call in command.call_args_list
                          if call.args[0].startswith("native-openapi-policy-")]
                self.assertEqual(2, len(native))
                for provider, call in zip(("sqlite", "postgres"), native):
                    self.assertIn("*OpenApiTests", call.args[1])
                    self.assertIn("--filter-class", call.args[1])
                    self.assertIn("--filter-trait", call.args[1])
                    self.assertIn(f"Provider={provider}", call.args[1])
                    self.assertEqual(
                        provider == "postgres",
                        call.kwargs["environment"].get("KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE") == "true",
                    )

    def test_sqlite_contract_policy_never_selects_postgres_or_requires_disposable_opt_in(self):
        for name in verify.REQUIRED_SUITE_FILES["contract"]:
            path = self.root / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text("isolated selector fixture")
        runner = verify.Runner(self.root, self.report("contract"), {})

        @contextmanager
        def owned_runtime(_runner, provider, disposable, *_args, **_kwargs):
            self.assertEqual("sqlite", provider)
            self.assertFalse(disposable)
            yield Mock(environment={}, directory=self.root, check=lambda: None)

        with patch.object(runner, "versions"), patch.object(runner, "command") as command, \
                patch.object(runner, "composition_guide") as guide, \
                patch.object(verify, "runtime", side_effect=owned_runtime):
            verify.execute_suite(runner, "contract", "sqlite", Mock(
                disposable_postgres=False, postgres_connection=None,
            ))
        native = [call for call in command.call_args_list if call.args[0].startswith("native-openapi-policy-")]
        self.assertEqual(1, len(native))
        self.assertIn("*OpenApiTests", native[0].args[1])
        self.assertIn("Provider=sqlite", native[0].args[1])
        self.assertNotIn("Provider=postgres", native[0].args[1])
        self.assertNotIn("KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE", native[0].kwargs["environment"])
        public = next(call for call in command.call_args_list if call.args[0] == "platform-public-contracts")
        self.assertIn("--filter-not-trait", public.args[1])
        self.assertIn("Provider=postgres", public.args[1])
        self.assertNotIn("KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE", public.kwargs["environment"])
        self.assertEqual(["sqlite"] * 12, [call.args[0] for call in guide.call_args_list])

    def test_normal_browser_does_not_run_privacy_without_its_seeded_capture_session(self):
        directory = self.root / "tests/e2e"
        directory.mkdir(parents=True, exist_ok=True)
        for name in ("accessibility.spec.ts", "bootstrap.spec.ts", "recovery.spec.ts",
                     "startup.spec.ts", "privacy.spec.ts"):
            (directory / name).write_text("isolated selector fixture")
        self.assertEqual(["accessibility.spec.ts", "bootstrap.spec.ts", "identity.spec.ts", "recovery.spec.ts"],
                         verify.browser_specs(self.root))
        (directory / "accessibility.spec.ts").unlink()
        with self.assertRaisesRegex(verify.VerificationError, "browser_source_inventory_invalid"):
            verify.browser_specs(self.root)

    def test_postgres_pause_requires_the_matching_real_fixture_acknowledgment(self):
        runner = verify.Runner(self.root, self.report(), {})
        lease = verify.PostgresLease(runner, self.root)
        lease.child = Mock()
        lease.child.poll.return_value = None
        lease.child.stdin = io.BytesIO()
        lease.events.put("postgres_fixture_paused")
        lease.command("pause")
        self.assertEqual(b"pause\n", lease.child.stdin.getvalue())
        lease.events.put("postgres_fixture_paused")
        with self.assertRaisesRegex(verify.VerificationError, "postgres_fixture_event_unexpected"):
            lease.command("resume")
        lease.child.stdin.close()

    def test_runtime_controls_are_test_only_and_host_restart_preserves_its_owned_configuration(self):
        runner = verify.Runner(self.root, self.report("privacy"), {
            "BOOTSTRAP_TEST_CONTROL": "/foreign/control",
            "BOOTSTRAP_TEST_RUN_ID": "foreign",
            "BOOTSTRAP_PRIVACY_CANARY": "foreign-canary",
            "DOTNET_ROOT": "/pinned/sdk",
            "DOTNET_ENVIRONMENT": "Production",
        })
        groups = []

        class Owned:
            def __init__(self, _root, environment, capture=None):
                self.environment, self.capture = dict(environment), capture
                self.commands, self.stopped = [], False
                groups.append(self)

            def start(self, command, **_kwargs):
                self.commands.append(command)
                self.capture.read(io.BytesIO(b"safe runtime output"))

            def check(self):
                pass

            def stop(self):
                self.stopped = True

        initialization = []

        def initialize(name, command, **kwargs):
            initialization.append((name, command))
            if name == "isolated-store-init":
                database = Path(kwargs["environment"]["KOFTE_DATA_ROOT"]) / "platform/platform.db"
                database.parent.mkdir()
                database.write_bytes(b"owned store")

        with patch.object(runner, "build_runtime"), patch.object(runner, "command", side_effect=initialize), \
                patch.object(verify, "OwnedProcesses", Owned), patch.object(verify.dev, "wait_ready"):
            with verify.runtime(runner, "sqlite", False, privacy_canary="current-canary",
                                identity_browser=True) as session:
                directory = session.directory
                self.assertEqual("current-canary", session.environment["BOOTSTRAP_PRIVACY_CANARY"])
                self.assertEqual(0o700, Path(session.environment["BOOTSTRAP_TEST_CONTROL"]).stat().st_mode & 0o777)
                canary_root = Path(session.environment["KOFTE_IDENTITY_PRIVATE_ROOT"])
                self.assertEqual(0o700, canary_root.stat().st_mode & 0o777)
                canaries = canary_root / "identity-browser-canaries.json"
                canaries.write_text(json.dumps({
                    "version": 1, "canaries": ["a" * 16, "b" * 16, "c" * 16],
                }))
                canaries.chmod(0o600)
                self.assertRegex(session.environment["BOOTSTRAP_TEST_RUN_ID"], "^[a-f0-9]{32}$")
                identity = Path(session.environment["KOFTE_AUTH_CONFIG_FILE"])
                self.assertEqual(0o600, identity.stat().st_mode & 0o777)
                self.assertFalse(identity.is_relative_to(Path(session.environment["KOFTE_DATA_ROOT"])))
                self.assertEqual("Development", session.environment["ASPNETCORE_ENVIRONMENT"])
                self.assertEqual("Development", session.environment["DOTNET_ENVIRONMENT"])
                configuration = json.loads(identity.read_text())
                self.assertEqual("development", configuration["mode"])
                self.assertEqual(session.environment["KOFTE_WEB_URL"], configuration["publicOrigin"])
                self.assertEqual({"enabled": True, "identities": [{"key": "synthetic"}]},
                                 configuration["development"])
                self.assertFalse(Path(configuration["keyRingDirectory"]).is_relative_to(
                    Path(session.environment["KOFTE_DATA_ROOT"])))
                self.assertEqual([
                    ("isolated-store-init", ["dotnet", verify.HOST, "--store", "init"]),
                    ("isolated-registry-upgrade", ["dotnet", verify.HOST, "--registry", "upgrade"]),
                    ("isolated-identity-prepare", ["dotnet", verify.HOST, "--identity", "prepare"]),
                ], initialization)
                session.worker.actions["stop-host"]()
                session.worker.actions["start-host"]()
                session.check()
                hosts = [group for group in groups if ["dotnet", verify.HOST] in group.commands]
                self.assertEqual(2, len(hosts))
                self.assertTrue(hosts[0].stopped)
                self.assertEqual(hosts[0].environment, hosts[1].environment)
                for group in groups:
                    self.assertFalse(any(key.startswith("BOOTSTRAP_") for key in group.environment))
                    self.assertNotIn("KOFTE_IDENTITY_PRIVATE_ROOT", group.environment)
                    self.assertEqual("/pinned/sdk", group.environment["DOTNET_ROOT"])
        self.assertFalse(directory.exists())
        self.assertTrue(all(group.stopped for group in groups))
        receipt_step = next(step for step in runner.report.data["steps"]
                            if step["name"] == "identity-browser-canaries")
        self.assertEqual(3, receipt_step["identityCanaries"])
        self.assertEqual("passed", receipt_step["outcome"])

    def test_uncertain_process_cleanup_retains_only_the_private_runtime_root(self):
        runner = verify.Runner(self.root, self.report("privacy"), {})
        groups = []

        def processes(_root, _environment, capture=None):
            capture.read(io.BytesIO(b"safe runtime output"))
            group = Mock()
            groups.append(group)
            return group

        with patch.object(runner, "build_runtime"), patch.object(runner, "command"), \
                patch.object(verify, "OwnedProcesses", side_effect=processes), \
                patch.object(verify.dev, "wait_ready"):
            with self.assertRaisesRegex(verify.VerificationError, "runtime_cleanup_process_failed"):
                with verify.runtime(runner, "sqlite", False) as session:
                    directory = session.directory
                    groups[0].stop.side_effect = RuntimeError("private diagnostics")
        self.assertTrue(directory.is_dir())
        self.assertTrue(all(group.stop.called for group in groups))
        self.assertEqual("passed", runner.report.data["steps"][-1]["outcome"])
        self.assertEqual("runtime-output-scan", runner.report.data["steps"][-1]["name"])
        self.assertNotIn("private diagnostics", runner.report.path.read_text())

    def test_owned_process_cleanup_never_signals_a_retired_group_twice(self):
        group = verify.OwnedProcesses(self.root, {})
        group.start(["python3", "-c", "import time; time.sleep(60)"], stdin=subprocess.DEVNULL)
        self.addCleanup(group.stop)
        with patch.object(verify.os, "killpg", wraps=verify.os.killpg) as signal_group:
            group.stop()
            group.stop()
        self.assertEqual(1, signal_group.call_count)
        with self.assertRaisesRegex(verify.VerificationError, "owned_group_already_stopped"):
            group.start(["python3", "-c", "pass"])

    def test_exited_group_permission_error_requires_kernel_confirmation_of_absence(self):
        group = verify.OwnedProcesses(self.root, {})
        child = group.start(["python3", "-c", "pass"])
        self.assertEqual(0, child.wait(timeout=5))
        with patch.object(verify.os, "killpg", side_effect=[
                PermissionError(), ProcessLookupError()]) as signal_group:
            group.stop()
        self.assertTrue(group.stopped)
        self.assertEqual([
            ((child.pid, signal.SIGTERM), {}),
            ((child.pid, 0), {}),
        ], signal_group.call_args_list)

    def test_exited_leader_does_not_hide_a_present_or_unverifiable_descendant_group(self):
        for observation in (None, PermissionError()):
            with self.subTest(observation=type(observation).__name__):
                group = verify.OwnedProcesses(self.root, {})
                child = group.start(["python3", "-c", "pass"])
                self.assertEqual(0, child.wait(timeout=5))
                with patch.object(verify.os, "killpg", side_effect=[
                        PermissionError(), observation]) as signal_group:
                    with self.assertRaises(PermissionError):
                        group.stop()
                self.assertFalse(group.stopped)
                self.assertEqual([
                    ((child.pid, signal.SIGTERM), {}),
                    ((child.pid, 0), {}),
                ], signal_group.call_args_list)
                group.stop()

    def test_framework_stream_reader_failure_is_safe_and_cannot_pass_the_suite(self):
        class Broken(io.BytesIO):
            def read(self, *_args):
                raise OSError("credential-canary /private/diagnostics")
        group = verify.OwnedProcesses(self.root, {})
        group.read(Broken())
        with self.assertRaisesRegex(verify.runtime_support.RuntimeFailure, "owned_output_read_failed"):
            group.check()
        with self.assertRaisesRegex(verify.runtime_support.RuntimeFailure, "owned_output_read_failed"):
            group.stop()
        group.stop()

    def startup_samples(self, composition=False):
        return [
            {"cycle": cycle, "readySeconds": cycle / 10, "pageSeconds": cycle / 100, "fixtureMatches": True,
             **({"totalSeconds": cycle / 5, "modulesPublished": True} if composition else {})}
            for cycle in (1, 2)
        ]

    def test_startup_retains_only_fresh_validated_samples_in_the_uploaded_verifier_report(self):
        stale = self.root / "tests/e2e/test-results/previous/startup-observations.json"
        stale.parent.mkdir(parents=True)
        stale.write_text("credential-canary /private/old-results")
        runner = verify.Runner(self.root, self.report("startup"), {})
        outputs = []
        commands = []

        def child(name, command, **kwargs):
            commands.append(name)
            if name in ("startup-storage-fixture-build", "startup-composition-fixture-build"):
                self.assertIn("tests/contract/Kofte." + (
                    "Composition" if "composition" in name else "Storage") + ".Consumer", command)
                runner.operation(name, lambda _: None, command)
                return
            def produce(_):
                directory = Path(command[command.index("--output") + 1])
                outputs.append(directory)
                self.assertEqual([], list(directory.iterdir()))
                composition = name == "startup-composition-restarts"
                if not composition:
                    self.assertEqual("2", kwargs["environment"]["BOOTSTRAP_WARM_STARTS"])
                self.assertIn("--grep" if composition else "--grep-invert", command)
                filename = "composition-startup-observations.json" if composition else "startup-observations.json"
                path = directory / "actual-startup-test" / filename
                path.parent.mkdir()
                path.write_text(json.dumps(self.startup_samples(composition)))
            runner.operation(name, produce, command)

        with patch.object(runner, "versions"), patch.object(runner, "build_runtime"), \
                patch.object(runner, "command", side_effect=child):
            verify.execute_suite(runner, "startup", None, Mock(warm_starts=2))
        persisted = json.loads(runner.report.path.read_text())
        step = next(step for step in persisted["steps"] if step["name"] == "startup-observations")
        self.assertEqual("passed", step["outcome"])
        self.assertEqual(2, step["configuredCycles"])
        self.assertEqual(self.startup_samples(), step["observations"])
        composition = next(step for step in persisted["steps"] if step["name"] == "startup-composition-observations")
        self.assertEqual("passed", composition["outcome"])
        self.assertEqual(self.startup_samples(True), composition["observations"])
        self.assertIn(runner.report.path, list((self.root / "artifacts/verification").glob("*.json")))
        self.assertNotIn("credential-canary", runner.report.path.read_text())
        self.assertNotIn(str(self.root), runner.report.path.read_text())
        self.assertTrue((outputs[0] / "actual-startup-test/startup-observations.json").is_file())
        self.assertEqual("credential-canary /private/old-results", stale.read_text())
        self.assertLess(commands.index("startup-storage-fixture-build"), commands.index("startup-warm-restarts"))
        self.assertLess(commands.index("startup-composition-fixture-build"), commands.index("startup-composition-restarts"))

    def test_composition_startup_requires_its_own_samples_and_total_with_cleanup(self):
        directory = self.root / "composition-startup"
        directory.mkdir()
        (directory / "startup-observations.json").write_text(json.dumps(self.startup_samples()))
        with self.assertRaisesRegex(verify.VerificationError, "startup_observations_missing"):
            verify.startup_observations(directory, 2, composition=True)
        path = directory / "composition-startup-observations.json"
        path.write_text(json.dumps(self.startup_samples(True)))
        self.assertEqual(self.startup_samples(True),
                         verify.startup_observations(directory, 2, composition=True))
        for total in (None, 300, 0.01, True):
            samples = self.startup_samples(True)
            if total is None:
                del samples[0]["totalSeconds"]
            else:
                samples[0]["totalSeconds"] = total
            path.write_text(json.dumps(samples))
            with self.subTest(total=total), \
                    self.assertRaisesRegex(verify.VerificationError, "startup_observations_invalid"):
                verify.startup_observations(directory, 2, composition=True)
        samples = self.startup_samples(True)
        samples[0]["modulesPublished"] = False
        path.write_text(json.dumps(samples))
        with self.assertRaisesRegex(verify.VerificationError, "startup_observations_invalid"):
            verify.startup_observations(directory, 2, composition=True)

    def test_startup_cannot_pass_with_only_empty_host_warm_observations(self):
        runner = verify.Runner(self.root, self.report("startup"), {})

        def child(name, command, **_kwargs):
            if name == "startup-warm-restarts":
                directory = Path(command[command.index("--output") + 1])
                (directory / "startup-observations.json").write_text(json.dumps(self.startup_samples()))

        with patch.object(runner, "versions"), patch.object(runner, "build_runtime"), \
                patch.object(runner, "command", side_effect=child), \
                self.assertRaisesRegex(verify.VerificationError, "startup_observations_missing"):
            verify.execute_suite(runner, "startup", None, Mock(warm_starts=2))
        self.assertEqual("startup-composition-observations", runner.report.data["steps"][-1]["name"])
        self.assertEqual("failed", runner.report.data["steps"][-1]["outcome"])

    def test_startup_requires_exact_sample_count_sequence_fields_and_fixture_matches(self):
        directory = self.root / "observations"
        directory.mkdir()
        path = directory / "startup-observations.json"
        invalid = [
            [], self.startup_samples()[:1], self.startup_samples() + self.startup_samples(),
            list(reversed(self.startup_samples())), {"samples": self.startup_samples()},
        ]
        for field, value in (("cycle", True), ("cycle", 1.0), ("fixtureMatches", False),
                             ("fixtureMatches", 1), ("extra", "credential-canary")):
            sample = self.startup_samples()
            sample[0][field] = value
            invalid.append(sample)
        missing = self.startup_samples()
        del missing[0]["pageSeconds"]
        invalid.append(missing)
        for index, samples in enumerate(invalid):
            with self.subTest(case=index):
                path.write_text(json.dumps(samples))
                with self.assertRaisesRegex(verify.VerificationError, "startup_observations_invalid"):
                    verify.startup_observations(directory, 2)
        for malformed in ('"credential-canary"', '{"cycle":1,"cycle":2}', "invalid-json"):
            path.write_text(malformed)
            with self.assertRaisesRegex(verify.VerificationError, "startup_observations_invalid"):
                verify.startup_observations(directory, 2)

    def test_startup_durations_must_be_finite_nonnegative_numeric_and_strictly_within_budget(self):
        directory = self.root / "observations"
        directory.mkdir()
        path = directory / "startup-observations.json"
        for field, limit in (("readySeconds", 285), ("pageSeconds", 2)):
            for value in (float("nan"), float("inf"), -float("inf"), -0.01, limit, True, "0.1"):
                with self.subTest(field=field, value=value):
                    samples = self.startup_samples()
                    samples[0][field] = value
                    path.write_text(json.dumps(samples))
                    with self.assertRaisesRegex(verify.VerificationError, "startup_observations_invalid"):
                        verify.startup_observations(directory, 2)
        samples = self.startup_samples()
        samples[0].update(readySeconds=0, pageSeconds=0)
        samples[1].update(readySeconds=284.999, pageSeconds=1.999)
        path.write_text(json.dumps(samples))
        self.assertEqual(samples, verify.startup_observations(directory, 2))

    def test_startup_missing_ambiguous_and_linked_observation_files_fail_closed(self):
        directory = self.root / "observations"
        directory.mkdir()
        with self.assertRaisesRegex(verify.VerificationError, "startup_observations_missing"):
            verify.startup_observations(directory, 2)
        path = directory / "startup-observations.json"
        path.write_text(json.dumps(self.startup_samples()))
        duplicate = directory / "duplicate/startup-observations.json"
        duplicate.parent.mkdir()
        duplicate.write_text(path.read_text())
        with self.assertRaisesRegex(verify.VerificationError, "startup_observations_invalid"):
            verify.startup_observations(directory, 2)
        duplicate.unlink()
        outside = self.root / "outside-sentinel"
        outside.write_text("credential-canary")
        path.unlink()
        path.symlink_to(outside)
        with self.assertRaisesRegex(verify.VerificationError, "startup_observations_invalid"):
            verify.startup_observations(directory, 2)
        self.assertEqual("credential-canary", outside.read_text())

    def test_successful_startup_requires_fresh_valid_samples_without_saving_invalid_payloads(self):
        stale = self.root / "tests/e2e/test-results/previous/startup-observations.json"
        stale.parent.mkdir(parents=True)
        stale.write_text(json.dumps(self.startup_samples()))
        invalid = self.startup_samples()
        invalid[0]["extra"] = "credential-canary /private/fixture"
        for payload, code in ((None, "missing"), (invalid, "invalid")):
            with self.subTest(code=code):
                runner = verify.Runner(self.root, self.report("startup"), {})

                def child(_name, command, **_kwargs):
                    if _name != "startup-warm-restarts":
                        return
                    if payload is not None:
                        directory = Path(command[command.index("--output") + 1])
                        (directory / "startup-observations.json").write_text(json.dumps(payload))

                with patch.object(runner, "versions"), patch.object(runner, "build_runtime"), \
                        patch.object(runner, "command", side_effect=child), \
                        self.assertRaisesRegex(verify.VerificationError, f"startup_observations_{code}"):
                    verify.execute_suite(runner, "startup", None, Mock(warm_starts=2))
                step = runner.report.data["steps"][-1]
                self.assertEqual("startup-observations", step["name"])
                self.assertEqual("failed", step["outcome"])
                self.assertNotIn("observations", step)
                self.assertNotIn("credential-canary", runner.report.path.read_text())
                self.assertNotIn("/private/fixture", runner.report.path.read_text())
        self.assertTrue(stale.is_file())

    def test_failed_startup_child_cannot_publish_partial_or_failed_samples(self):
        runner = verify.Runner(self.root, self.report("startup"), {})

        def child(name, command, **_kwargs):
            if name in ("startup-storage-fixture-build", "startup-composition-fixture-build"):
                runner.operation(name, lambda _: None, command)
                return
            def fail(_):
                directory = Path(command[command.index("--output") + 1])
                (directory / "startup-observations.json").write_text(json.dumps(self.startup_samples()))
                raise verify.VerificationError("command_failed")
            runner.operation(name, fail, command)

        with patch.object(runner, "versions"), patch.object(runner, "build_runtime"), \
                patch.object(runner, "command", side_effect=child), \
                patch.object(verify, "startup_observations") as parse, \
                self.assertRaisesRegex(verify.VerificationError, "command_failed"):
            verify.execute_suite(runner, "startup", None, Mock(warm_starts=2))
        parse.assert_not_called()
        self.assertFalse(any("observations" in step for step in runner.report.data["steps"]))


if __name__ == "__main__":
    unittest.main()
