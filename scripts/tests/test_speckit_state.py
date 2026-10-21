import importlib.util
import json
from pathlib import Path
import tempfile
import unittest


ROOT = Path(__file__).resolve().parents[2]
MODULE_PATH = ROOT / ".github/skills/drive-speckit/scripts/state.py"
SPEC = importlib.util.spec_from_file_location("speckit_state", MODULE_PATH)
state = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(state)


class DriverTests(unittest.TestCase):
    def setUp(self):
        self.temporary = tempfile.TemporaryDirectory()
        self.addCleanup(self.temporary.cleanup)
        self.root = Path(self.temporary.name)
        for name in (
            ".specify/drive-state.json", "docs/master-spec-inventory.md",
            "docs/constitution-prompt.md", ".specify/memory/constitution.md",
        ):
            destination = self.root / name
            destination.parent.mkdir(parents=True, exist_ok=True)
            destination.write_bytes((ROOT / name).read_bytes())
        self.write_state({
            "schema_version": 1, "constitution": {"status": "pending"},
            "features": {}, "active": None, "history": [],
        })
        self.directory = self.root / "specs/001-bootstrap"
        self.branch = "feature/001-test-stage"

    def write_state(self, value):
        (self.root / ".specify/drive-state.json").write_text(json.dumps(value))

    def ledger(self):
        return state.Ledger(self.root)

    def write(self, path, text):
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(text)

    def adopt_constitution(self):
        ledger = self.ledger()
        ledger.begin("chore/constitution")
        self.write(
            self.root / ".specify/memory/constitution.md",
            "# Köfte Constitution\n\n**Version**: 1.0.0\n",
        )
        ledger.complete("chore/constitution", "Human-reviewed constitutional principles")
        ledger.save()

    def prepare(self, step, tasks="- [x] T001 Build and verify the slice\n"):
        self.adopt_constitution()
        ledger = self.ledger()
        ledger.begin(self.branch, "specs/001-bootstrap")
        ledger.state["features"]["001"]["next_step"] = step
        ledger.state["active"]["step"] = step
        ledger.save()
        for path, text in {
            "spec.md": "# User requirements\n",
            "plan.md": "# Implementation plan\n",
            "research.md": "# Decisions and evidence\n",
            "quickstart.md": "# Validation scenarios\n",
            "tasks.md": tasks,
            "checklists/requirements.md": "- [x] Requirements have acceptance criteria\n",
            "checklists/quality.md": "- [x] Boundaries are explicitly specified\n",
            "analysis.md": "**Result:** pass\n\nNo blocking artifact inconsistencies.\n",
            "implementation.md": "# Commands and results\n",
            "convergence.md": "**Result:** pass\n\nNo implementation gaps.\n",
            "review.md": "**Result:** pass\n\nAcceptance-to-check evidence.\n",
            "acceptance.md": "# Human decision and evidence\n",
        }.items():
            self.write(self.directory / path, text)
        return self.ledger()

    def test_initial_inventory_and_prompt_are_exact(self):
        ledger = self.ledger()
        self.assertEqual(len(ledger.inventory), 38)
        self.assertEqual(ledger.next()["step"], "constitution")
        prompt_file = (self.root / "docs/constitution-prompt.md").read_text()
        self.assertIn(f"```text\n{ledger.prompt()}\n```", prompt_file)
        self.assertIsNone(ledger.state["active"])

    def test_constitution_template_cannot_complete(self):
        self.write(
            self.root / ".specify/memory/constitution.md",
            (ROOT / ".specify/templates/constitution-template.md").read_text(),
        )
        ledger = self.ledger()
        ledger.begin("chore/constitution")
        with self.assertRaisesRegex(ValueError, "still a template"):
            ledger.complete("chore/constitution", "Not actually generated")
        self.assertEqual(ledger.state["constitution"]["status"], "pending")

    def test_constitution_is_its_own_persisted_stage(self):
        self.adopt_constitution()
        ledger = self.ledger()
        self.assertEqual((ledger.next()["spec"], ledger.next()["step"]), ("001", "specify"))
        self.assertEqual(ledger.state["features"], {})
        self.assertEqual(len(ledger.state["history"]), 1)
        self.assertIn("Specify Köfte feature 001:", ledger.prompt())
        ledger.begin(self.branch, "specs/001-bootstrap")
        ledger.save()
        self.assertEqual(self.ledger().next()["step"], "specify")
        self.assertEqual(self.ledger().next()["status"], "running")

    def test_number_and_directory_cannot_be_renumbered_or_escape(self):
        self.adopt_constitution()
        for directory in (
            "specs/002-wrong", "../001-escape", "specs/001-a/child",
            "/specs/001-absolute", "docs/specs/001-wrong",
        ):
            with self.subTest(directory=directory), self.assertRaises(ValueError):
                self.ledger().begin(self.branch, directory)

    def test_symlinked_artifact_cannot_escape_repo(self):
        ledger = self.prepare("specify")
        path = self.directory / "spec.md"
        path.unlink()
        path.symlink_to(Path(self.temporary.name).parent / "outside.md")
        with self.assertRaisesRegex(ValueError, "escapes repository"):
            ledger.complete(self.branch, "Invalid evidence")

    def test_existing_untracked_spec_requires_reconciliation(self):
        self.adopt_constitution()
        self.directory.mkdir(parents=True)
        with self.assertRaisesRegex(ValueError, "Untracked spec artifacts"):
            self.ledger().begin(self.branch, "specs/001-bootstrap")

    def test_one_completion_advances_only_one_step_and_hashes_evidence(self):
        ledger = self.prepare("specify")
        entry = ledger.complete(self.branch, "Requirements reviewed")
        ledger.save()
        self.assertEqual(self.ledger().next()["step"], "clarify")
        self.assertIsNone(ledger.state["active"])
        self.assertIn("specs/001-bootstrap/spec.md", entry["evidence_sha256"])
        with self.assertRaisesRegex(ValueError, "No active stage"):
            ledger.complete(self.branch, "Must not chain")

    def test_active_stage_blocks_duplicate_or_wrong_branch_and_survives_reload(self):
        ledger = self.prepare("clarify")
        with self.assertRaisesRegex(ValueError, "already active"):
            ledger.begin(self.branch)
        with self.assertRaisesRegex(ValueError, "original branch"):
            ledger.complete("feature/another-branch", "Wrong branch")
        ledger.block(self.branch, "Awaiting an answer")
        ledger.save()
        ledger = self.ledger()
        self.assertEqual(ledger.next()["reason"], "Awaiting an answer")
        with self.assertRaisesRegex(ValueError, "blocked"):
            ledger.complete(self.branch, "Not resolved")
        ledger.resume(self.branch)
        ledger.complete(self.branch, "Answer recorded")
        self.assertEqual(ledger.next()["step"], "plan")

    def test_unanswered_clarification_prevents_plan(self):
        ledger = self.prepare("clarify")
        self.write(self.directory / "spec.md", "[NEEDS CLARIFICATION: Choose scope]\n")
        with self.assertRaisesRegex(ValueError, "clarification"):
            ledger.complete(self.branch, "Not resolved")

    def test_missing_artifact_and_unchecked_quality_prevent_advancement(self):
        ledger = self.prepare("plan")
        (self.directory / "research.md").unlink()
        with self.assertRaisesRegex(ValueError, "Missing or empty"):
            ledger.complete(self.branch, "Not researched")
        ledger.state["features"]["001"]["next_step"] = "checklist"
        ledger.state["active"]["step"] = "checklist"
        self.write(self.directory / "checklists/quality.md", "- [ ] Reviewer decision needed\n")
        with self.assertRaisesRegex(ValueError, "reviewer completion"):
            ledger.complete(self.branch, "Not reviewed")

    def test_analysis_requires_explicit_passing_evidence(self):
        ledger = self.prepare("analyze", tasks="- [ ] T001 Implement approved intent\n")
        self.write(self.directory / "analysis.md", "# Critical findings unresolved\n")
        with self.assertRaisesRegex(ValueError, "must record"):
            ledger.complete(self.branch, "Still blocked")
        self.write(self.directory / "analysis.md", "**Result:** pass\n\nFindings resolved.\n")
        ledger.complete(self.branch, "Cross-artifact evidence reviewed")
        self.assertEqual(ledger.next()["step"], "implement")

    def test_open_tasks_prevent_implementation_completion(self):
        ledger = self.prepare("implement", tasks="- [ ] T001 Incomplete work\n")
        with self.assertRaisesRegex(ValueError, "open tasks"):
            ledger.complete(self.branch, "Not done")

    def test_convergence_and_review_route_defects_back_to_implement(self):
        ledger = self.prepare("converge", tasks="- [ ] T002 Remaining criterion\n")
        self.write(self.directory / "convergence.md", "**Result:** gaps\n\nT002 is missing.\n")
        ledger.complete(self.branch, "Appended T002", outcome="gaps")
        self.assertEqual(ledger.next()["step"], "implement")
        ledger.state["features"]["001"]["next_step"] = "review"
        ledger.begin("feature/001-review")
        self.write(self.directory / "review.md", "**Result:** changes\n\nT002 remains.\n")
        ledger.complete("feature/001-review", "Defect requires remediation", outcome="changes")
        self.assertEqual(ledger.next()["step"], "implement")

    def test_acceptance_requires_human_decision_and_selects_next_spec(self):
        ledger = self.prepare("accept")
        with self.assertRaisesRegex(ValueError, "explicit human"):
            ledger.complete(self.branch, "Assistant cannot accept")
        entry = ledger.complete(
            self.branch, "Review and evidence accepted",
            approval="I accept spec 001 based on the review.",
        )
        ledger.save()
        self.assertEqual(entry["human_approval"], "I accept spec 001 based on the review.")
        self.assertEqual(self.ledger().next()["spec"], "002")
        self.assertNotIn("002", ledger.state["features"])

    def test_post_mvp_is_not_automatically_selected(self):
        self.adopt_constitution()
        ledger = self.ledger()
        for number in list(ledger.inventory)[:24]:
            ledger.state["features"][number] = {
                "directory": f"specs/{number}-test", "next_step": "accepted",
            }
        self.assertEqual(ledger.next()["status"], "post-mvp-paused")
        self.assertEqual(ledger.next(include_post_mvp=True)["spec"], "025")
        with self.assertRaisesRegex(ValueError, "post-mvp-paused"):
            ledger.begin("feature/025-specify", "specs/025-test")

    def test_full_feature_cycle_advances_one_stage_per_reload(self):
        ledger = self.prepare("specify")
        for index, step in enumerate(state.STEPS):
            with self.subTest(step=step):
                self.assertEqual(ledger.next()["step"], step)
                branch = self.branch if index == 0 else f"feature/001-{step}"
                if index:
                    ledger.begin(branch)
                ledger.complete(
                    branch, f"Evidence reviewed for {step}",
                    approval="I accept spec 001." if step == "accept" else None,
                )
                ledger.save()
                ledger = self.ledger()
                self.assertIsNone(ledger.state["active"])
        self.assertEqual(ledger.next()["spec"], "002")
        self.assertEqual(len(ledger.state["history"]), len(state.STEPS) + 1)

    def test_corruption_and_lock_conflicts_fail_explicitly(self):
        with state.lock(self.root):
            with self.assertRaisesRegex(ValueError, "locked"):
                with state.lock(self.root):
                    self.fail("Concurrent write must not proceed")
        self.assertFalse((self.root / ".specify/drive-state.json.lock").exists())
        self.write_state({"schema_version": 99})
        with self.assertRaisesRegex(ValueError, "Invalid driver ledger"):
            self.ledger()

    def test_inventory_missing_prompt_is_an_error(self):
        self.write(self.root / "docs/master-spec-inventory.md", "### 001 Test\nNo fenced prompt\n")
        with self.assertRaisesRegex(ValueError, "Missing or duplicate"):
            self.ledger()


if __name__ == "__main__":
    unittest.main()
