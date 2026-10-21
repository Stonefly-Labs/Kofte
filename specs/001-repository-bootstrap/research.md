# Research: 001 - Repository Bootstrap and Developer Experience

**Date**: 2026-10-20

**Status**: Planning decisions, not installed dependencies or runtime evidence

**Inputs**: [Specification](spec.md), constitution v2.0.0 and canonical architecture

**Decision record**: [ADR 0002](../../docs/adr/0002-bootstrap-development-baseline.md)

## Research boundary

The pre-research constitution assessment found no conflict in the approved
platform-only scope. Unknowns were supported versions, orchestration/package
manager, source boundaries, bootstrap storage, diagnostic contracts, telemetry
and verification tooling. The decisions below resolve those design questions;
they do not approve the later human-owned quality checklist.

Microsoft Learn and Context7 MCP tools were not connected in this session.
Public official documentation, release metadata and package registries were used
instead. Version observations use a publication cutoff of **2026-10-20**, not an
unqualified `latest` tag. No packages were installed and no transitive dependency
graph exists yet.

## R1. Supported, reproducible toolchain

**Decision**: Target .NET 10 LTS / `net10.0`, SDK **10.0.402**, runtime
**10.0.13**; Node.js **24.22.0** LTS with bundled npm **11.19.1**. Use Next.js
16.4 / React 19.3, TypeScript 6.0, Tailwind 4, matching stable ESLint/Next
configuration, and npm workspaces. Preserve repository Python 3.11+.

The official .NET 10 release metadata reports the 2026-10-13 release,
`runtime.version=10.0.13`, and SDKs `10.0.402` and `10.0.113`. The SDK and
runtime versions are **not interchangeable**: `global.json` must pin `10.0.402`,
not `10.0.13`. The Node distribution index reports
`v24.22.0`, date `2026-10-14`, npm `11.19.1`, LTS `Krypton`.
Node 24 remains supported LTS; Node 26 has not yet entered LTS at this cutoff.

**Rationale**: A supported LTS foundation with native bundled workspace tooling
minimizes setup. TypeScript 6.0 retains the compiler API compatibility needed by
the researched `typescript-eslint` peer range (`>=4.8.4 <6.1.0`); do not select
TypeScript 7 merely because it has the newer registry tag.

**Pinning implementation**: Record exact compatible patch versions with no
floating direct ranges; use `.nvmrc`, root `engines`/`packageManager`,
`global.json` with SDK roll-forward disabled, central NuGet versions and
`packages.lock.json` files, and one root `package-lock.json`. Restore with
`dotnet restore --locked-mode` and `npm ci`. Verify the complete Next/React,
TypeScript/ESLint, Vite/Vitest and Node peer graph before committing it.
Development and CI must reject a mismatched baseline, not silently rewrite locks.
If a researched patch has been withdrawn or is vulnerable at implementation,
record a supported replacement and update this decision, pins and evidence
together; never substitute a floating `latest`.

**Alternatives**: .NET 8/9 approach end of support in November 2026; .NET 11 is
prerelease at the cutoff. Node 22 is supported but offers less remaining runway.
pnpm and Aspire add tooling without a demonstrated bootstrap need.

