# Research: 002 - Platform Contracts and Module Identity

**Date:** 2026-10-21
**Status:** Planning research, not implementation/adoption evidence.

**Implementation amendment:** The human subsequently removed the fixed
Ubuntu/CPU/RAM benchmark environment; see
[ADR 0004](../../docs/adr/0004-module-manifest-contract.md#human-directed-benchmark-amendment).
SC-004 now accepts an available supported workstation/CI runner with actual
metadata and unchanged 100 individual one-second checks. No new dependency or
runtime design follows from this hardware-policy change.

**Subsequent author-guide amendment:** The human declined the timed manual
exercise. SC-001 now requires actual automated guide/correction/retry execution,
not unfamiliar-author attestation; see
[ADR 0004](../../docs/adr/0004-module-manifest-contract.md#human-directed-author-guide-amendment).
This does not provide human-usability evidence or accept the feature.

Read the accepted bootstrap source, solution/project settings, verification
wrapper, local guide, canonical product sources and constitution v2.0.1 before
choosing the following designs. The pre-research constitutional gate found no
conflict. One bounded research-agent pass verified public standards and official
.NET documentation. Microsoft Learn/Context7 MCP tools are not connected in this
session; official web documentation was the fallback, not fabricated MCP output.

## R1 - Strict JSON and a structural schema

**Decision:** JSON only, UTF-8 without BOM; JSON Schema Draft 2020-12 with closed
objects, required explicit declarations and local `$ref` definitions. Use
`urn:kofte:module-manifest:1` as the identifier, not a retrievable module URL.
No YAML, `$ref` manifest property, parser directives, comments or trailing commas.
Schema is author/tooling documentation; the .NET validator remains the complete
acceptance authority.

**Rationale:** [RFC 8259 sections 4, 8 and 9](https://www.rfc-editor.org/rfc/rfc8259)
permit parser budgets, require interoperable UTF-8, and describe unpredictable
duplicate-name behavior. Reject duplicate **decoded** keys before any serializer
can apply last-wins behavior, including `id` versus `\u0069d`. The
[JSON Schema instance model](https://json-schema.org/draft/2020-12/json-schema-core)
does not represent duplicate object properties. Its
[validation vocabulary](https://json-schema.org/draft/2020-12/json-schema-validation)
cannot evaluate a supplied host version against a SemVer range or enforce
uniqueness by one field of an object. Those checks must remain semantic.

**Alternatives considered:** YAML adds aliases/tags/parser dependencies and
ambiguity without user value in this slice. Schema-only acceptance cannot
prove identity immutability, decoded duplicate rejection, contextual compatibility
or safe output. Runtime schema-package adoption is unnecessary for a small fixed
document; existing Ajv in the contract workspace independently checks structural
parity without adding a production dependency.

## R2 - Semantic versions and a deliberately small range language

**Decision:** SemVer 2.0.0 for module/host/capability versions, maximum 128 ASCII
characters each. Preserve build metadata but ignore it for precedence. Compare
numeric identifier text by length then ordinal digits, not bounded integer
conversion, floating point, locale or `System.Version`.

Ranges are one exact version or a lower (`>`/`>=`) and upper (`<`/`<=`)
comparator separated by exactly one ASCII space. No shorthand, wildcards,
disjunction, one-sided ranges or implicit whitespace normalization. A prerelease
target is eligible only if a comparator explicitly mentions a prerelease with
the same major/minor/patch tuple. Empty intervals reject as malformed ranges.
The same syntax describes dependency ranges, without evaluating installed sets.

**Rationale:** [SemVer 2.0.0](https://semver.org/spec/v2.0.0.html), especially
rules 2 and 9-11 and its BNF, defines numeric/ASCII prerelease precedence, prohibits
numeric leading zeros and excludes build metadata from precedence. It does
**not** standardize ranges. Thus Köfte must publish its own syntax and prerelease
policy rather than suggest npm or NuGet compatibility. Length-bounded decimal
text preserves all supported SemVer numbers without an arbitrary Int32 ceiling.

**Alternatives considered:** NuGet/npm range libraries support different, much
broader languages. A dependency only for this small grammar adds adoption surface
and risks undocumented rules. `System.Version` lacks SemVer prereleases/build
metadata. Lexical whole-version comparison orders `1.10.0` incorrectly.
Unbounded backtracking regexes are unsuitable for the production parser.

## R3 - Runtime parser, budgets and safe deterministic diagnostics

**Decision:** Use runtime `Utf8JsonReader` for token/depth/shape handling, a strict
UTF-8 check, and per-object decoded-name tracking for duplicate keys. Bound bytes
before decoding, depth while reading and collections before materializing
descriptors. Treat parser exceptions as internal data: map them to fixed
categories/messages and never return their message, path or stack.

**Rationale:** Official
[Utf8JsonReader guidance](https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/use-utf8jsonreader)
and [JsonReaderOptions](https://learn.microsoft.com/en-us/dotnet/api/system.text.json.jsonreaderoptions?view=net-10.0)
describe forward token reading, explicit `MaxDepth`, disallowed comments and
trailing-comma controls. Defaults are not the complete Köfte policy: duplicate
handling and total byte/collection/output limits need explicit enforcement.
Tracking names while reading also permits a stable duplicate category without
depending on runtime exception text or a particular serializer option.

Use ordinal comparisons for identifiers and fixed diagnostic ordering.
[.NET string comparison guidance](https://learn.microsoft.com/en-us/dotnet/standard/base-types/best-practices-strings)
recommends ordinal comparison for non-linguistic identifiers. No input-derived
property names, IDs or ranges enter output paths/messages. String limits use
Unicode scalar counts, while byte budgets use original UTF-8 bytes.

**Alternatives considered:** Deserializing then checking duplicates loses raw
information. Passing `JsonException.Message` through can disclose input/runtime
details. Global mutable caches create retry/locale contamination risks.
Silent truncation of declarations changes the accepted meaning and is forbidden.

## R4 - Minimal shared types, consumer proof and no runtime side effects

**Decision:** One `Kofte.Platform` library with public immutable descriptions,
explicit context and accepted/rejected result variants. No public setter or
caller-owned mutable list may survive into a descriptor. Private defensive
copies exposed as read-only collections are sufficient; no additional immutable
collection package or generic repository/mediator abstraction.

The author console references only that library; the separate host-facing
contract consumer proves the host can consume the same public surface without
host-internal dependencies. No loader, DI registration lifecycle, registry,
environment-variable context fallback or new host command is added.

**Rationale:** Existing `Directory.Build.props`, `Kofte.slnx`, host startup and
verification are small and module-free. A genuinely shared assembly satisfies
FR-001/013 without coupling module authors to ASP.NET Core, Npgsql or SQLite.
No new runtime package is needed. Existing xUnit/Ajv tooling can prove behavior;
lock/inventory changes remain subject to normal implementation adoption review.

**Alternatives considered:** Keeping public contracts in the host assembly
pulls host concerns into consumers. Creating plugin lifecycle/resource/provider/
MCP service interfaces now guesses later numbered specs. A production validation
endpoint/CLI installer expands authority unnecessarily. A test consumer is not
the separately packaged full vertical reference module required by 020.

## R5 - Version domains, metadata and future compatibility

**Decision:** Schema string `1`; initial published host contract `1.0.0`; every
listed declaration kind supports capability-contract `1.0.0` only. Capability
kind is unique even when versions differ. Full SemVer is parsed before checking
supported capability versions by precedence (build metadata does not change
support). Prerelease capability contracts are unsupported in this initial table.

Permissions use local names attributable to `(moduleId,name)`; storage declares
only a relational need. No team/permission grant, physical database location or
namespace ownership verification. Unsupported schema/kind/capability-contract
versions fail explicitly. Mathematically comparing an arbitrary explicit valid
host SemVer is allowed; it does not publish or certify that host runtime.

**Rationale:** The spec requires four separate version domains and full level-3
declaration coverage, not early runtime APIs. Closed vocabularies make mistakes
actionable and versioned changes reviewable. Identity remains an assigned
namespace convention, not DNS/signature verification.

**Alternatives considered:** Using product/assembly version as the default host
contract hides missing context. Arbitrary extension dictionaries or accepting
unknown versions silently undermine fail-closed behavior. Physical schema/file
fields would bypass 003's registry ownership.

## Remaining gates, not unresolved planning questions

The precise limits, grammar, API shape, schema, vocabulary, consumer and reference
environment are chosen in the accompanying design. No material research question
or clarification marker remains. ADR 0004 remains Proposed pending human review.
Implementation must review its project locks/inventory, prove the corpus and
timing/security expectations, wire CI and preserve bootstrap regressions.
Under the subsequent explicit author-guide amendment, SC-001 needs actual
automated guide/example/correction/retry evidence, not a human exercise or
elapsed-time attestation. Later review and explicit human feature acceptance
remain separate gates.
