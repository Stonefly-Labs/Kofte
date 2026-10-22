# Quickstart and Validation: 002

**Status:** Executable author guide for the implemented, unaccepted contract.
Focused evidence is in [implementation.md](implementation.md). The human removed
the manual usability gate; implementation is complete, not feature acceptance. The
[local guide](../../docs/development/local-development.md) covers the unchanged host.
The subsequent [convergence assessment](convergence.md) qualifies that original
checkpoint: T049 must complete documented public/repeated corpus coverage before
the feature can pass convergence. Existing passing selections omit that evidence;
no product defect is demonstrated.

## Prerequisites

Use the accepted SDK 10.0.402/runtime 10.0.13, Node 24.22.0/npm 11.19.1 and Python
3.11+ on macOS 15+ arm64 or Ubuntu 24.04 x64. The project locks and unchanged
dependency graph are [reviewed](../../docs/development/dependencies.md).
Existing Ajv/xUnit are reused; no version/range or production schema package is added.

From the repository root:

```bash
dotnet restore Kofte.slnx --locked-mode
npm ci
dotnet build Kofte.slnx --no-restore -c Release -warnaserror
```

After explicit restore, manifest validation is offline and needs no running host,
SQLite file, PostgreSQL, Docker, cloud account or Kubernetes cluster.

## Validate, correct and retry as an author

The test-owned author console references only the public `Kofte.Platform` API.
It reads stdin, not a file path supplied to the library. Inspect the
[minimal manifest](contracts/examples/minimal.json),
[full declaration example](contracts/examples/full.json) and
[contract](contracts/validation.md) without changing the host.

```bash
dotnet run --project tests/contract/Kofte.Manifest.Consumer --no-build -c Release -- --host-contract 1.0.0 < specs/002-platform-contracts/contracts/examples/minimal.json
```

Expected exit 0, stdout only `{"status":"accepted"}`; no storage allocated.
To see compatibility failure without changing input:

```bash
dotnet run --project tests/contract/Kofte.Manifest.Consumer --no-build -c Release -- --host-contract 2.0.0 < specs/002-platform-contracts/contracts/examples/minimal.json
```

Expected exit 2, `status:"rejected"`, `host_incompatible` at `/hostContract`;
no version/input echo. Retry the first command: acceptance needs no reset.
An explicit update comparison:

```bash
dotnet run --project tests/contract/Kofte.Manifest.Consumer --no-build -c Release -- --host-contract 1.0.0 --previous-id org.example.other < specs/002-platform-contracts/contracts/examples/minimal.json
```

Expected exit 2 and `identity_changed` at `/id`, never implicit rename.
For an identity error without writing a file, pipe an edited declaration:

```bash
python3 -c 'import json,sys; m=json.load(sys.stdin); m["id"]="Org.example.notes"; print(json.dumps(m))' < specs/002-platform-contracts/contracts/examples/minimal.json | dotnet run --project tests/contract/Kofte.Manifest.Consumer --no-build -c Release -- --host-contract 1.0.0
```

Expected exit 2, `module_id_invalid` at `/id`. Correct the author's value to
`org.example.notes`, or rerun the unchanged minimal-example command: exit 0.
The validator never normalizes IDs. Never reset bootstrap data for validation.
Invalid invocation exits 2; IO/internal failures exit 1 with fixed safe output.

The test-owned console also accepts `--culture en-US` or `--culture tr-TR`
and `--repeat N` (1-100) for proof fixtures. The former writes a fixed culture
confirmation to stderr; the latter emits one independent decision per repetition.
Without these switches, output remains the single documented result. Neither
option supplies host context, fetches input or activates modules.

## Focused automated proof

These selectors are required named verifier/CI steps, not optional
`--if-present` additions. Minimum expected tests prevents empty success.
All .NET test source stays inside its dedicated project under `tests/`.

```bash
dotnet format Kofte.slnx --verify-no-changes --no-restore
dotnet test --project tests/unit/Kofte.Platform.Tests --no-restore -c Release --minimum-expected-tests 1
dotnet test --project tests/contract/Kofte.Platform.ContractTests --no-restore -c Release --minimum-expected-tests 1
npm run test --workspace tests/contract -- module-manifest.test.ts
```

The unit command covers grammar, immutability, precedence, shape, collection
uniqueness, budgets and safe diagnostics. The contract project runs the shared
[corpus](contracts/acceptance-corpus.md) through the independent author process
and host-facing public consumer, 100 times each in `en-US` and `tr-TR`, with
explicit comparison context. It also measures bounded calls, observes zero
external effects, captures safe output and proves corrected retry. The
TypeScript command checks structural schema decisions with existing Ajv offline.
It must not redefine SemVer/range validation or treat schema pass as acceptance.

