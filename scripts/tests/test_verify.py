from contextlib import contextmanager, redirect_stdout
import io
import json
from pathlib import Path
import shutil
import subprocess
import tempfile
import unittest
from unittest.mock import Mock, patch

from scripts import verify


class VerifyTests(unittest.TestCase):
    def setUp(self):
        base = verify.ROOT / "artifacts/python-tests"
        base.mkdir(parents=True, exist_ok=True)
        self.temporary = tempfile.TemporaryDirectory(dir=base)
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name).resolve()
        for name in (*verify.PLATFORM_REQUIRED_FILES, *verify.STORAGE_REQUIRED_FILES):
            path = self.root / name
            path.parent.mkdir(parents=True, exist_ok=True)
            path.write_text("required platform fixture")
        (self.root / "tests/integration/Kofte.TestSupport/StorageScenarioCatalog.cs").write_text(
            " ".join(json.dumps(name) for name in verify.STORAGE_SCENARIOS))
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

    def test_storage_selectors_retain_provider_intersection_and_fail_on_zero_selected_cases(self):
        for provider in ("sqlite", "postgres"):
            for suite in ("recovery", "privacy"):
                command = verify.integration_command(provider, suite, project=verify.STORAGE_INTEGRATION)
                self.assertIn(verify.STORAGE_INTEGRATION, command)
                self.assertIn(f"/[(Suite={suite})&(Provider={provider})]", command)
                self.assertEqual(["--minimum-expected-tests", "1"], command[-2:])
                self.assertEqual(command, verify.safe_command(command))

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

    def test_successful_browser_command_never_publishes_failure_diagnostics(self):
        report = self.report("browser")
        runner = verify.Runner(self.root, report, {})
        diagnostic = verify.command_diagnostics(["npm", "run", "test", "--workspace", "tests/e2e"])
        runner.command("browser-tests", [
            "python3", "-c", "print('credential-canary /private/tests/e2e/recovery.spec.ts:7:8')",
        ], diagnostics=diagnostic)
        self.assertNotIn("sourceLocations", report.data["steps"][-1])
        self.assertNotIn("credential-canary", report.path.read_text())
        self.assertEqual(set(), diagnostic.sources)

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
                patch.object(verify, "runtime", side_effect=owned_runtime):
            verify.execute_suite(runner, "recovery", "both", arguments)
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
                capture = next(call for call in command.call_args_list
                               if call.args[0] == "privacy-supervisor-capture")
                self.assertIn("scripts/run_python_tests.py", capture.args[1])
                self.assertIn("test_dev_output.py", capture.args[1])
            else:
                public = next(call for call in command.call_args_list
                              if call.args[0] == "platform-public-contracts")
                self.assertEqual(["--minimum-expected-tests", "1"], public.args[1][-2:])
                self.assertEqual("true", public.kwargs["environment"]["KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE"])
                author = next(i for i, call in enumerate(command.call_args_list)
                              if call.args[0] == "manifest-author-build")
                contracts = next(i for i, call in enumerate(command.call_args_list)
                                 if call.args[0] == "platform-public-contracts")
                self.assertLess(author, contracts)
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

    def test_normal_browser_does_not_run_privacy_without_its_seeded_capture_session(self):
        directory = self.root / "tests/e2e"
        directory.mkdir(parents=True, exist_ok=True)
        for name in ("bootstrap.spec.ts", "recovery.spec.ts", "startup.spec.ts", "privacy.spec.ts"):
            (directory / name).write_text("isolated selector fixture")
        self.assertEqual(["bootstrap.spec.ts", "recovery.spec.ts"], verify.browser_specs(self.root))

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

        def initialize(_name, _command, **kwargs):
            database = Path(kwargs["environment"]["KOFTE_DATA_ROOT"]) / "platform/platform.db"
            database.parent.mkdir()
            database.write_bytes(b"owned store")

        with patch.object(runner, "build_runtime"), patch.object(runner, "command", side_effect=initialize), \
                patch.object(verify, "OwnedProcesses", Owned), patch.object(verify.dev, "wait_ready"):
            with verify.runtime(runner, "sqlite", False, privacy_canary="current-canary") as session:
                directory = session.directory
                self.assertEqual("current-canary", session.environment["BOOTSTRAP_PRIVACY_CANARY"])
                self.assertEqual(0o700, Path(session.environment["BOOTSTRAP_TEST_CONTROL"]).stat().st_mode & 0o777)
                self.assertRegex(session.environment["BOOTSTRAP_TEST_RUN_ID"], "^[a-f0-9]{32}$")
                session.worker.actions["stop-host"]()
                session.worker.actions["start-host"]()
                session.check()
                hosts = [group for group in groups if ["dotnet", verify.HOST] in group.commands]
                self.assertEqual(2, len(hosts))
                self.assertTrue(hosts[0].stopped)
                self.assertEqual(hosts[0].environment, hosts[1].environment)
                for group in groups:
                    self.assertFalse(any(key.startswith("BOOTSTRAP_") for key in group.environment))
                    self.assertEqual("/pinned/sdk", group.environment["DOTNET_ROOT"])
        self.assertFalse(directory.exists())
        self.assertTrue(all(group.stopped for group in groups))

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

    def startup_samples(self):
        return [
            {"cycle": cycle, "readySeconds": cycle / 10, "pageSeconds": cycle / 100, "fixtureMatches": True}
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
            if name == "startup-storage-fixture-build":
                self.assertIn("tests/contract/Kofte.Storage.Consumer", command)
                runner.operation(name, lambda _: None, command)
                return
            def produce(_):
                directory = Path(command[command.index("--output") + 1])
                outputs.append(directory)
                self.assertEqual([], list(directory.iterdir()))
                self.assertEqual("2", kwargs["environment"]["BOOTSTRAP_WARM_STARTS"])
                path = directory / "actual-startup-test/startup-observations.json"
                path.parent.mkdir()
                path.write_text(json.dumps(self.startup_samples()))
            runner.operation(name, produce, command)

        with patch.object(runner, "versions"), patch.object(runner, "build_runtime"), \
                patch.object(runner, "command", side_effect=child):
            verify.execute_suite(runner, "startup", None, Mock(warm_starts=2))
        persisted = json.loads(runner.report.path.read_text())
        step = next(step for step in persisted["steps"] if step["name"] == "startup-observations")
        self.assertEqual("passed", step["outcome"])
        self.assertEqual(2, step["configuredCycles"])
        self.assertEqual(self.startup_samples(), step["observations"])
        self.assertIn(runner.report.path, list((self.root / "artifacts/verification").glob("*.json")))
        self.assertNotIn("credential-canary", runner.report.path.read_text())
        self.assertNotIn(str(self.root), runner.report.path.read_text())
        self.assertTrue((outputs[0] / "actual-startup-test/startup-observations.json").is_file())
        self.assertEqual("credential-canary /private/old-results", stale.read_text())
        self.assertLess(commands.index("startup-storage-fixture-build"), commands.index("startup-warm-restarts"))

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
            if name == "startup-storage-fixture-build":
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
