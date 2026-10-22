# Module Manifest and Validation Contract

**Status:** Implemented under Accepted ADR 0004; spec 002 is not accepted.
See [implementation evidence and remaining gates](../implementation.md).
**Schema:** [module-manifest.schema.json](module-manifest.schema.json)
**Schema ID:** `urn:kofte:module-manifest:1` (identifier only, never fetched).

## Versions and input document

The initial supported combination is schema string **`1`**, published host
contract **`1.0.0`**, and capability-contract **`1.0.0`** for each kind below.
These are not the host product, bootstrap OpenAPI `1.0.0`, assembly, dependency or
module release version. Host version is always supplied explicitly. Comparison
against another syntactically valid host SemVer is a mathematical decision,
not certification of that host's executable API. Unknown schemas/kinds or
unsupported capability versions reject.

JSON only: a single UTF-8 object, no BOM, malformed UTF-8, unpaired surrogate,
comments, trailing comma, extra document or duplicate decoded property name.
Field names are case-sensitive. Required fields:

| Field | Value |
| --- | --- |
| `schemaVersion` | Exact string `1` |
| `id` | Canonical ModuleId, FR-003's lowercase ASCII, 3+ labels, 63 per label/253 total |
| `version` | Full SemVer 2.0.0 module release, at most 128 characters |
| `displayName` | 1-128 Unicode scalars; not whitespace-only; no C0/C1 control or unpaired surrogate; Unicode including Köfte preserved |
| `hostContract` | Köfte range below, at most 512 ASCII characters |
| `capabilities` | Required array, 0-9 objects with required `kind` and `contractVersion` |
| `dependencies` | Required array, 0-64 objects with required `id` and `versionRange` |
| `permissions` | Required array, 0-128 objects with required `name` and `description` |
| `storage` | Required object containing only required boolean `relational` |

All objects are closed: no undeclared fields at any depth. Empty collections and
`storage: {"relational": false}` are valid. There are no null/default substitutes
for required declarations.

Kinds are `backend`, `frontend`, `resources`, `providers`, `events`, `workflows`,
`jobs`, `permissions`, `mcp-tools`. A kind occurs once regardless of its version.
`contractVersion` is a full SemVer whose precedence equals stable `1.0.0`;
`1.0.0+build` is supported, `1.0.0-rc.1`/`1.1.0` are not.
These versioned declarations reserve categories, not executable SDKs for later
resources, providers, jobs, frontend or MCP runtimes.

Dependency IDs use the module grammar, are unique and cannot equal the owning
ID. Their ranges are parsed but no installed-set lookup, cycle/order check or
resolution occurs. Permissions have a 1-64-character local ASCII `name` matching
`[a-z][a-z0-9]*(?:[.-][a-z][a-z0-9]*)*` and a 1-256-scalar non-whitespace-only
`description` without C0/C1 controls/unpaired surrogates. Names are unique;
ownership is `(moduleId,name)`, not a new global permission-string encoding or a
grant. Capabilities and permission lists are independent: declaring metadata
does not require activation or a runnable contribution.

Storage expresses a need only. No caller-selected schema/file/path/connection,
credential, migration or hook field is supported.

## Version and range semantics

Versions use SemVer 2.0.0: three unpadded nonnegative numeric components, optional
dot-separated ASCII alphanumeric/hyphen prerelease/build identifiers. Empty
identifiers reject; numeric prerelease identifiers cannot have leading zeros;
numeric build identifiers may. Compare core components numerically, then
prerelease identifiers per SemVer (numeric below alphanumeric; ordinal ASCII
among nonnumeric identifiers; shorter equal-prefix sequence below longer).
Release sorts after its prereleases; build metadata does not affect precedence.
Length-bounded decimal-text comparison must not impose Int32/Int64 limits.

Supported range grammar, where each `VERSION` is a full bounded SemVer:

```text
range := VERSION | LOWER VERSION SP UPPER VERSION
LOWER := ">" | ">="
UPPER := "<" | "<="
SP := exactly one ASCII space
```

No whitespace between an operator/version; no leading/trailing spaces,
one-sided ranges, `=`, `*`, `^`, `~`, `||`, bracket or hyphen shorthand. This is
**Köfte syntax, not npm or NuGet range compatibility**. Upper must have greater
precedence than lower, except an equal pair with both inclusive is valid.
An equal pair with either exclusive, or reversed pair, is `range_invalid`.

