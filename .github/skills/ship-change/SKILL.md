---
name: ship-change
description: Finish an approved Köfte change with focused checks, documentation, a commit, immediate push and a human-owned PR handoff. Use when asked to commit, publish or open a PR; never merge or close it.
---

# Ship a focused change

1. Read the repository instructions and `CONTRIBUTING.md`. Inspect worktree, branch,
   upstream and existing PR. Never publish unrelated edits, work on main or reuse
   a merged/closed PR branch. Confirm a fresh main-based task branch or an explicitly
   approved/resumed task branch before changing anything.
2. Review the entire intended diff. Update README/related docs where affected.
   For Spec Kit, include stage evidence and `.specify/drive-state.json`; never
   equate this bootstrap with completion of product spec 001.
3. Run repository checks and applicable focused product checks. Review outputs,
   scope, ignored files and secrets. Resolve failures caused by the change.
   Incomplete work uses a draft PR with a clear blocker.
4. Stage **explicit paths** belonging to this task, not an indiscriminate `git add .`.
   Commit with an intent-based message and the required Copilot co-author trailer.
   **Push immediately after every commit** using `git push -u origin HEAD`.
   Do not amend, rewrite published history or force-push without explicit approval.
5. Use `gh pr list --head BRANCH --state all` before creating a PR. Create to main
   with `gh pr create` if no task PR exists; otherwise update the open task PR.
   Use the repository PR template and exact check outcomes. Do not attach secrets
   or private browser/session output.
6. Verify the PR URL, base/head, draft state, commits and checks. Wait for required
   checks if available; fix failures belonging to this task and push follow-ups.
7. Request a human merge with `Please merge [#N](PR_URL)` only when ready.
   **Never merge, enable auto-merge, close the PR or delete its branch.**
   Do not continue into another Spec Kit stage after publishing.