**Sources**:
[.NET support](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core),
[.NET 10 release metadata](https://builds.dotnet.microsoft.com/dotnet/release-metadata/10.0/releases.json),
[Node distribution metadata](https://nodejs.org/dist/index.json),
[Node release schedule](https://github.com/nodejs/Release#release-schedule),
[Next installation](https://nextjs.org/docs/app/getting-started/installation),
[React versions](https://react.dev/versions),
[TypeScript registry](https://registry.npmjs.org/typescript),
[typescript-eslint registry](https://registry.npmjs.org/typescript-eslint),
[npm workspaces](https://docs.npmjs.com/cli/v11/using-npm/workspaces).

## R2. Native local processes; containers only for PostgreSQL verification

**Decision**: A small standard-library Python supervisor starts the two native
processes, defaults to SQLite, checks prerequisites/ports/configuration and waits
for real HTTP readiness. Docker Engine with Compose is needed only for the
disposable PostgreSQL test profile. Select PostgreSQL **17**, still supported
through November 2029, with an exact maintained image tag and digest recorded
at implementation.

**Rationale**: A contributor needs only existing Python plus .NET/Node for a
warm local run. The supervisor can propagate failed child exits, bound startup,
handle interrupts and terminate only its own children. No app container builds,
Kubernetes, external collector or cloud account is needed.

**Alternatives**: Compose for the whole stack would make a container daemon a
SQLite prerequisite; Aspire would introduce a third application host and extra
workload knowledge. Shell background jobs do not provide portable ownership,
bounded readiness and reliable failure propagation. PostgreSQL 18 is supported,
but this feature does not need a newer-major-specific capability.

**Sources**:
[Compose startup/readiness](https://docs.docker.com/compose/how-tos/startup-order/),
[PostgreSQL version policy](https://www.postgresql.org/support/versioning/),
[official PostgreSQL image](https://hub.docker.com/_/postgres).

## R3. No application schema before the storage-registry feature

**Decision**: Use thin `Microsoft.Data.Sqlite` and `Npgsql` adapters for host-owned
connectivity/readiness. The platform file is `data/platform/platform.db`; the
PostgreSQL platform namespace is `kofte_system`. Ship **no application tables,
DbContext, migration history or module registry in 001**. Tests create private
fixture tables inside their dedicated platform file/schema, never through HTTP.

**Rationale**: Real providers, persistence, backup/restore and reconnection can be
proved without inventing a domain model. Module EF Core contexts and migrations
remain required in 003 and later modules; direct bootstrap probes neither replace
those contracts nor establish a cross-module repository abstraction.

**Alternatives**: EF Core plus empty contexts/migrations adds no useful persistent
model here. `EnsureCreated`, shared module tables, SQLite `ATTACH`, automatic
database replacement and in-memory PostgreSQL substitutes would provide the wrong
evidence or weaken ownership/recovery.

Use SQLite foreign keys and a bounded busy timeout; retain its default rollback
journal for this single-process foundation. Do not enable WAL without a need and
matching backup tests. Explicitly initialize and verify the platform store;
ordinary restart must not recreate a missing/corrupt existing store unnoticed.
PostgreSQL credentials stay in process environment, not command arguments/logs.

**Sources**:
[Microsoft.Data.Sqlite](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/),
[SQLite backup](https://www.sqlite.org/backup.html),
[SQLite locking](https://www.sqlite.org/lockingv3.html),
[Npgsql basic usage](https://www.npgsql.org/doc/basic-usage.html),
[PostgreSQL schemas](https://www.postgresql.org/docs/17/ddl-schemas.html),
[PostgreSQL backup](https://www.postgresql.org/docs/17/backup.html).

## R4. Small explicit HTTP and UI contracts

**Decision**: Use ASP.NET Core health checks with a custom safe response writer
and built-in OpenAPI generation. Expose only `GET /health/live`,
`GET /health/ready`, and development-only `GET /openapi/v1.json` on the host.
Use a same-origin Next.js **GET route handler** for the browser's fixed
`/backend/health/ready` path. Its only operation is fetching the existing host
readiness contract. Restrict its destination to the configured loopback host;
never accept a caller-supplied destination or forward caller credentials.

**Rationale**: The UI and HTTP callers observe exactly one readiness operation.
The narrow adapter avoids permissive CORS and browser-exposed backend configuration.
Three routes can be contract-tested against a versioned OpenAPI baseline without
Swagger UI or a new business endpoint. A narrow `packages/bootstrap-contract`
workspace shares the versioned schema/generated TypeScript types between the
web app and HTTP contract tests; it is not a module SDK.

**Alternatives**: An always-green static page conceals failure; client-only fake
status is not evidence. A general-purpose proxy, credential-forwarding backend,
unrestricted CORS or browser-selected URL expands the bootstrap trust boundary.
A transparent rewrite would forward request headers by default; the GET adapter
instead explicitly controls the fixed upstream request and safe error response.
TanStack Query/form libraries have no useful role for one read-only status poll.

**Sources**:
[ASP.NET Core health checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-10.0),
[ASP.NET Core OpenAPI](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/openapi/overview?view=aspnetcore-10.0),
[Next route handlers](https://nextjs.org/docs/app/getting-started/route-handlers),
[OpenAPI 3.1.1](https://spec.openapis.org/oas/v3.1.1.html).

## R5. Accessible small UI; vendor-neutral diagnostics

**Decision**: Use the mandated Tailwind/shadcn/Radix/Lucide baseline only for the
bootstrap page and retry control. No full navigation, feature SDK or remote
bundles. Announce status accessibly, keep focus stable, use non-color text, abort
stale requests, enforce a 10-second attempt deadline and 30-second maximum
freshness interval. A deadline invalidates the displayed healthy state.

Use .NET structured JSON logs and OpenTelemetry ASP.NET Core/HTTP tracing and
metrics, with OTLP export opt-in. A local collector is never required. Emit
allowlisted event fields/error codes, not raw exceptions or configuration.
Trace correlation is server-generated or derived from validated W3C context;
arbitrary incoming headers are never reflected as log fields.

**Alternatives**: A full observability stack or application audit database belongs
to later specs. Raw exception serialization and automated configuration dumps
are not acceptable diagnostic shortcuts.

**Sources**:
[shadcn installation](https://ui.shadcn.com/docs/installation/next),
[Radix accessibility](https://www.radix-ui.com/primitives/docs/overview/accessibility),
[WCAG 2.2](https://www.w3.org/TR/WCAG22/),
[OpenTelemetry .NET](https://opentelemetry.io/docs/languages/dotnet/).

## R6. Evidence and adoption/license gates

**Decision**: xUnit for .NET; Vitest/Testing Library for status logic;
Playwright Chromium and axe for browser/accessibility evidence; exact-version
OpenAPI type generation and runtime schema validation in contract tests. A
root verification runner executes named suites and fails on unavailable mandatory
providers/browsers. CI runs the same suites, not lighter substitutes.

**License/provenance boundary**: Expected direct license families include
MIT (.NET/Next/React/Tailwind/Radix/Vitest), Apache-2.0
(TypeScript/OpenTelemetry/Playwright/xUnit), ISC (Lucide),
PostgreSQL-style permissive (PostgreSQL/Npgsql) and SQLite public domain.
axe's MPL-2.0 requires explicit review of file-level/source/notice obligations,
including bundled code, before adoption; test-only use is not a blanket exemption.
Copied shadcn source and icon notices also need retention. Metadata is a starting
point, not approval of a future lock graph.

At implementation, record direct and transitive package versions, source,
integrity/provenance, SPDX/license evidence, notices and vulnerability findings in
`docs/development/dependencies.md`; fail adoption for unknown or incompatible
obligations. Pin GitHub Actions by commit SHA and the PostgreSQL image by digest.
Docker Desktop is proprietary and subject to entitlement rules; a compatible
Engine/Compose installation is sufficient and Desktop is not mandatory.

**Alternatives**: Mocked databases and a skipped browser job cannot prove the
specification. A package-name allowlist or vulnerability scan alone does not
constitute a license/provenance review.

**Sources**:
[xUnit](https://xunit.net/),
[Vitest](https://vitest.dev/guide/),
[Playwright](https://playwright.dev/docs/intro),
[axe license](https://github.com/dequelabs/axe-core/blob/develop/LICENSE),
[SQLite public domain](https://www.sqlite.org/copyright.html),
[PostgreSQL license](https://www.postgresql.org/about/licence/),
[Docker Desktop terms](https://www.docker.com/legal/docker-subscription-service-agreement/).

## Research outcome

No unresolved material design question or constitutional conflict remains.
Exact secondary package patches, image digest and the actual transitive license
inventory must be resolved by implementation before adoption; they are not
evidence that dependencies were restored, licensed or tested during planning.
Full module migrations, authentication, MCP, production packaging and release
proof remain at their specified later inventory gates.