Stable target versions follow ordinary bounds. A prerelease target must also
have the same major/minor/patch tuple as at least one explicitly prerelease
endpoint (an exact version is its endpoint). Otherwise it is ineligible, even
if its precedence fits the interval. Build metadata never changes membership.

| Range | Explicit target host version | Expected |
| --- | --- | --- |
| `>=1.0.0 <2.0.0` | `1.0.0`, `1.5.0`, `1.999.0+build` | Accept |
| `>=1.0.0 <2.0.0` | `0.9.9`, `2.0.0`, `1.1.0-rc.1` | `host_incompatible` |
| `>1.0.0 <=2.0.0` | `1.0.0` / `2.0.0` | Incompatible / accept |
| `>=1.1.0-rc.1 <2.0.0` | `1.1.0-rc.2` / `1.2.0-rc.1` | Accept / incompatible |
| `1.0.0+one` | `1.0.0+two` / `1.0.0-rc.1` | Accept / incompatible |
| `>=1.0.0 <=1.0.0` | `1.0.0` | Accept |
| `>=2.0.0 <1.0.0`, `>=1.0.0 <1.0.0`, `^1.0.0` | Any valid context | `range_invalid` |

The same membership rule will apply to dependency versions when a later resolver
exists; this contract only validates the dependency range itself.

## Public .NET surface and author consumer

Namespace `Kofte.Platform.Modules.Manifest`; production assembly `Kofte.Platform`
targets `net10.0` without ASP.NET Core, EF, database or IO dependencies.

- `ManifestValidator.Validate(ReadOnlySpan<byte> utf8Json, ManifestValidationContext context)`
  returns `ManifestValidationResult`; no environment, clock, network, filesystem,
  logger or plugin loader dependency.
- `ManifestValidationContext` takes required host-contract string and optional
  prior-ID string. Null context/host, empty/invalid version and invalid prior ID
  reject explicitly rather than throw an input exception or assume a default.
- Result variants `Accepted` and `Rejected` are disjoint. Accepted contains only
  a complete `ModuleDescription`; rejected contains only a nonempty read-only
  diagnostic list. Callers cannot construct accepted descriptions or mutate
  their identities/collections in place.
- Public get-only value types/objects expose all fields in the data model;
  collection backing arrays are privately copied/read-only. No internal parser
  type, host implementation type or caller-owned mutable reference is exposed.
- The library emits **no operational output**. Callers receive explicit errors
  and may log only allowlisted codes/paths; no broad catch-and-success fallback.
  Known parsing/input exceptions become safe rejection; unexpected defects must
  fail the caller, not be misclassified as valid or silently swallowed.

The test-owned `tests/contract/Kofte.Manifest.Consumer` console reads bounded raw
UTF-8 bytes from stdin and takes `--host-contract VERSION` plus optional
`--previous-id ID`. Do not accept input filenames or use host environment defaults.
Read at most 65,537 bytes (one sentinel beyond the input budget) before validation.
On acceptance stdout is only `{"status":"accepted"}`; do not echo a descriptor
or input. On rejection stdout is an object with `status:"rejected"` and the same
safe ordered `diagnostics`. Exit codes: 0 accepted; 2 rejected/invalid invocation;
1 operational IO/internal failure. Invocation/operational failures emit only a
fixed safe code, no command argument, raw exception or path. No file is written.

Optional test-proof arguments are `--culture en-US|tr-TR` (fixed verified
culture/decimal confirmation on stderr) and `--repeat N` (1-100, one result per
independent call). Defaults preserve one result and no culture confirmation.
Neither changes context or acceptance rules; invalid/duplicate options reject.

This independent author adapter uses only the public library. A host-facing
contract-test consumer references the host/library and consumes the same public
API, without internals or a second implementation. It proves direction and
observable parity; it does not add a production host validation endpoint.

## Budgets, phases and diagnostics

| Budget | Limit / enforcement |
| --- | --- |
| Original input bytes | 65,536 inclusive; one over rejects before decoding |
| JSON container depth | 8 inclusive, root object depth 1; ninth container rejects while reading |
| Collections | 9 capabilities, 64 dependencies, 128 permissions; one over rejects, never truncates into acceptance |
| Version/range strings | 128 / 512 ASCII characters respectively, including prerelease/build text |
| Other strings | ID 253 total/63 per label; display name 128 scalars; permission name 64 ASCII/description 256 scalars |
| Diagnostics | 32 total maximum; first 31 plus final `diagnostics_truncated` if further errors exist |
| Per-call work | Input-bounded scans, bounded collection sets, no recursive/backtracking regex or external resolution |