Maintain the existing schema/contract corpus as documentation or explicitly
copied test data with parity assertions; do not let a second unreviewed corpus
diverge. Duplicate-key/encoding fixtures must stay original bytes through the
public validator, not be parsed/reserialized by a harness.

## Foundation preservation and complete implementation gate

Select existing pinned tools and restore browser/image prerequisites per the
local guide. `scripts/verify.py` includes the new unit/contract commands and
required-target inventory, retaining owned fixtures and prior steps. Existing real-provider regressions
must not silently become mocks or get skipped because the new library is pure.

```bash
python3 scripts/check_repository.py
python3 -m unittest discover -s scripts/tests -v
git diff --check
python3 scripts/verify.py --suite quality
python3 scripts/verify.py --suite unit
python3 scripts/verify.py --suite contract --provider both --disposable-postgres
python3 scripts/verify.py --suite integration --provider both --disposable-postgres
python3 scripts/verify.py --suite browser
python3 scripts/verify.py --suite startup --warm-starts 10
```

Focused checks are not the complete implementation gate. Run the existing
`python3 scripts/verify.py --suite all --disposable-postgres --warm-starts 10`
before final implementation handoff, with actual current-head Ubuntu CI
inspection. Do not weaken the wrapper's required script/file inventory or
failure-propagation checks. Record exact commands, exit results, assessed revision
and safe reports in `implementation.md`; unavailable infrastructure
is an explicit blocker, not a pass.

## Available-machine timing and automated guide evidence

Use any available supported developer workstation or CI runner for SC-004.
There is **no fixed OS/CPU/RAM benchmark profile or dedicated-machine requirement**,
following the [human-directed ADR amendment](../../docs/adr/0004-module-manifest-contract.md#human-directed-benchmark-amendment).
Record source revision, exact SDK/runtime, actual OS/architecture/CPU/memory
and per-case results; do not claim equivalent performance across machines. Initialize/JIT
before timing pure calls (not process startup); every one of 100 consecutive
representative checks must finish below one second, including byte/depth-limit
rejections. Retain safe numeric per-case durations, counts, locale and environment
only; a p95/aggregate pass does not prove the individual-call bound.

For SC-001, execute the documented example and identity/compatibility failure
commands above, check the stated safe diagnostics/exit codes, then verify that
corrected input/context is accepted without host edits or external services.
Automated execution and the public-consumer/retry tests supply this evidence.
The human explicitly removed the unfamiliar-author exercise, ten-minute target
and elapsed-time attestation in the
[author-guide amendment](../../docs/adr/0004-module-manifest-contract.md#human-directed-author-guide-amendment).
Do not claim that automation proves human usability or completion time.
Convergence, final review and explicit numbered-feature acceptance remain
separate later steps; this amendment does not accept the feature.

## Failure and rollback boundaries

Supported initial combination: schema `1`, published host contract `1.0.0`,
capability contracts stable `1.0.0` (build metadata is ignored). Other explicit
valid host versions can be compared mathematically, not certified as executable
host APIs. The module release is a separate SemVer domain.

Ranges accept only one full SemVer or a lower (`>`/`>=`) and upper (`<`/`<=`)
pair separated by exactly one ASCII space, such as `>=1.0.0 <2.0.0`.
No one-sided, wildcard, caret, tilde, shorthand or extra-whitespace forms exist.
Empty/reversed intervals reject. Build metadata does not affect membership;
prerelease targets require a same-core prerelease endpoint. See the
[complete boundary table](contracts/validation.md#version-and-range-semantics).
Permission ownership is `(moduleId,localName)`, not a grant; relational storage
is a need, not a path/schema allocation.

Correct source input/context and resubmit; do not reinstall, create a registry,
disable permissions or clear any store. Unknown schema/capability versions need
a reviewed contract update, not silent fallback. Accepted declaration means
neither publisher trust nor installation, permissions, data isolation, dependency
resolution or executable compatibility. No new HTTP/UI/MCP surface or migration
exists to roll back in 002; source/library/schema changes use ordinary reviewed
version control.

Breaking grammar, fields, kinds, budgets or diagnostic identities require a
reviewed schema/host-contract compatibility decision and ADR update. Revert
source/library/schema through normal review, retaining original authored input;
there is no installed-data migration or automatic reinterpretation in this feature.
