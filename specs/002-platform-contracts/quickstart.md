# Quickstart and Validation: 002

**Status:** Future implementation validation guide. New project commands below
do not run yet; planning added no product source, restored no dependencies and
does not claim these tests passed. For today's bootstrap use the
[local guide](../../docs/development/local-development.md).

## Prerequisites

Use the accepted SDK 10.0.402/runtime 10.0.13, Node 24.22.0/npm 11.19.1 and Python
3.11+ on macOS 15+ arm64 or Ubuntu 24.04 x64. Implementation must add/review the
new project locks and dependency inventory before locked restore. Existing Ajv/
xUnit are reused; do not install a new version/range or production schema package.

From the repository root, once those implementation changes exist:

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
Privately copy the minimal example, change its ID to `Org.example.notes` and
pipe that copy to the first command: expect `module_id_invalid`. Correct it to
lowercase canonical text and retry. The validator itself never normalizes IDs.
Remove only that explicit temporary copy when done; never reset bootstrap data.
Invalid invocation exits 2; IO/internal failures exit 1 with fixed safe output.

## Focused automated proof

These **planned selectors** must become required named verifier/CI steps, not
optional `--if-present` additions. Minimum expected tests prevents empty success.
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
local guide. Wire new unit/contract commands into `scripts/verify.py`; retain
owned fixtures and all prior required steps. Existing real-provider regressions
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
and safe reports in the later `implementation.md`; unavailable infrastructure
is an explicit blocker, not a pass.

## Reference timing and human evidence

Use Ubuntu 24.04 x64, 2 vCPU and 8 GiB RAM as the SC-004 reference environment,
with exact SDK/runtime and actual recorded resource measurements. Initialize/JIT
before timing pure calls (not process startup); every one of 100 consecutive
representative checks must finish below one second, including byte/depth-limit
rejections. Retain safe numeric per-case durations, counts, locale and environment
only; a p95/aggregate pass or another machine does not prove the reference bound.

For SC-001 an unfamiliar author follows only this guide and the linked contract,
validates M and corrects one identity and one compatibility error within ten
minutes after environment readiness. Record explicit reviewer attestation and
timing, not a planner's self-review or invented human observation. This human
exercise is not feature acceptance; the later passing review still requires an
explicit numbered-feature decision.

## Failure and rollback boundaries

Correct source input/context and resubmit; do not reinstall, create a registry,
disable permissions or clear any store. Unknown schema/capability versions need
a reviewed contract update, not silent fallback. Accepted declaration means
neither publisher trust nor installation, permissions, data isolation, dependency
resolution or executable compatibility. No new HTTP/UI/MCP surface or migration
exists to roll back in 002; source/library/schema changes use ordinary reviewed
version control.
