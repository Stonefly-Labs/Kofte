# Bootstrap dependency adoption

**Review date:** 2026-10-21. **Scope:** spec 001, T002; not feature acceptance.
The [ADR 0002](../adr/0002-bootstrap-development-baseline.md) design is human-approved.
The generated [inventory](dependency-inventory.json) records every resolved npm
and NuGet identity, archive source/integrity, declared license, available notice
hashes, source commits where published, image digest and action SHAs.
No floating direct dependency is approved.

## Spec 005 source-provenance follow-up, before adoption

**Review date:** 2026-10-26. The
[implementation evidence](../../specs/005-authentication-bootstrap/implementation.md#t002-follow-up-source-provenance-resolved)
resolves the original IdentityModel 8.19.2 source/license blocker without
changing versions or waiving a check. The missing advertised commit belongs
to a security-advisory-fork SourceLink, not the public repository named in the
nuspec. The fork remains unavailable; SSO reauthorization was not required.

All seven net10.0 PDBs are cryptographically bound to their package assemblies.
Their 275 source-document entries match exact public source or bound embedded
generated source: 259 match official 8.19.2 source, one unchanged input matches
official 8.19.1 source, and fifteen are generated. Both immutable public
revisions have the same original Microsoft MIT license. This is compiled-input
source correspondence, not a claim that the unavailable fork's whole tree is
identical to either public tag or a reproducible-build attestation.

All nine candidate archives also passed `dotnet nuget verify --all` in the
previously reviewed immutable Linux SDK image, with zero warnings/errors and
normal online trust/revocation checks. Exact artifacts, source pins, hashes,
checkout-byte handling and scope are recorded in the implementation report.
That investigation approved source/license evidence, not package adoption.
The subsequent actual adoption below completes the remaining T002 gates.

## Spec 005 OIDC dependency adoption

**Review date:** 2026-10-26. The host now references
`Microsoft.AspNetCore.Authentication.OpenIdConnect` 10.0.13, with all seven
IdentityModel identities pinned to 8.19.2 and Bcl.Cryptography to 10.0.2.
The actual restored union is **403 npm / 87 NuGet identities**: exactly the
nine reviewed candidates above are added to the previous 78 NuGet identities.
No minimum transitive version or unreviewed identity slipped into the graph.

All nine restored signed archives are byte-identical to the candidates whose
source/license and Linux signatures were verified. Original OIDC and
Bcl.Cryptography notices remain in their restored archives and inventory,
with the previously recorded hashes; the seven IdentityModel archives do not
ship a separate notice file. Original Microsoft MIT source licenses and the
source correspondence remain linked in the implementation report.
No dependency source is copied into the repository, so the existing
source-copy third-party notice file/checker remains unchanged.

`dotnet restore Kofte.slnx --force-evaluate` and subsequent
`dotnet restore Kofte.slnx --locked-mode` passed. The dependency checker
reconciles all project locks, central pins, installed/archive provenance,
integrity and notices. The actual direct/transitive vulnerability audit
reports zero vulnerable packages across thirteen projects. The Release
solution builds with zero warnings/errors, 192 existing host unit tests pass
with zero skips, and solution formatting passes. This is dependency
compatibility evidence, not implemented authentication or feature acceptance.

### Spec 005 operational fixture project edges

**Review date:** 2026-10-28. The test-owned identity consumer now references the
existing storage consumer for real preservation/recovery recipes. Only the
identity-consumer and platform-contract lock files gain internal project edges;
no external package or pin changes. Locked restore and warnings-as-errors
solution builds pass. `npm run dependencies:check` and `npm run contract:check`
also pass on the current worktree
(`identity-final-dependencies-none-ae6f2690da1b41cfa5edcf6c90a5b411.json`).
The adopted union, archives and retained notices remain unchanged; this is not
a new signature audit or a passing full feature gate. The actual legacy-host
compatibility test separately builds immutable accepted pre-005 source with its
original locked graph in an owned private root; it does not add old packages
to the current product graph.

On the later 2026-10-28 worktree, `npm run dependencies:check` and
`npm run contract:check` pass again
(`identity-current-dependencies-contract-none-3d58bdf913d74be280568f0944f8ed9a.json`).
`npm audit --json` reports zero vulnerabilities.
`dotnet list Kofte.slnx package --vulnerable --include-transitive --format json --no-restore`
also reports zero vulnerabilities across all **14 current solution projects**
(`identity-current-nuget-advisories-none-3def18e170c24de88d486c9d0cdc014a.json`).
The first audit wrapper incorrectly required framework entries in NuGet's
vulnerable-only JSON even for projects without findings; that failed parser
attempt remains recorded in
`identity-current-advisories-none-13d3f7cca6714ae6a09488d3455ad82a.json`,
whose npm audit step passed. The corrected check requires every distinct
solution project, no findings, no CLI warnings and the expected report shape.
These are current advisory results, not a renewed archive-signature audit,
a complete verification gate or feature acceptance.

## Spec 002 project adoption

The platform library and test-owned author console introduce no runtime package.
Two dedicated test projects reuse the centrally pinned xUnit/test SDK; the
host-facing contract project also has the existing host's transitive graph.
Generated new project locks and updated host-reference locks were reviewed:
the union remains exactly **403 npm and 69 NuGet identities**, with the same
archives, integrity, provenance, licenses and retained notices below.
`dotnet restore Kofte.slnx --locked-mode` and `npm run dependencies:check`
passed after these project changes. No pin, source, license decision, production
test dependency or new NuGet identity was added; existing Ajv 8.20.0 is reused.

## Spec 004 DI project-edge review

**Review date:** 2026-10-24 (UTC). Accepted ADR 0006 requires
`Microsoft.Extensions.DependencyInjection.Abstractions` **10.0.13** in
`Kofte.Platform` for the public native-registration signature. Before adding
that edge, the actual restored official archive/nuspec was inspected:
MIT, source `dotnet/dotnet` commit
`4099f83977f4d29604a9aeb971fb0a7bc1d5f699`, with explicit `net10.0` assets
and **no net10.0 package dependencies**. Framework-specific dependencies for
net462/netstandard2.0 do not enter this target's closure.

The existing reviewed inventory's NuGet content hash matches `.nupkg.metadata`;
the signed archive's raw SHA-512 separately matches its retained `.nupkg.sha512`.
These are different NuGet hash domains, not interchangeable archive hashes.
The existing `THIRD-PARTY-NOTICES.TXT` retains SHA-256
`6d15e10a101c6bfff2ab4429ed061bf76c456fc4b23ad6b03e0d0f8377148a21`.
Both current official NuGet vulnerability-feed pages were retrieved and contain
no advisory entry for this identity. Existing supported Linux signature
verification and notice-retention decisions remain applicable; no unsupported
macOS signature verification is relabeled as passing.

This exact new project edge adds no package identity, version, native asset,
commercial term, loader, mediator or mapper. Independent test module/deployment
projects reuse the reviewed infrastructure/test graph. Generated locks and the
complete dependency checker must still confirm that actual adoption matches
the reviewed union of 403 npm / 78 NuGet identities.

## Spec 003 candidate review

**Review date:** 2026-10-22 (UTC). Explicit human ADR 0005 approval authorizes
the design, not arbitrary packages. Before adoption, the official NuGet archive
closure was inspected for EF Core/Relational/Sqlite **10.0.13** and Npgsql EF
**10.0.3**. Nine new identities are required: those four, EF Abstractions,
Analyzers and Sqlite.Core 10.0.13, plus Microsoft.Extensions.Caching.Memory
and Caching.Abstractions 10.0.13. Their remaining dependencies reuse the
reviewed baseline. EF Design, global tools and another SQLite bundle are
unnecessary and are not approved.

All eight new Microsoft identities declare MIT and exact source commit
`4099f83977f4d29604a9aeb971fb0a7bc1d5f699` in `dotnet/dotnet`; Npgsql EF
declares PostgreSQL and source commit
`5e912bf071068a44e5abbdab497b313ba99d3ee1` in `npgsql/efcore.pg`.
The exact-commit upstream licenses were retrieved. Both caching packages'
retained `THIRD-PARTY-NOTICES.TXT` match SHA-256
`6d15e10a101c6bfff2ab4429ed061bf76c456fc4b23ad6b03e0d0f8377148a21`.
The original notice/attribution retention rules below apply to all nine.

Every new runtime assembly has `net10.0` assets (Analyzers is analyzer-only).
Npgsql EF's `[10.0.4,11.0.0)` EF range admits the exact 10.0.13 pins.
The SQLite adapter retains Microsoft.Data.Sqlite 10.0.13 and the same reviewed
SQLitePCLRaw 2.1.13 native graph for macOS arm64/Ubuntu x64; no new native
library or target claim is introduced. Actual restore/build/provider evidence
must still confirm the adopted graph.

The two official NuGet vulnerability-feed pages contained no applicable range
for these nine versions; the caching-memory entries affect only older 6/8/9
versions. All nine signed archives passed `dotnet nuget verify --all` in the
already reviewed immutable Linux SDK image
`mcr.microsoft.com/dotnet/sdk@sha256:0d99ebf1ccad15037b5c98c9fe8979d6b8dbac23812feae2277a0b277b5c5ffa`,
with **zero warnings/errors** and online revocation available. An initial
network-disabled attempt returned revocation warnings and is not counted as
clean verification; no verification/trust setting was weakened.

This review approves only this exact closure for adoption. Final project
locks, inventory reconciliation, locked restore, advisory audit and actual
both-provider tests remain required before their tasks can complete.

## Adopted baseline and compatibility

Use .NET SDK **10.0.402**, runtime **10.0.13**, Node **24.22.0** and bundled npm
**11.19.1**. Official release metadata confirmed these releases. The isolated
macOS tool archives were verified before use: Node SHA-256
`6a9724d9b50a5f22b4816cded441b5a8d9ddae546191a5e5b7efa5a5e2ee26d5`;
SDK SHA-512
`ee0d379c144ebddef6efe851e1514883d1b695430d13b3fb13e8153f8476e1e5ac7abc42c08668471aaf4d738ff367cf53009845f94d224ba126febe74d00b75`.
No global tool installation or trust-store modification is required.

Next **16.4.2**, React/React DOM **19.3.1**, TypeScript **6.0.3**, Tailwind
**4.3.3**, Radix Slot **1.5.0** and Lucide **1.60.0** form the runtime UI baseline.
The actual complete graph, not this short direct-dependency summary, controls adoption.

Two compatibility corrections were necessary:

- ESLint **10.13.0**, `@eslint/js` **10.0.1**, `typescript-eslint` **8.72.0**
  and React Hooks rules **7.1.1** use native flat configuration. Next's
  `eslint-config-next` 16.4.2 requires an import plugin incompatible with ESLint
  10; ESLint 9.39.5 is unsupported. Its Next plugin also pulls the unpatched
  braces stack-exhaustion advisory
  [GHSA-vfj7-8cjw-p6xm](https://github.com/advisories/GHSA-vfj7-8cjw-p6xm).
  Neither unsupported ESLint nor that preset/plugin is adopted. Next compilation,
  typed lint and React Hooks checks remain required; no peer override or forced
  installation is used.
- `openapi-typescript` **7.13.0** has a TypeScript `^5.x` peer. The generation-only
  workspace pins **5.9.3** locally, while application/contract type checking uses
  **6.0.3**. Both versions are reviewed and locked. This is not a downgrade of
  the application baseline. Vite **7.3.7**, React plugin **5.1.4** and Vitest
  **5.0.5** have compatible peers.

The remaining exact direct/transitive versions are in the inventory and locks.
They include xUnit, Microsoft.NET.Test.Sdk, real SQLite/Npgsql, OpenTelemetry,
Playwright/axe, Testing Library, JSON-schema validation and formatting.
PostgreSQL 17 and GitHub Actions use the immutable references in the inventory.

The actual-project drift check found that the unit project's transitive
Microsoft.Extensions minimums initially resolved 15 additional **10.0.0**
identities, rather than the already reviewed **10.0.13** identities. SDK/runtime
patch selection does not pin NuGet transitives. Central transitive pinning now
explicitly selects those 15 reviewed 10.0.13 packages; the regenerated locks and
locked restore must match the 69-identity reviewed union. This corrects the
earlier candidate-versus-actual graph gap without adopting unreviewed 10.0.0
archives. The drift checker also exposed omitted British-spelled `LICENCE`
hashes for decimal.js and std-env; their already retained MIT notices were
checked against the pinned archives and are now recorded.

The first Ubuntu CI then exposed three Linux-only notice omissions:
`@img/sharp-linux-x64@0.35.5` (Apache-2.0),
`@tailwindcss/oxide-linux-x64-gnu@4.3.3` (MIT) and
`lightningcss-linux-x64-gnu@1.32.0` (MPL-2.0). Their official archives matched
the already reviewed SHA-512 integrities; `package/LICENSE` SHA-256 values
matched installed Linux notices and reviewed Darwin sibling notices. The
inventory now retains those hashes without changing identity/source/version/
license adoption or relaxing unknown-notice enforcement.

A clean npm cache also lacked the reviewed
`@tailwindcss/oxide-wasm32-wasi@4.3.3` archive needed to prove six bundled
identities. `dependencies:check` explicitly prepares bundled evidence via
`python3 scripts/check_dependencies.py --prepare-bundled-evidence`, using
`npm cache add` only for reviewed immutable sources, then checking original
integrity, metadata, licenses and notices. This downloads evidence, not a new
dependency or lifecycle script approval. Offline checks need that verified
cache populated in advance; missing evidence must fail, not disappear.

Native Ubuntu verification also exposed NuGet's normalized installed metadata
filenames: an archive may contain `Microsoft.ApplicationInsights.nuspec` while
the restored file is lowercase. The checker reads NuGet's canonical installed
name and still compares the full installed/archive provenance metadata.
Case-sensitive cold-cache regression and actual Ubuntu CI pass; missing,
nonregular or tampered metadata remains a failure. No archive hash, license
decision or reviewed identity changed to accommodate that platform difference.

## License and notice decisions

This is a source checkout/local-development adoption, not approval to discard
notices in a later binary/container distribution. Restore retains each package's
original LICENSE/NOTICE files; the inventory hashes available installed notices.
Do not strip them from future standalone output, images or browser bundles.
The following decisions cover the complete observed license-expression set:

| License family / component | Decision and required compliance |
| --- | --- |
| MIT, MIT-0, ISC, 0BSD, BSD-2/3-Clause, BlueOak-1.0.0, PostgreSQL | Approved with original copyright, license and disclaimer retention; no endorsement claims. |
| Apache-2.0, including compound expressions | Approved; retain license/NOTICE, preserve attribution, identify modifications and observe patent terms. |
| CC0-1.0 / MIT OR CC0-1.0 | Approved; choose MIT where dual-licensed and retain its notice. |
| Python-2.0 (`argparse`) | Approved; retain the complete Python-derived license/disclaimer and attribution; no trademark endorsement. |
| CC-BY-4.0 (`caniuse-lite`) | Approved with attribution to the original caniuse contributors, license/source link and modification indication in redistributed notices. |
| MPL-2.0 (axe, lightningcss and platform binaries) | Approved with file-level copyleft compliance. Preserve MPL notices and corresponding source/version links; disclose modifications to covered files if any. Test-only use is not an exemption. No covered source modification is planned; MIT application files are not relicensed. |
| LGPL-3.0-or-later and compound sharp binary expressions | Approved for unmodified, dynamically linked upstream libraries, retaining all upstream notices. Redistribution must provide exact corresponding source, build/relink information and LGPL/GPL license texts, permit replacement/debugging of the library, and retain copyright/patent notices. Do not statically combine these libraries into a restricted binary or strip source offers. Production packaging must verify compliance again. |

Sharp's libvips bundles contain additional native libraries not represented as
separate npm nodes. Their shipped README was inspected: aom (BSD plus AOM patent
license), cairo (MPL-1.1), fontconfig/FreeType/libpng/libtiff (their permissive
notices), mozjpeg (zlib/IJG/BSD), and the listed LGPLv3 GLib/Pango/libvips family.
These obligations are retained together, not collapsed to an MIT claim.
Sources/build recipes and exact native versions come from the
[sharp-libvips release](https://github.com/lovell/sharp-libvips/releases/tag/v1.3.4)
and its package metadata. Preserve the package README and upstream source offers
if distributing its binaries. This bootstrap does not distribute compiled assets.

Minimal shadcn-style source must retain the
[shadcn MIT notice](https://github.com/shadcn-ui/ui/blob/main/LICENSE.md)
(reviewed license blob `fad4d887a681dd49233e5ed01ee2c7a1513089a0`);
Lucide retains ISC and icon-specific notices in its package. No commercial
mediator, AutoMapper, cloud SDK or Docker Desktop license is introduced.
Compatible Docker Engine/Compose suffices; Desktop remains optional under its
own terms. Unknown/new obligations block adoption until explicitly reviewed.

## Provenance and vulnerability evidence

Review used a disposable manifest/lock graph outside the source tree, with
`npm install --package-lock-only --ignore-scripts --strict-peer-deps` and
`npm ci --ignore-scripts`. No dependency lifecycle script ran during inspection.
Every external npm lock entry has an archive source and integrity hash.
The adopted npm 11.19.1 installation policy leaves the unapproved esbuild and
optional fsevents lifecycle scripts blocked; `npm ci` reports that explicitly.
No script approval was added. The actual builds and tests work without those
scripts. Next CLI telemetry is disabled in all web workspace lifecycle commands,
not only when the local supervisor supplies the environment.
The candidate complete graph has **413 external lock entries**, including
cross-platform optional packages; deduplicated identities are in the inventory.
`npm sbom --sbom-format cyclonedx` passed. `npm audit` reported **zero findings**.
`npm audit signatures` verified **310 registry signatures and 128 attestations**
on installed packages; uninstalled platform-optionals remain integrity-pinned,
not falsely counted as locally verified signatures.

The resolved NuGet graph contains **69 packages**, all MIT/Apache/PostgreSQL
licensed, with no reported direct/transitive vulnerability. Native macOS
`dotnet nuget verify --all` failed certificate/timestamp trust chains. Microsoft
[documents this verification as unsupported on macOS](https://learn.microsoft.com/en-us/dotnet/core/tools/nuget-signed-package-verification).
No check was disabled and no certificate was trusted manually: the same 69
archives were verified **successfully** using Microsoft's pinned Linux SDK
image and its supported code-signing/timestamp bundles. Restore source is
`https://api.nuget.org/v3/index.json`; integrity and published source commits are
recorded. This platform difference must remain visible in verification guidance.

Any manifest/lock change must reconcile the entire graph, notices, integrity,
peer compatibility and advisory results. A name allowlist or zero-vulnerability
scan alone is not a license/provenance review. Generated inventory mismatch,
missing mandatory evidence or unknown license fails closed. This review does
not replace the later both-provider/browser/privacy/manual/CI acceptance gates.
