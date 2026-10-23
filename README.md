<p align="center">
  <img src="docs/assets/kofte-logo.svg" alt="Köfte — a plate of char-grilled köfte, herbs and lemon" width="720">
</p>

<p align="center">
  <strong>Your platform. Your ingredients.</strong><br>
  A composable, self-hosted internal developer platform — built to fit your team, not the other way around.
</p>

<p align="center">
  <a href="LICENSE"><img alt="License: MIT" src="https://img.shields.io/badge/license-MIT-34d399?style=flat-square"></a>
  <img alt="Status: storage isolation accepted" src="https://img.shields.io/badge/status-storage_isolation_accepted-34d399?style=flat-square">
  <a href="docs/master-spec-inventory.md"><img alt="Built with Spec Kit" src="https://img.shields.io/badge/workflow-Spec_Kit-a78bfa?style=flat-square"></a>
</p>

<p align="center">
  <a href="docs/product-vision.md">🌍 Vision</a> ·
  <a href="docs/architecture.md">🧩 Architecture</a> ·
  <a href="docs/master-spec-inventory.md">🗺️ Roadmap</a> ·
  <a href="CONTRIBUTING.md">🤝 Contribute</a>
</p>

## 🍽️ Meet Köfte

Developers deserve a clear, welcoming home for services, owners, resources and guarded automations. Platform teams deserve the freedom to assemble that experience from modules instead of maintaining a fork.

**Köfte is being designed for both.** Its planned interfaces share the same operations and permissions, whether you work in a browser, call an API, or use an MCP-enabled coding assistant.

| Ingredient | What we're working toward |
| --- | --- |
| 🧩 Deep composability | Modules contributing backend features, UI, resources, events, workflows, permissions and MCP tools |
| 🏠 Self-hosted freedom | A lightweight SQLite profile, PostgreSQL for shared installations, and first-class Kubernetes support |
| 🤖 Assistant-native access | Early authenticated MCP tools, not a privileged side door |
| ✨ Thoughtful experience | A fast, accessible Next.js console with clear loading, empty, error and permission states |
| 🔐 Explicit boundaries | Team-scoped authorization, audited actions and module-owned storage |

> [!IMPORTANT]
> **Spec 001's bootstrap is human-accepted and merged, not a finished product.** A module-free .NET host, persistent local SQLite store, Next.js status page, local supervisor, owned PostgreSQL verification, guarded recovery and generated Development OpenAPI now exist. All 64 implementation tasks are evidenced, including the complete local gate, all ten actual Ubuntu CI jobs and human-confirmed T056/T059. Final review found no acceptance defect, and [explicit human acceptance](specs/001-repository-bootstrap/acceptance.md) is recorded. Spec 002's offline manifest-validation contracts are [explicitly human-accepted](specs/002-platform-contracts/acceptance.md) and human-merged. Spec 003's storage registry and module isolation are now [explicitly human-accepted](specs/003-storage-registry/acceptance.md), with human merge pending. Nothing here claims production readiness or MVP completion.

## 🧭 The shape of things to come

```mermaid
flowchart LR
    Developer["🧑‍💻 Developer"] --> Web["Web console"]
    Assistant["🤖 Coding assistant"] --> MCP["MCP tools"]
    Web --> Operations["Shared operations + permissions"]
    MCP --> Operations
    API["REST clients"] --> Operations
    Operations --> Modules["🧩 Composable modules"]
    Modules --> Storage["Module-owned storage"]
    Modules --> Integrations["Optional providers + automations"]
```

This is the **planned architecture**, not a diagram of implemented services. PostgreSQL uses a named schema per module; SQLite uses a physical file per module. Hoard, cloud providers and infrastructure engines stay optional.

## 🚀 Start here

1. Read the [product charter](docs/product-charter.md), [vision](docs/product-vision.md) and [architecture](docs/architecture.md).
2. Follow the [contributor workflow](CONTRIBUTING.md), [local bootstrap guide](docs/development/local-development.md) and [assistant/MCP setup](docs/development/assistant-tooling.md).
3. When ready to begin product work, invoke **`drive-speckit`**. Each invocation performs one stage and publishes its result on the **same branch and draft PR for that numbered feature**. No merges between steps; request human merge only after final review and explicit feature acceptance.

