# 0002: Bootstrap development and verification baseline

**Status:** Accepted

**Date:** 2026-10-20

**Spec:** [001 - Repository bootstrap](../../specs/001-repository-bootstrap/spec.md)

## Context

Spec 001 needs a working local host/web skeleton and real-provider evidence, not
the module registry, identity system or production packaging. Canonical proposals
leave supported versions, orchestration, package boundaries and bootstrap schema
open. The approved local profile must not depend on containers or hosted accounts.

## Decision

Plan .NET 10 LTS (SDK 10.0.402, runtime 10.0.13), Node 24.22.0 LTS / npm 11.19.1,
Next.js 16.4 / React 19.3, and TypeScript 6.0 for compatible lint tooling.
Exact compatible package patches/lockfiles and provenance/license approval are
mandatory at adoption, not claimed by this proposal. Use npm workspaces.

A Python standard-library supervisor owns native host and web child processes,
checks bounded readiness and preserves data on stop. Compose supplies PostgreSQL
17 only for explicitly disposable tests; Docker Desktop is not required.

Keep one host project with feature-local bootstrap handlers and native DI.
Share only the bootstrap HTTP schema/types between web and contract tests.
Use thin SQLite/Npgsql probes; ship no application tables or EF migrations yet.
Tests alone own fixture tables in the platform file or `kofte_system` schema.
This does not waive module-local EF contexts/migrations in later specs.

Expose minimal public liveness/readiness and development-only OpenAPI.
Keep default bindings loopback, the browser readiness adapter fixed and
loopback-only, and diagnostics allowlisted/correlated. No fake identity, module
SDK, domain endpoint or MCP tool. OpenTelemetry has no compulsory exporter.

No constitutional amendment is required. See the
[research](../../specs/001-repository-bootstrap/research.md) for sources and
[plan](../../specs/001-repository-bootstrap/plan.md) for gates/validation.

## Alternatives

Aspire and full-stack Compose add prerequisites without bootstrap value.
An empty EF model/migration or fake module adds speculative persistent contracts.
pnpm adds a separate tool where bundled npm is sufficient. Direct cross-origin
browser requests require extra CORS configuration; a general proxy adds an
unnecessary egress surface. A transparent rewrite forwards request headers;
the fixed GET adapter makes no credential-forwarding assumption.

## Consequences

Native process supervision and safe canonical-path initialization need focused
tests on supported Linux/macOS environments. PostgreSQL/browser checks require
their real dependencies and fail if unavailable. Timing and accessibility
targets require measured implementation evidence. Windows-native support is not
claimed by 001; Ubuntu under WSL can be evaluated separately.

There are no application schema upgrades/downgrades in this stage. Stop-before-copy
SQLite backups and PostgreSQL dump/restore must be proved with isolated fixtures.
No recovery may replace corrupt state or delete existing developer data.
Future schema-bearing specs must introduce explicit versioned migrations rather
than treating connectivity probes as migration infrastructure.

## Approval

Human decision on 2026-10-20: **"bro I approve adr 002"**.
This explicitly approves the ADR's design choices for implementation on
[draft PR #4](https://github.com/Stonefly-Labs/Kofte/pull/4), including native
supervision, no shipped application tables yet and macOS/Ubuntu targets without
native Windows support.

This resolves T001. It is not approval of an unresolved dependency graph,
runtime/manual verification, the numbered feature or the full MVP. Those gates
remain mandatory; the feature PR remains draft.
