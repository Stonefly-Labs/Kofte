# 0006: Explicit trusted composition and supervised module lifecycle

**Status:** Accepted for implementation
**Date:** 2026-10-22
**Spec:** [004 - Module composition and lifecycle](../../specs/004-module-lifecycle/spec.md)

## Context

Accepted 002 defines immutable manifest identities/ranges/category metadata;
accepted 003 defines real owner-bound stores, offline provisioning authority,
exclusive supervised migrations and explicit recovery. Feature 004 must compose
trusted modules and expose honest lifecycle/health without turning metadata
into permissions, privileging ordinary serving or claiming that cancellation
stops arbitrary in-process callbacks.

## Decision

Use an explicit **compiled deployment catalog** of copied inert manifests/
claims and uninvoked author delegates. Disabled modules have no runtime
construction or lifecycle calls. Operators persist explicit desired selection
and owner-scoped settings in a private versioned JSON file, separate from
process-local attempts/health. No directory scanning, network install,
hot-unload, new registry format or settings/audit product.

Reuse 002 syntax and version membership. Bound inputs and reject overflow;
choose the ordinal-smallest ready ID in dependency ordering. Validate the
whole graph, settings, contribution reservations and existing ownership/recipe
boundaries before platform-invoked registration or storage writes. Future
capabilities may remain inert metadata; unsupported executable contributions
fail explicitly. Route reservations include overlap/reserved-prefix checks,
not just identical strings; permissions retain `(owner, localName)`.

Use native DI and public versioned lifecycle contracts. Register only declared
scoped/transient module services after preflight, audit descriptors/build once,
create ordinary owner DI scopes for reverse dependency-safe disposal, initialize
ephemerally, verify/perform migrations, then start in dependency order with
whole-phase barriers. No module-owned `IHostedService` or self-published route.
The pure platform library gains only a reviewed DI-abstractions project edge;
provider recipes and assignment-aware contexts stay infrastructure/module-owned.

Separate explicit stopped **`--modules prepare`** from **ordinary serving**.
Prepare uses one admitted child/exclusive 003 scope, completes mutation-free
whole-set preflight, registers/initializes then provisions/migrates topologically
and cleans up without activation. Serve repeats ephemeral stages in a fresh
attempt, verifies already-current stores/history under the host lease, and
publishes only a fully healthy started set. No provisioning credential enters
serving DI or child environment. Lifecycle callbacks perform ephemeral work,
not irreversible business/external effects or exactly-once durable work.

Configured serving also has an owned supervisor; token cancellation/WaitAsync
cannot prove an uncooperative callback stopped. Shared monotonic work/cleanup
caps and actual owned-process exit evidence govern failures. Root exit alone
cannot prove descendants exited; module-spawned processes are unsupported.
Unverifiable cleanup remains a reported block, not successful termination.
Normal empty bootstrap remains direct and data-preserving.

Keep desired, lifecycle, own health, effective dependency health and freshness
separate. Nonhealthy/stale enabled owners withdraw aggregate readiness.
Anonymous health retains its schema and exposes no inventory; detailed fixed-code
records stay in the trusted operator channel. Validate bounded child event
frames and discard raw child output. Use bounded telemetry dimensions, not
settings/IDs/correlations as labels. These are audit-relevant observations,
not a durable audit service.

The [plan](../../specs/004-module-lifecycle/plan.md),
[research](../../specs/004-module-lifecycle/research.md),
[data model](../../specs/004-module-lifecycle/data-model.md),
[contracts](../../specs/004-module-lifecycle/contracts/composition-v1.md) and
[operator boundaries](../../specs/004-module-lifecycle/contracts/operator-v1.md)
fix finite budgets, semantics and required proof. No constitutional amendment
or inventory scope change is proposed.

## Alternatives

Dynamic loading frameworks/package scanning add discovery/trust/version policy
not needed for deliberate trusted composition. Per-module validate-and-write
breaks the whole-set preflight guarantee. Runtime CREATE credentials/live
migrations weaken accepted 003. Tasks/threads or `WaitAsync` alone cannot enforce
callback termination. One custom container per module, mediator-based dispatch,
automatic dependency installation/cascading disable and a durable lifecycle-job
engine add scope without satisfying an unmet requirement.

Exposing test routes or executing every declared capability invents later
auth/resource/MCP/UI runtimes. Persisted health creates stale success. Reset/
reallocation or global rollback conceals actual owner-local effects.

## Consequences

Compiled deployments require operator rebuild/redeploy, not hot install.
Preparation and serving initialize separate ephemeral instances; authors must
keep migrations/business effects out of Initialize. A finite batch may time
out even when its individual members fit their own caps; budgets never expand
per owner. Read-only infrastructure preflight must distinguish new owners from
broken persisted mappings, including structural recipes before allocation.

Actual both-provider migration/upgrade/three-cycle preservation, independent
public consumers, deterministic repeated corpora, partial-entry cleanup,
uncooperative process termination, privacy captures and retained foundation
gates are required. DI project-edge adoption still requires exact lock/
license/provenance review. Trusted in-process code is not sandboxed.
Forward recovery/explicit full-set restore retains data-loss responsibility;
no auto-down, delete/uninstall or cross-module transaction guarantee.

## Approval

On 2026-10-22, after the explanation of this ADR's compiled deployment/rebuild
tradeoff, whole-set validation, separate stopped preparation and supervised
lifecycle/health, the human explicitly replied **"yeah approved"**. This
approves ADR 0006's design choices for implementation on the existing
[draft PR #7](https://github.com/Stonefly-Labs/Kofte/pull/7).

This decision does not adopt an unreviewed dependency, evaluate or mark the
reviewer-owned quality checklist, accept numbered feature 004 or authorize
merge. Earlier plan/research/contract artifacts and ledger notes retain their
historical Proposed/pending-approval statements; this later explicit decision
is the current authority for ADR status. Those artifacts and evidence hashes
remain unchanged. **Checklist remains the next separate stage** on this same
branch/PR; recording approval does not execute it or begin implementation.