Before local startup, select the exact .NET/Node/npm versions in the
[local guide](docs/development/local-development.md) in the current shell.
Preflight identifies a missing or mismatched tool; it does not install tools
or use a different version merely because one is installed elsewhere.

The constitution's initial v1.0.0 was human-merged; its [v2.0.0 workflow amendment](.specify/memory/constitution.md) records the human-directed [one-feature/one-branch correction](docs/adr/0001-feature-delivery-lifecycle.md). Spec **001 / specify** has a [draft specification](specs/001-repository-bootstrap/spec.md) and its [author quality checklist](specs/001-repository-bootstrap/checklists/requirements.md), generated from the exact [inventory prompt](docs/master-spec-inventory.md). **Clarify, plan, checklist, tasks and analyze are complete:** no material requirements ambiguity remained; the [implementation plan](specs/001-repository-bootstrap/plan.md), research, data model, HTTP/UI contracts and future validation guide define the bounded foundation. At the reviewer's explicit request, agent-assisted evaluation of the [requirements-quality checklist](specs/001-repository-bootstrap/checklists/quality.md) satisfied all 36 criteria with evidence and found no blocking requirements gap. The [64-task implementation list](specs/001-repository-bootstrap/tasks.md) maps all four stories, 17 functional requirements and six measurable outcomes to path-specific work, explicit dependencies and mandatory validation. The read-only [analysis](specs/001-repository-bootstrap/analysis.md) passed with 23/23 requirements/outcomes and 15/15 acceptance scenarios covered, no unmapped tasks and no actionable or constitutional findings. This is design/task coverage, not runtime proof.

