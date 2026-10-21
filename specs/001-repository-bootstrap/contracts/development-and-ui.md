# Development and UI Contract: Bootstrap v1

**Status**: Implemented design baseline; final verification/reconciliation in progress. See
[current evidence](../implementation.md) and the
[local guide](../../../docs/development/local-development.md) for working versus outstanding commands.

**Traceability**: FR-002-006/008-011/014-017; US1, US3, US4

## Configuration and process ownership

Configuration uses environment variables, with tracked `.env.example` values
that are safe examples only. The supervisor may load a local ignored `.env`
without shell evaluation; explicit process environment wins. Invalid/unknown
Köfte configuration keys and unsupported provider/profile combinations fail,
rather than being silently ignored or replaced.

| Setting | Default / validation |
| --- | --- |
| `KOFTE_PROFILE` | `local-sqlite`; alternative `test-postgres` only with explicit disposable-test designation |
| `KOFTE_HOST_URL` | `http://127.0.0.1:5080`; absolute HTTP URL, no userinfo/path/query/fragment; loopback unless explicit unsafe exposure opt-in |
| `KOFTE_WEB_URL` | `http://127.0.0.1:3000`; same address validation; distinct unoccupied port |
| `KOFTE_ALLOW_UNSAFE_EXPOSURE` | `false`; broader bindings require `true` and a prominent local-only warning; not production support |
| `KOFTE_DATA_ROOT` | Absolute canonical checkout `data/`; SQLite suffix fixed to `platform/platform.db`; reject escapes/reparse links; owner-restricted directories |
| `KOFTE_POSTGRES_CONNECTION` | No default; secret environment input required for `test-postgres`, no command-line secret or log echo |
| `KOFTE_TEST_POSTGRES_ALLOW_DISPOSABLE` | `false`; test harness additionally requires loopback and proof that it created each database before cleanup |
| `KOFTE_BACKEND_ORIGIN` | Server-only adapter destination `http://127.0.0.1:5080`; loopback HTTP only, no userinfo/path/query/fragment; follows validated host address/port |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Unset: no export required; explicit trusted operator endpoint only; never browser/request-controlled |
| `ASPNETCORE_ENVIRONMENT` | Supervisor sets `Development` locally; unset/non-Development omits OpenAPI; never enables business authorization |

Host and browser URLs are non-secret settings but must not accept embedded
credentials. Never expose a PostgreSQL connection through `NEXT_PUBLIC_*`.
The dedicated PostgreSQL test role/schema/database are supplied by the harness,
not inferred from an arbitrary environment connection. Tests use separate
data roots and ports instead of altering the contributor's defaults.

`python3 scripts/dev.py` starts/builds the host/web after explicit restore and
initialization, remains attached, and signals ready only after actual probes.
Preflight/child/deadline failures exit nonzero. Ctrl-C stops only owned children.
It does not install missing tools, pull images, take over ports or reset data.
An unready startup ends within 300 seconds with a safe component/error code and
documented corrective step, never a misleading success.

The host's local `--store init|backup|restore|reset` CLI is separate from HTTP.
Initialization is explicit and non-overwriting. Backup/restore/reset refuse an
active host using an exclusive lock. Restore requires a validated backup and an
empty destination, or separate destructive replacement confirmation after backup.
Reset requires `--confirm-delete-platform-data`; it never runs on ordinary start,
retry or stop. PostgreSQL cleanup is test-run-owned, not a general reset command.
No CLI option grants an HTTP user a privilege or creates a domain record.

## Host HTTP

[bootstrap.openapi.json](bootstrap.openapi.json) is the planned OpenAPI 3.1.1
baseline, version 1.0.0; health payloads carry `version: "1"`. Its paths are
exact, case-sensitive contracts. Health 200/503 responses are JSON with no unknown
fields; invalid health input receives empty 400. All use `Cache-Control: no-store`.
A trace ID is a 32-character lowercase
hex server-generated/validated W3C correlation value, not arbitrary echoed input.

