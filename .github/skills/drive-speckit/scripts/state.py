"""Persist one Spec Kit stage at a time; never run product or Git operations."""

import argparse
from contextlib import contextmanager
from datetime import datetime, timezone
import hashlib
import json
from pathlib import Path
import re
import sys


STEPS = (
    "specify", "clarify", "plan", "checklist", "tasks", "analyze",
    "implement", "converge", "review", "accept",
)
PLACEHOLDERS = re.compile(
    r"\[(?:PROJECT_NAME|PRINCIPLE_\d+_[A-Z_]+|CONSTITUTION_VERSION|"
    r"RATIFICATION_DATE|LAST_AMENDED_DATE)\]"
)
TASK = re.compile(r"^\s*- \[([ xX])\] T\d+\b", re.MULTILINE)
CHECKBOX = re.compile(r"^\s*- \[([ xX])\]", re.MULTILINE)


def timestamp():
    return datetime.now(timezone.utc).isoformat(timespec="seconds")


class Ledger:
    def __init__(self, root):
        self.root = Path(root).resolve()
        self.path = self.root / ".specify/drive-state.json"
        self.state = json.loads(self.path.read_text())
        state = self.state
        if (
            state.get("schema_version") != 1
            or state.get("constitution", {}).get("status") not in {"pending", "complete"}
            or not isinstance(state.get("features"), dict)
            or not isinstance(state.get("history"), list)
            or "active" not in state
        ):
            raise ValueError("Invalid driver ledger; restore/reconcile it, do not reset progress.")
        self.inventory = self.read_inventory()
        for number, feature in state["features"].items():
            if (
                number not in self.inventory
                or feature.get("next_step") not in (*STEPS, "accepted")
            ):
                raise ValueError(f"Invalid feature state: {number}")
            self.feature_path(number, feature["directory"])
        active = state["active"]
        if active is not None:
            if (
                active.get("status") not in {"running", "blocked"}
                or active.get("step") not in ("constitution", *STEPS)
                or not active.get("branch")
                or not isinstance(active.get("reason"), str)
            ):
                raise ValueError("Invalid active stage; reconcile the ledger.")
            if active["step"] == "constitution":
                valid = active.get("spec") is None and state["constitution"]["status"] == "pending"
            else:
                feature = state["features"].get(active.get("spec"))
                valid = feature is not None and feature["next_step"] == active["step"]
            if not valid:
                raise ValueError("Active stage does not match recorded progress.")

    def read_inventory(self):
        text = (self.root / "docs/master-spec-inventory.md").read_text()
        pattern = r"^### (\d{3}) [^\n]+\n(.*?)(?=^### \d{3} |^## |\Z)"
        result = {}
        for match in re.finditer(pattern, text, re.MULTILINE | re.DOTALL):
            number, section = match.groups()
            prompt = re.search(r"```text\n(.*?)\n```", section, re.DOTALL)
            if number in result or prompt is None:
                raise ValueError(f"Missing or duplicate inventory prompt: {number}")
            result[number] = prompt.group(1)
        if not result or list(result) != sorted(result):
            raise ValueError("Inventory must contain ordered numbered specs with text prompts.")
        return result

    def feature_path(self, number, directory):
        path = self.root / directory
        if (
            Path(directory).is_absolute()
            or Path(directory).parts[:1] != ("specs",)
            or len(Path(directory).parts) != 2
            or not re.fullmatch(rf"{number}-[a-z0-9]+(?:-[a-z0-9]+)*", path.name)
            or not path.resolve().is_relative_to(self.root / "specs")
        ):
            raise ValueError("Feature directory must be specs/NNN-lowercase-short-name.")
        return path

    def require_constitution(self):
        path = self.root / ".specify/memory/constitution.md"
        text = path.read_text()
        if PLACEHOLDERS.search(text) or not re.search(r"\*\*Version\*\*:?\s+\d+\.\d+\.\d+", text):
            raise ValueError("Constitution is still a template or has no explicit version.")
        return path

    def next(self, include_post_mvp=False):
        if self.state["active"] is not None:
            return dict(self.state["active"])
        if self.state["constitution"]["status"] == "pending":
            return {"spec": None, "step": "constitution", "status": "ready"}
        self.require_constitution()
        for number in self.inventory:
            feature = self.state["features"].get(number)
            if feature is not None and feature["next_step"] == "accepted":
                continue
            if int(number) > 24 and not include_post_mvp:
                return {"spec": number, "step": None, "status": "post-mvp-paused"}
            return {
                "spec": number,
                "step": feature["next_step"] if feature else "specify",
                "directory": feature["directory"] if feature else None,
                "status": "ready",
            }
        return {"spec": None, "step": None, "status": "finished"}

    def prompt(self, include_post_mvp=False):
        current = self.next(include_post_mvp)
        if current["step"] == "constitution":
            text = (self.root / "docs/constitution-prompt.md").read_text()
            prompts = re.findall(r"```text\n(.*?)\n```", text, re.DOTALL)
            if len(prompts) != 1:
                raise ValueError("Expected exactly one fenced constitution prompt.")
            return prompts[0]
        if current["step"] == "specify":
            return self.inventory[current["spec"]]
        raise ValueError("The current stage has no inventory prompt; read its stage instructions.")

    def begin(self, branch, directory=None, include_post_mvp=False):
        if self.state["active"] is not None:
            raise ValueError("Stage already active; resume its original branch instead of beginning again.")
        if not re.fullmatch(r"(?:chore|feature|bugfix|docs|refactor|test)/[a-z0-9][a-z0-9/-]*", branch):
            raise ValueError("Use a typed task branch, never main.")
        current = self.next(include_post_mvp)
        if current["status"] != "ready":
            raise ValueError(f"Cannot begin: {current['status']}")
        number = current["spec"]
        if number is not None:
            existing = self.state["features"].get(number)
            if existing is None:
                if directory is None:
                    raise ValueError("First specify stage requires --directory specs/NNN-short-name.")
                if any(entry["branch"] == branch for entry in self.state["history"]):
                    raise ValueError("Start a new feature on a fresh branch, not a completed task branch.")
                self.feature_path(number, directory)
                matches = list((self.root / "specs").glob(f"{number}-*"))
                if matches:
                    raise ValueError("Untracked spec artifacts exist; reconcile instead of overwriting.")
                existing = {"directory": directory, "next_step": "specify"}
                self.state["features"][number] = existing
            else:
                if directory is not None and directory != existing["directory"]:
                    raise ValueError("Feature directory is immutable; reconcile before changing it.")
                branches = {
                    entry["branch"] for entry in self.state["history"]
                    if entry["spec"] == number
                }
                if len(branches) != 1:
                    raise ValueError("Feature branch history needs human reconciliation.")
                if branch not in branches:
                    raise ValueError(
                        f"Continue feature {number} on its original branch: {next(iter(branches))}."
                    )
            current["directory"] = existing["directory"]
        current.update(status="running", branch=branch, reason="", started_at=timestamp())
        self.state["active"] = current
        return current

    def active(self, branch):
        active = self.state["active"]
        if active is None or active["branch"] != branch:
            raise ValueError("No active stage on this branch; use its recorded original branch.")
        return active

    def block(self, branch, reason):
        if not reason.strip():
            raise ValueError("A blocked stage requires an actionable reason.")
        self.active(branch).update(status="blocked", reason=reason)

    def resume(self, branch):
        self.active(branch).update(status="running", reason="")

    def require_file(self, path):
        if not path.resolve().is_relative_to(self.root):
            raise ValueError(f"Artifact escapes repository: {path}")
        if not path.is_file() or not path.read_text().strip():
            raise ValueError(f"Missing or empty stage artifact: {path.relative_to(self.root)}")
        return path

    def require_checklists(self, directory):
        paths = sorted((directory / "checklists").glob("*.md"))
        if not paths:
            raise ValueError("Missing feature checklists.")
        for path in paths:
            markers = CHECKBOX.findall(self.require_file(path).read_text())
            if not markers or " " in markers:
                raise ValueError(f"Checklist needs reviewer completion: {path.relative_to(self.root)}")
        return paths

    def complete(self, branch, note, outcome="pass", approval=None):
        active = self.active(branch)
        if active["status"] != "running":
            raise ValueError("Stage is blocked; resolve its reason and resume first.")
        if not note.strip():
            raise ValueError("Completion requires an evidence/decision note.")
        step, number = active["step"], active["spec"]
        if outcome != "pass" and (step, outcome) not in {("converge", "gaps"), ("review", "changes")}:
            raise ValueError("Invalid outcome for this stage.")
        if step == "constitution":
            paths = [self.require_constitution()]
            next_step = "specify"
        else:
            feature = self.state["features"][number]
            directory = self.feature_path(number, feature["directory"])
            artifacts = {
                "specify": ["spec.md", "checklists/requirements.md"],
                "clarify": ["spec.md"],
                "plan": ["plan.md", "research.md", "quickstart.md"],
                "checklist": ["checklists/quality.md"],
                "tasks": ["tasks.md"],
                "analyze": ["analysis.md"],
                "implement": ["tasks.md", "implementation.md"],
                "converge": ["tasks.md", "convergence.md"],
                "review": ["tasks.md", "review.md"],
                "accept": ["tasks.md", "review.md", "acceptance.md"],
            }
            paths = [self.require_file(directory / item) for item in artifacts[step]]
            if step in {"clarify", "plan"}:
                for path in (directory / "spec.md", *paths):
                    if "NEEDS CLARIFICATION" in self.require_file(path).read_text():
                        raise ValueError("Resolve clarification markers before advancing.")
            if step in {"checklist", "analyze", "implement", "review", "accept"}:
                paths.extend(self.require_checklists(directory))
            if step in {"tasks", "implement", "converge", "review", "accept"}:
                markers = TASK.findall((directory / "tasks.md").read_text())
                if not markers:
                    raise ValueError("tasks.md has no numbered task checkboxes.")
                open_tasks = markers.count(" ")
                if step != "tasks":
                    needs_work = outcome in {"gaps", "changes"}
                    if needs_work != (open_tasks > 0):
                        raise ValueError("Outcome disagrees with open tasks; reconcile tasks and evidence.")
            if step in {"analyze", "converge", "review", "accept"}:
                report = {
                    "analyze": "analysis.md", "converge": "convergence.md",
                    "review": "review.md", "accept": "review.md",
                }[step]
                if f"**Result:** {outcome}" not in (directory / report).read_text():
                    raise ValueError(f"{report} must record **Result:** {outcome}")
            if step == "accept" and (not approval or not approval.strip()):
                raise ValueError("Acceptance requires --approval quoting the explicit human decision.")
            next_step = "accepted" if step == "accept" else STEPS[STEPS.index(step) + 1]
            if outcome in {"gaps", "changes"}:
                next_step = "implement"
        evidence = {
            str(path.relative_to(self.root)): hashlib.sha256(path.read_bytes()).hexdigest()
            for path in paths
        }
        entry = {
            **active, "status": "complete", "completed_at": timestamp(),
            "outcome": outcome, "note": note, "evidence_sha256": evidence,
        }
        if approval is not None:
            entry["human_approval"] = approval
        self.state["history"].append(entry)
        if step == "constitution":
            self.state["constitution"]["status"] = "complete"
        else:
            self.state["features"][number]["next_step"] = next_step
        self.state["active"] = None
        return entry

    def save(self):
        temporary = self.path.with_suffix(".json.tmp")
        temporary.write_text(json.dumps(self.state, indent=2, ensure_ascii=False) + "\n")
        temporary.replace(self.path)