**Spec 001 is explicitly human-accepted and merged.** The [acceptance record](specs/001-repository-bootstrap/acceptance.md) preserves the exact decision, scope and limits. The [final review](specs/001-repository-bootstrap/review.md) independently traces all 17 functional requirements, six success criteria, 15 acceptance scenarios and twelve constitutional principles to source/tests, with no acceptance defect. Its complete local gate passes all 170 named steps and actual assessed-head Ubuntu CI has matching coverage. Human T056/T059 attestations and reference-machine limitations remain explicit. The earlier [convergence assessment](specs/001-repository-bootstrap/convergence.md), human-approved [ADR 0002](docs/adr/0002-bootstrap-development-baseline.md), [dependency adoption](docs/development/dependencies.md), [implementation evidence](specs/001-repository-bootstrap/implementation.md) and .NET test-source correction remain in force. [PR #4](https://github.com/Stonefly-Labs/Kofte/pull/4) was human-merged with its accepted ledger/evidence on main.

**Spec 003 is explicitly human-accepted; human merge is pending.**
The [acceptance record](specs/003-storage-registry/acceptance.md) preserves the
exact decision, **"this human accesspts 003"**, and the passing review's scope
and limits. The [review](specs/003-storage-registry/review.md) covers all 40
requirements/outcomes/scenarios, fourteen plan decisions, 83 tasks and twelve
principles with no remaining finding. Acceptance changes no product/test code,
tasks, reviewed checklists or prior evidence and adds no runtime measurement.
The same [PR #6](https://github.com/Stonefly-Labs/Kofte/pull/6) is ready for
human merge after publication. **004 must wait for that human merge** and
accepted evidence on updated main; it is not started here.

The following paragraphs preserve the implementation and earlier stage history.
The initial implementation checkpoint followed completed
specify through analyze and explicit human approval of
[ADR 0005](docs/adr/0005-module-storage-ownership.md).
The [specification](specs/003-storage-registry/spec.md), [plan](specs/003-storage-registry/plan.md)
and [task list](specs/003-storage-registry/tasks.md) retain the approved scope;
the author and reviewer-owned quality checklists remain 16/16 and 36/36.
Reviewed EF/provider dependencies, provider-neutral storage contracts and a
platform-local registry now support immutable allocation, explicit no-storage,
private SQLite files, offline PostgreSQL provisioning and marker verification.
Actual both-provider registration/constraint checks, assigned EF isolation,
the complete 20-by-ten connection/process race matrix, killed-process recovery,
30/120-second worker termination and all-seven-outcome privacy checks now have
focused evidence. All nine public examples execute, including corrected retry,
real interrupted provisioning and provider-backed complete-set restore.
The original PostgreSQL failures did not reproduce in the unchanged 105-case
baseline. Later Linux CI identified a process-worker acquisition deadline:
ownership verification and the first lease probe now share one guarded query,
and worker provider initialization completes before the ten-way race barrier.
Budgets and Busy-only retry rules are unchanged. The corrected complete local
gate passes all 200 steps. Assessed-head Linux CI also passes all ten product
jobs / 200 steps and repository checks after the browser-prerequisite mirror
correction documented in the local guide. The implementation checkpoint marked
all 78 original tasks complete; convergence identifies two remaining public-example
obligations below rather than inferring acceptance from those markers.
Baseline provider selection and warm-restart fixtures now honor the new
registry/module ownership boundary without weakening its checks.
See the [implementation report](specs/003-storage-registry/implementation.md)
for exact revisions, commands, results and supported limits, and the
[validation guide](specs/003-storage-registry/quickstart.md) for supported checks
and executable examples. Normal serving remains module-free; storage is not
a new endpoint, module loader or authorization bypass. Work stays on
`feature/003-storage-registry` and [draft PR #6](https://github.com/Stonefly-Labs/Kofte/pull/6).
The first [convergence assessment](specs/003-storage-registry/convergence.md)
identified two HIGH partial public-example gaps and appended **T079-T080**.
The [remediation increment](specs/003-storage-registry/implementation.md#convergence-remediation-t079-t080)
now makes `same-id-retry` release maintenance and launch three fresh processes,
each re-registering changed metadata and verifying two exact assignments and
retained sentinels. `collision` now forces a second distinct ID against a
registered owner's candidate, verifies the real UNIQUE rejection and unchanged
assignment/data/inventory, and retains the separate unowned-target refusal.
Standalone and `all` harnesses require these observations, not positive counts
alone. All 80 tasks are evidenced; no runtime storage rule or dependency changes.
The [follow-up convergence](specs/003-storage-registry/convergence.md) now
assesses all 40 requirements/outcomes/scenarios, fourteen plan decisions,
80 tasks and twelve principles against current source/tests, with zero actionable
gaps. Both public-example findings are closed; `tasks.md` is byte-for-byte
unchanged and no product/test code is edited by convergence. Actual assessed-head
CI passes all eleven contexts and 200 product steps. **Review is next** on this
same draft PR, followed separately by explicit numbered-feature acceptance.
The earlier gaps assessment is preserved as history; convergence does not
perform final review, accept the feature or request a merge.

The subsequent [final review](specs/003-storage-registry/review.md) records
**changes** for one MEDIUM evidence finding: the prescribed changed-allocator
retry of an existing mapping and persisted no-storage-to-relational refusal
lack regression assertions. Production code appears correct; no storage-engine
failure is demonstrated. Prior public F1/F2 are closed. Exactly **T081** is
appended, preserving all 80 historical task markers and earlier reports.
Fresh quality/unit/both-provider integration/recovery/contract/privacy gates
pass 157 targeted steps; actual assessed-head CI passes eleven contexts and
200 product steps. Passing existing tests does not supply the absent cases.
That review selected implement without a new approval or merge prerequisite.
The [T081 implementation](specs/003-storage-registry/implementation.md#final-review-remediation-t081)
now adds both inherited regressions: all four real-provider cases pass and
detect controlled behavioral mutations. Exact assignments, sentinels, revisions
and complete inventory remain unchanged after reopening. All 81 tasks are
evidenced; fresh quality/unit/integration gates pass 46 targeted steps after
an unchanged PostgreSQL rerun of two initial deadline failures. Production,
APIs and dependencies are unchanged.
The [current convergence](specs/003-storage-registry/convergence.md) now assesses
all 40 requirements/outcomes/scenarios, fourteen plan decisions, all 81 tasks
and twelve principles against actual source/assertions. R1/T081 and prior public
F1/F2 are closed, with zero actionable gaps and no appended task. Tasks,
product/test code, approved intent and the historical review remain unchanged.
That convergence selected review on the same draft PR. The subsequent
[follow-up final review](specs/003-storage-registry/review.md) confirms R1/T081
and public F1/F2 are closed, but identifies one MEDIUM implementation finding
R2: explicit registry-upgrade mutations and rejected boundaries bypass required
correlated storage records. Existing CLI success/generic failure summaries do
not supply the direct API's complete operation evidence. Exactly **T082** is
appended unchecked for safe direct-API/CLI telemetry and both-provider captures;
all 81 historical task bytes/markers, code, design and earlier reports remain
unchanged. No storage-isolation failure or new design decision is demonstrated.
Fresh relevant gates pass 157 targeted steps; actual assessed-head CI passes
eleven contexts and 200 product steps, not the absent upgrade-record assertions.
That review selected implement without accepting the feature or requesting merge.
The [T082 increment](specs/003-storage-registry/implementation.md#final-review-remediation-t082)
now emits correlated upgrade and failed-acquisition records with fixed guidance
and truthful durability, without manufacturing a module result. The CLI projects
allowlisted worker outcomes while continuing to discard raw child output.
Both-provider captures cover commit, idempotent retry, rejected scopes,
unsupported/corrupt registries and sanitized commit-boundary failures.
All 82 tasks were evidenced at that implementation checkpoint, which selected
follow-up converge. That assessment appended T083 without rewriting
those historical markers; the implementation increment above completes it,
while feature acceptance remains separate.

**Spec 002 is explicitly human-accepted and merged.**
The [acceptance record](specs/002-platform-contracts/acceptance.md) preserves
the exact decision, **"spec is fully human approved"**, in response to the
passing final review and explicit numbered-feature acceptance handoff.
Acceptance retains the approved scope and review limits; it adds no product
capability or new measurement. The same [PR #5](https://github.com/Stonefly-Labs/Kofte/pull/5)
was human-merged. Before starting 003, all five acceptance evidence hashes and
the accepted ledger were verified on updated `origin/main`; acceptance itself
did not begin the next feature.

The completed review checkpoint follows.
The [final review](specs/002-platform-contracts/review.md) independently traces
all 15 functional requirements, six amended outcomes, 14 scenarios, twelve plan
decisions, 49 tasks and twelve principles to actual source/tests. No acceptance
defect or remediation remains. Fresh quality/unit/both-provider contract checks
pass all 62 targeted steps; actual assessed-head CI passes all eleven contexts
and 177 product steps. This is not a new local all-suite or human-usability
claim. Review did not accept the feature; the separate explicit human decision
is now recorded above.

The preceding convergence/remediation checkpoint follows.
The [implementation increment](specs/002-platform-contracts/implementation.md#convergence-remediation-t049)
adds the documented public boundary recipes, reversed range and ordered
multi-error/truncation cases identified by the
[prior convergence assessment](specs/002-platform-contracts/convergence.md).
Every corpus case now runs 100 times through each public consumer in both real
`en-US` and `tr-TR` cultures, including recursively reordered properties;
corrected retry remains covered. Focused quality/unit/both-provider contract gates pass; no
production code or contract rule changed. The
[follow-up convergence](specs/002-platform-contracts/convergence.md) now assesses
all 35 requirements/outcomes/scenarios, twelve plan decisions, 49 tasks and
twelve principles with zero actionable gaps. It leaves `tasks.md` byte-for-byte
unchanged. The passing final review above is separate from human acceptance.

The original completed implementation checkpoint follows.
The [approved design](docs/adr/0004-module-manifest-contract.md) now has a pure
`Kofte.Platform` library with strict bounded JSON, immutable identity, SemVer
compatibility, safe ordered diagnostics and non-authoritative declarations.
The [executable author guide](specs/002-platform-contracts/quickstart.md) uses a
test-owned console; host-facing and structural consumers verify the same contract
without loading modules or adding routes/storage. [Implementation evidence](specs/002-platform-contracts/implementation.md)
records focused tests, observed privacy/retry behavior, dependency review and
complete implementation evidence. The human removed the fixed Ubuntu/CPU/RAM benchmark profile;
T044 is evidenced on the available supported workstation with actual metadata
and 100 individually timed checks. The human also removed T045's manual timed
exercise; actual automated guide/error/corrected-retry checks now satisfy it.
No human study or elapsed-time attestation is claimed. All 49
implementation tasks, including T049's corpus remediation, have recorded
evidence. Follow-up convergence and final review pass; the separate acceptance
record above now completes the feature's human decision.
The [task list, now including T049](specs/002-platform-contracts/tasks.md), passing
[analysis](specs/002-platform-contracts/analysis.md), 16/16 author checklist and
36/36 reviewer-authorized quality checklist preserve traceability, not acceptance.
All 002 stages stayed on `feature/002-platform-contracts` and
[PR #5](https://github.com/Stonefly-Labs/Kofte/pull/5) through human acceptance
and merge; there was no intermediate merge or inferred acceptance.
**Installing a skill does not execute it.**

```mermaid
flowchart LR
    Constitution --> Specify --> Clarify --> Plan --> Checklist --> Tasks
    Tasks --> Analyze --> Implement --> Converge --> Review --> Accept
    Converge -->|gaps| Implement
    Review -->|changes| Implement
    Accept -->|human approval| Next["Next open spec"]
```

See [Spec Kit workflow and recovery](docs/development/spec-workflow.md) for stage gates, durable state and acceptance.

## 🗂️ Repository map

```text
.github/       Contributor templates, Copilot instructions, skills, agents and CI
.specify/      Existing Spec Kit templates/scripts and tracked driver progress
.vscode/       Shared editor MCP configuration
apps/web/      Minimal Next.js bootstrap page and fixed readiness adapter
src/           .NET host and pure platform manifest/identity contracts
modules/       Reserved for independently packaged reference modules
packages/      Bootstrap contract; UI/module SDK placeholders remain reserved
tests/         Dedicated .NET test projects, HTTP contract and browser tests
deploy/        Pinned disposable PostgreSQL Compose; Helm remains reserved
scripts/       Repository/dependency checks, supervisor, verifier and their tests
specs/         Numbered Spec Kit artifacts (not docs/specs/)
docs/          Product context, decisions, contributor guides and visual assets
```

The bootstrap and approved platform manifest contracts are implemented: one host,
one web app and narrow shared contracts. Modules, module/UI SDKs, workers and Helm
remain reserved. No application tables, fake identity, feature/admin/MCP routes
or module isolation are introduced by this foundation.

.NET test source belongs inside its dedicated test project under `tests/`,
never in production projects or linked from production vertical slices. Test
projects may mirror feature ownership; see the human-approved
[source ownership decision](docs/adr/0003-dotnet-test-source-ownership.md).

## 📚 Project bookshelf

| Read | Why |
| --- | --- |
| [Product charter](docs/product-charter.md) | Mandate, boundaries and required outcomes |
| [Product vision](docs/product-vision.md) | People, experience and long-term direction |
| [Technical architecture](docs/architecture.md) | Canonical architecture requirements and proposals |
| [Constitution](.specify/memory/constitution.md) | Product principles and delivery gates; v2.0.1 clarifies .NET test-source ownership |
| [Master spec inventory](docs/master-spec-inventory.md) | Ordered prompts; **001–024 MVP**, **025–038 post-MVP** |
| [Contributing](CONTRIBUTING.md) | Fresh branches, checks, commits, pushes and PR handoffs |
| [Local development](docs/development/local-development.md) | Current bootstrap startup, prerequisites, checks and remaining limits |
| [Assistant tooling](docs/development/assistant-tooling.md) | Microsoft Learn, Context7, Next.js, shadcn/ui and Playwright |
| [Spec workflow](docs/development/spec-workflow.md) | One-stage driver, checkpoints and human acceptance |
| [Architecture decisions](docs/adr/README.md) | Recording consequential choices without silently changing invariants |

The [older stack exploration](docs/kofte-technology-stack-architecture.md) is background, not authority over the charter, architecture or exact inventory scope.

## 🤝 Build it together

Open a focused [issue](https://github.com/Stonefly-Labs/Kofte/issues/new/choose), discuss a proposed slice, and keep each PR reviewable. Every change updates the README and related docs where behavior, setup or progress changes.

**Humans merge and close PRs. Assistants never do.**

## 📄 License

[MIT](LICENSE) · Stonefly Labs

<p align="center"><sub>Small core. Great modules. A platform with room for your recipe. 🧆</sub></p>