Validation phase precedence is:

1. Byte budget and strict encoding, then token parsing/depth/collection budget
   and repeated decoded keys. Fatal failures return one diagnostic; first token
   failure in input byte order wins. Unknown keys never enter a diagnostic path.
2. Root/schema envelope: unsupported schema yields only `schema_unsupported`;
   missing/wrong-type schema yields a shape diagnostic. Do not interpret a
   different schema. Other shape errors collect; no semantic partial success.
3. Semantic fields and context: grammar, supported versions/kinds, duplicates,
   self-reference and optional identity comparison. Missing/invalid context
   rejects, not host incompatibility. Collect only applicable checks (no range
   comparison if range parsing failed).
4. Host compatibility, only after all preceding phases succeed.

For a phase collecting multiple diagnostics, sort by origin (`context` before
`manifest`), then schema field order (`schemaVersion`, `id`, `version`,
`displayName`, `hostContract`, `capabilities`, `dependencies`, `permissions`,
`storage`); context order is host version then prior ID. Within a collection use
numeric item index, declared child-field order, then ordinal code. Missing or
unknown fields are located at the known containing object, never at an unknown
submitted key; otherwise use the known field. Apply truncation after ordering;
append its marker at manifest root. Bound collection/count processing even when
the document is already invalid.

Diagnostic fields: fixed lowercase `code`; `origin` is `manifest` or `context`;
`path` is a JSON Pointer using **only** schema-known names/bounded array indices,
or empty string for root; `message` is fixed corrective prose selected by code.
No raw name/value, manifest snippet, display name, version/range, control
character, path to a local file, stack, parser exception or correlation data is
reflected. Example: `capability_duplicate`, `manifest`, `/capabilities/1/kind`,
"Declare each capability kind once."

| Category | Stable codes and corrective intent |
| --- | --- |
| Parsing/budgets | `input_too_large` (reduce bytes), `encoding_invalid` (use strict UTF-8/scalars without BOM), `document_invalid` (single strict JSON object), `field_duplicate` (declare each key once), `depth_limit` (reduce nesting), `collection_limit` (reduce entries) |
| Shape/schema | `field_required`, `field_type`, `field_unknown`, `schema_unsupported` (supply only the documented required fields/types/schema) |
| Identity | `module_id_invalid`, `identity_changed`, `previous_id_invalid` (canonical ID; keep prior identity unchanged) |
| Version/context | `version_invalid`, `range_invalid`, `host_context_required`, `host_context_invalid` (supply explicit full SemVer/supported range syntax) |
| Declarations | `display_name_invalid`, `capability_unknown`, `capability_unsupported`, `capability_duplicate`, `dependency_duplicate`, `dependency_self`, `permission_invalid`, `permission_duplicate` (correct declared field/kind/version/uniqueness) |
| Compatibility/output | `host_incompatible` (target a compatible host contract), `diagnostics_truncated` (fix reported fields and retry) |

`module_id_invalid` also applies to a dependency's ID at its known location;
`version_invalid` applies to module/capability syntax, whereas malformed dependency
ranges use `range_invalid`. Malformed context version uses `host_context_invalid`;
null/empty/missing uses `host_context_required`. Unknown capability kind is a
semantic `capability_unknown` even though a schema engine rejects its enum.

## Structural versus complete acceptance

The schema verifies property/type/length/enum/shape patterns; it cannot by itself
prove original-byte budgets, decoded duplicate absence, well-formed scalar text,
whitespace-only text exclusion, numeric range endpoint grammar/order, semantic
identity uniqueness, unchanged prior ID, capability version support, or supplied
host compatibility. Schema-valid examples of each semantic rejection belong in
the [shared corpus](acceptance-corpus.md). A schema pass never means installed,
trusted, permitted, resolvable, migrated, enabled, sandboxed or executable.

The library must not fetch schema URIs or user references, load assemblies,
evaluate hooks, resolve DNS, allocate files/schemas, modify host settings or
emit raw logs. Future protected REST/UI/MCP/jobs still owe common actor/team/
resource authorization and audit; a declaration does not bypass them.
