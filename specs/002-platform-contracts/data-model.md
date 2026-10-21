# Data Model: 002 - Transient Module Declarations

**Status:** Proposed design; no table, store, migration or runtime registration.
See [validation contract](contracts/validation.md) for exact types/budgets.

| Entity | Fields and relationship | Invariants |
| --- | --- | --- |
| ModuleId | `Value`: canonical ASCII reverse-DNS text | At least three labels, each 1-63 characters; total at most 253; lowercase letters/digits/interior hyphens, no normalization. Immutable, ordinal identity. |
| ModuleDescription | `SchemaVersion`, `Id`, `Version`, `DisplayName`, `HostContract`, read-only `Capabilities`, `Dependencies`, `Permissions`, `Storage` | Complete accepted interpretation only; constructor internal; immutable object graph with private collection copies. All required JSON fields present. No installation/team/status authority. |
| SemanticVersion | Preserved source value; parsed major/minor/patch digit strings, optional prerelease/build identifiers | Full SemVer 2.0.0; 128-character maximum; no numeric overflow. Build excluded from precedence. No public mutator. |
| VersionRange | Exact version, or lower/upper version and inclusive flags | One documented grammar, nonempty interval and explicit prerelease eligibility. No dependency resolver. Immutable. |
| CapabilityDeclaration | `Kind`, `ContractVersion` | One per supported kind; initial supported version precedence `1.0.0`, no prerelease. No implementation/handler/type name. |
| DependencyDeclaration | `Id`, `VersionRange` | Canonical target identity, unique per target, not self. Absence/cycles/install compatibility deliberately unassessed. |
| PermissionDeclaration | `Name`, `Description`; owner inherited from enclosing module | Local ASCII name unique within module; identity is tuple `(ModuleId,Name)`. Descriptive, not granted or bound to a team/resource. |
| StorageRequirements | `Relational`: boolean | Both true/false valid; never a schema, path, credential, connection or cross-module request. |
| ManifestValidationContext | Required `HostContractVersion` string; optional `PreviousModuleId` string | Supplied explicitly; invalid/missing context is a rejection, no environment/product defaults. Prior identity compared ordinally after validation. |
| ManifestValidationResult | Exactly one of `Accepted(Description)` or `Rejected(Diagnostics)` | Never partial descriptor plus errors. Rejected has 1-32 immutable diagnostics; accepted has no rejection diagnostics. |
| ManifestDiagnostic | `Code`, `Origin`, `Path`, fixed `Message` | Stable safe machine identity `(origin,path,code)`; known paths/bounded indices, no raw untrusted values or exception details. |

All relationships are containment/value references in memory. Parsed IDs may
repeat across independent calls; only dependencies/capabilities/permissions within
one manifest have collection uniqueness rules. No durable or global identity
uniqueness is asserted.

## Validation and state transitions

`supplied bytes + explicit context -> bounded parse -> shape -> semantic checks
-> compatibility -> accepted immutable description OR rejected diagnostics`.

Each stage either returns its rejection or permits the next; no partial
description escapes. Collection/depth/byte limit violations stop immediately.
Unknown schema stops interpretation. A corrected retry starts a new independent
call; no registry/configuration/cache/activation state can survive a failure.

An update comparison carries a prior ID only: same ID with new display name or
module version is allowed, changed ID rejects. It is not an installed upgrade,
version downgrade policy, migration, uniqueness reservation or author-ownership
claim. No parsed value is turned into a physical storage identifier.

## Migration, retention and rollback

No persisted model, EF context/history, database schema version or platform
backup format changes. Retaining a manifest is caller-owned source/configuration
management. Schema `1` artifacts and validator changes are reviewed/versioned
together; unsupported versions reject rather than mutate old input. Runtime
installation, storage mappings, migrations and recovery remain 003/004.