| Route | Outcome / access |
| --- | --- |
| `GET /health/live` | 200, `state: live`, `component: host`; independent of database, anonymous non-sensitive response |
| `GET /health/ready` | 200 `ready/storage` only after selected-store probe; 503 `unavailable/host` during incomplete startup or `unavailable/storage` on dependency failure |
| `GET /openapi/v1.json` | 200 current generated description only in Development; otherwise absent/404 |

Readiness's total server budget is 5 seconds and provider budget is 3 seconds;
timeouts cancel work and map to 503 when the server can respond. A disconnected
client aborts work. No stale ready flag, implicit provider switch, detailed
dependency name/address/version, filesystem path, raw exception or secret.

Contract tests compare normalized runtime OpenAPI paths, methods, required
headers, anonymous access, version and response schemas against the baseline;
descriptions/order may differ, behavior may not. Generate web types from that
same baseline and reject drift. Extra anonymous routes, fake auth, test mutation
routes or MCP endpoints are not permitted. Requests with unsupported methods
or unknown paths return 404/405 with no diagnostic body/secret echo. Health
operations accept no request body or domain parameters; reject body-bearing or
query-bearing requests with a sanitized empty 400. Framework protocol errors
must also avoid reflecting untrusted values or development exception pages.
Negative contract tests include wrong methods, unexpected query/body, malformed
headers, unknown routes, credential canaries and non-Development docs exposure.

## Web state and transport

`/` renders Köfte, bootstrap-only scope and an honest no-features/modules state.
No fabricated service/team/user, login, privileged action or working tool appears.
Web public assets/framework internals are not additional host business endpoints.

The browser fetches only `/backend/health/ready` with `cache: no-store`.
A fixed Next GET route handler fetches **only** the validated loopback readiness
route; it is not a wildcard proxy or OpenAPI forwarder. It rejects body/query
input with empty 400 and unsupported methods with empty 405; it never forwards
caller cookies, authorization or arbitrary headers. It has no independent
readiness logic: validated 200/503 host payloads retain their status/body and
no-store header. Bound its upstream fetch/body processing to 5 seconds and
propagate client cancellation. Transport failure, malformed payload, non-200/503
status or wrong content type yields a minimal version-1
`unavailable/host` 503 with a new safe correlation ID, never raw upstream text.
That is an explicit failure, not a success-shaped fallback. Contract tests cover
the adapter as well as direct host responses.

| State | Visible behavior / transition |
| --- | --- |
| Loading | Text indicating readiness is being checked; bounded attempt begins on initial hydration, periodic refresh or retry |
| Ready | Text stating host is ready, not platform/MVP/security-complete; only after validating a current 200 payload |
| Unavailable | Safe actionable guidance to check the local host/database and retry; optional validated correlation ID, no raw error |
| Retry | Keyboard-operable control starts a fresh attempt; preserves visible focus, prevents duplicate work and cancels the superseded attempt |

An attempt's absolute deadline is 10 seconds including transfer/body parsing.
Schedule refresh starts no more than 30 seconds apart; do not add the previous
request duration to the 30-second interval. Aborting/unmounting clears timers
and prevents stale responses overwriting newer attempts. Once a current attempt
fails/times out, prior healthy state cannot persist. Refocus/visibility return
immediately revalidates stale status; while suspended/background-throttled, never
represent an old result as a fresh readiness observation. Test bounded behavior
in a visible active page, with explicit stale handling after background suspension.

Use semantic landmarks/headings, actual text alongside icons/color, a polite
live status region and an accessible retry name. Avoid repeatedly announcing
unchanged periodic results; announce meaningful state changes. Preserve keyboard
focus across updates; include focus-visible styling, contrast, zoom/reflow and
reduced-motion considerations. No horizontal overflow at 320/768/1440 CSS px.
Use the minimal mandated UI baseline, not a full portal/navigation system.

## Verification result contract

`python3 scripts/verify.py --suite all --disposable-postgres --warm-starts 10`
is the complete automated gate. Each named
suite reports its exact child commands, start/end/duration, revision, sanitized
environment/tool versions and pass/fail. Mandatory missing infrastructure is
failure; a targeted invocation reports only what it ran, never implies all passed.
Machine-readable reports and captured safe logs live under ignored `artifacts/`.
No test control/fault-injection route may be shipped to achieve this evidence.
