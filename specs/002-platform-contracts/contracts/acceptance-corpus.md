# Shared Acceptance Corpus

**Status:** Independently expected design fixtures, not executed parser evidence.
Both public .NET consumers must use these same expectations without copying
validation logic. The structural consumer uses the same inputs where meaningful.

Base **M** is [minimal.json](examples/minimal.json), context host `1.0.0`, no prior
ID. Each row starts fresh, changes only the named field/context and retains all
other fields. `S` = structural schema expectation; `V` = complete public
validation. A dash means no meaningful structural result (raw/context-only case).
An expected rejection names its diagnostic code and known pointer; origin is
`manifest` unless explicitly `context`. Accepted results have no diagnostics.

| ID | Input/change | S | V / expected diagnostic identity |
| --- | --- | --- | --- |
| C01 | M unchanged | Pass | Accept |
| C02 | [full.json](examples/full.json), no installed dependencies | Pass | Accept, absent dependency preserved and nine kinds exposed |
| C03 | Omit `storage` | Fail | Reject `field_required`, root |
| C04 | Set `capabilities` to null | Fail | Reject `field_type`, `/capabilities` |
| C05 | Add root field `unexpected` | Fail | Reject `field_unknown`, root |
| C06 | `schemaVersion` = `2` | Fail | Reject only `schema_unsupported`, `/schemaVersion` |
| C07 | [duplicate-key.txt](examples/duplicate-key.txt) original bytes | — | Reject `field_duplicate`, root; escaped key alias cannot replace ID |
| C08 | Truncated M, trailing comma, JSON comment, or second root object (separate raw fixtures) | — | Reject `document_invalid`, root |
| C09 | `id` = `Org.example.notes`, ` org.example.notes`, `org.exämple.notes`, `org..notes`, `org.example.-notes`, `org/example/notes`, or `org.notes` (each separate) | Fail | Reject `module_id_invalid`, `/id`; no normalization |
| C10 | Prior `org.example.notes`; new display `Köfte Notes`, release `0.2.0` | Pass | Accept, ID unchanged |
| C11 | Prior `org.example.other` | — | Reject `identity_changed`, `/id` |
| C12 | Prior `ORG.example.notes` | — | Reject `previous_id_invalid`, context `/previousModuleId` |
| C13 | `version` = `1.02.0`, `1.0`, `1.0.0-01`, or `1.0.0+` (each separate) | Fail | Reject `version_invalid`, `/version` |
| C14 | `version` = `2147483648.0.0+001` | Pass | Accept; no Int32 ceiling or build-leading-zero rejection |
| C15 | `hostContract` = `>=1.0.0 <1.0.0` | Pass | Reject `range_invalid`, `/hostContract` |
| C16 | `hostContract` = `>=1.02.0 <2.0.0` | Pass | Reject `range_invalid`, `/hostContract` |
| C17 | `hostContract` = `^1.0.0` | Fail | Reject `range_invalid`, `/hostContract` |
| C18 | Context `2.0.0` | — | Reject `host_incompatible`, `/hostContract` |
| C19 | Empty/missing host context | — | Reject `host_context_required`, context `/hostContractVersion` |
| C20 | Host context `1.0` | — | Reject `host_context_invalid`, context `/hostContractVersion` |
| C21 | Two `backend` capabilities, contracts `1.0.0` and `1.0.0+different` | Pass | Reject `capability_duplicate`, `/capabilities/1/kind` |
| C22 | One kind `unknown`, version `1.0.0` | Fail | Reject `capability_unknown`, `/capabilities/0/kind` |
| C23 | One `backend` kind, version `1.1.0` or `1.0.0-rc.1` (separate) | Pass | Reject `capability_unsupported`, `/capabilities/0/contractVersion` |
| C24 | Two dependencies on `org.example.absent` with distinct valid ranges | Pass | Reject `dependency_duplicate`, `/dependencies/1/id` |
| C25 | Dependency `org.example.notes`, valid range | Pass | Reject `dependency_self`, `/dependencies/0/id` |
| C26 | Dependency `org.other.notes`, valid range, absent from installation | Pass | Accept; no resolution/ordering/cycle claim |
| C27 | Two permission objects with `name:"notes.read"` and valid descriptions | Pass | Reject `permission_duplicate`, `/permissions/1/name` |
| C28 | Permission name `Notes.Read` | Fail | Reject `permission_invalid`, `/permissions/0/name` |
| C29 | Add `storage.path`, `storage.schema`, root `$ref`, or root `hooks` (each separate) | Fail | Reject `field_unknown` at `/storage` or root; no traversal/fetch/execution |
| C30 | `displayName` = one space | Pass | Reject `display_name_invalid`, `/displayName` |
| C31 | Valid supplementary Unicode scalar in display name | Pass | Accept; scalar count, no UTF-16-unit truncation |
| C32 | Known display field contains escaped C0/C1 control | Fail | Reject `display_name_invalid`, `/displayName`; no raw value in output |
| C33 | Unknown key containing a generated secret marker/newline | Fail | Reject `field_unknown`, root; output must not include submitted key |
| C34 | UTF-8 BOM, malformed UTF-8, or lone escaped surrogate (separate raw fixtures) | — | Reject `encoding_invalid`, root |