@contextmanager
def lock(root):
    path = root / ".specify/drive-state.json.lock"
    try:
        with path.open("x") as handle:
            handle.write(f"{timestamp()}\n")
    except FileExistsError:
        raise ValueError("Ledger is locked; check for another writer before removing a stale lock.") from None
    try:
        yield
    finally:
        path.unlink()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("command", choices=("status", "prompt", "begin", "complete", "block", "resume"))
    parser.add_argument("--branch")
    parser.add_argument("--directory")
    parser.add_argument("--note", default="")
    parser.add_argument("--outcome", choices=("pass", "gaps", "changes"), default="pass")
    parser.add_argument("--approval")
    parser.add_argument("--include-post-mvp", action="store_true")
    args = parser.parse_args()
    root = Path(__file__).resolve().parents[4]
    try:
        if args.command in {"status", "prompt"}:
            ledger = Ledger(root)
            if args.command == "prompt":
                print(ledger.prompt(args.include_post_mvp))
            else:
                print(json.dumps(ledger.next(args.include_post_mvp), indent=2))
            return
        if args.branch is None:
            raise ValueError("Mutations require --branch from git branch --show-current.")
        with lock(root):
            ledger = Ledger(root)
            if args.command == "begin":
                result = ledger.begin(args.branch, args.directory, args.include_post_mvp)
            elif args.command == "complete":
                result = ledger.complete(args.branch, args.note, args.outcome, args.approval)
            elif args.command == "block":
                ledger.block(args.branch, args.note)
                result = ledger.next()
            else:
                ledger.resume(args.branch)
                result = ledger.next()
            ledger.save()
        print(json.dumps(result, indent=2))
    except (OSError, ValueError, KeyError, TypeError, AttributeError) as error:
        print(f"drive-speckit: {error}", file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()