Every row of the version/range boundary table in
[validation.md](validation.md#version-and-range-semantics) is also a fixture.
Exercise the full SemVer precedence chain
`alpha < alpha.1 < alpha.beta < beta < beta.2 < beta.11 < rc.1 < release`;
test build-insensitive equality and huge bounded numeric components, not lexical
sorting or numbers guessed from the validator.

## Exact limits and one-over inputs

Generate these inputs from explicit recipes, never from the implementation's
constants or parser decisions:

- Identity total 253: four labels of lengths `63,63,63,61`, each all `a`;
  254: lengths `63,63,63,62`. For label 63/64 use `org.example.` plus that
  many `a` characters. The first of each pair accepts, the second rejects
  `module_id_invalid` at `/id`; all other fields remain M.
- Bytes 65,536: append JSON whitespace to M's original UTF-8 bytes until total
  is exactly 65,536. Accept. One more space rejects `input_too_large` at root,
  before decoding. Multibyte display text must count bytes separately.
- Depth 8/9: under an unknown root `unexpected` field, nest arrays with the
  outer root counted as depth 1. Eight containers reaches shape rejection
  `field_unknown`; ninth rejects `depth_limit` first. Neither is accepted.
  Use this to prove parser precedence, not to imply nested arbitrary data is legal.
- Nine capabilities: C02 is valid. A tenth object rejects `collection_limit`
  at `/capabilities`, even if a later duplicate/unknown diagnostic could exist.
  Dependencies 64/65: unique `org.example.d0` through `d63`/`d64`, valid ranges.
  Permissions 128/129: names `p0` through `p127`/`p128`, valid descriptions.
- Display 128/129 scalars, permission description 256/257, permission name
  64/65 ASCII, version 128/129 ASCII. Choose otherwise-valid values; exact limit
  accepts, one-over rejects the corresponding semantic field code. For version
  length use `1.0.0+` plus nonempty `a` metadata. A range can contain at most
  two bounded 128-character versions plus operators/space; the 512-character
  coarse budget is defensive, not a promise every length has a valid range.
- Create 33 invalid dependency IDs within the collection limit: reject with
  first 31 ordered `module_id_invalid` diagnostics then one
  `diagnostics_truncated` at root. A 32-error input returns all 32 without a
  marker. Use numeric item order, not lexical index order (`2` before `10`).

## Cross-consumer, privacy, side-effect and retry evidence

Run the complete corpus 100 times per public consumer under both explicit
`en-US` and `tr-TR` .NET cultures, with real culture support (not invariant-mode
substitutes). Assert exact decisions/ordered `(origin,path,code)`, not machine
messages or Ajv internal errors. Reorder input object keys to establish the
documented collected-diagnostic field order; retain original bytes for raw tests.

For valid output, attempt to mutate all exposed collections/identity and verify
prior accepted descriptors remain unchanged. Follow every rejection class with
M and verify acceptance without reset. Include fixed simultaneous errors to
prove phase precedence and only applicable diagnostics.

Use unpredictable test-only secret/control-character markers in known values and
unknown keys, capture all caller/library output and require zero occurrences,
zero raw stack/path/manifest echoes and bounded line/diagnostic counts. Public
codes/paths remain useful even when input is malicious. IO/internal consumer
failures must exit nonzero with a fixed code, not successful acceptance.

Run the pure calls with no writable module data and an owned local observer for
attempted URL resolution; verify zero observer requests, assemblies activated,
registry writes, module files/schemas or host-configuration changes. Combine
process/filesystem/network observations with source-level dependency review:
a closed-schema URL rejection alone is not proof that no network effects exist.
There is no real-provider allocation/migration to test here; existing bootstrap
persistence regressions protect the unchanged provider paths.

Measure every one of 100 initialized representative checks (including oversized/
deep failures) against the one-second bound. Record actual environment/revision,
safe per-case durations and counts; do not persist seeded raw inputs/output.
SC-001 now requires automated execution of the guide's example, identity and
compatibility failures and accepted corrected retries, per the human-directed
[ADR amendment](../../../docs/adr/0004-module-manifest-contract.md#human-directed-author-guide-amendment).
No unfamiliar-human exercise or guide completion-time claim is required or
inferred. Final feature acceptance remains a separate human decision.
