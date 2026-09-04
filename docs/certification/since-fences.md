# Since-fences: what the certificate actually binds

This page is the D3b reconciliation of compile-authority claims against the code
that shipped on this branch. Every sentence here is true of the current tree;
there are no unverified markers.

Published package version (the public feed): see `ci/published-version`. That
file, not `VERSION`, is what consumers have. `VERSION` may already read ahead
of a release that has not been published.

## Class A — judged = compiled = shipped

- `BrickCertificationProjectLoader` never runs `dotnet build` on an author
  project. Author `Target` / `UsingTask` / `Import` / `Exec` / Analyzer items
  and toolchain sidecars (`NuGet.Config`, `global.json`, `Directory.Build.*`)
  are refused by `BuildSurfaceFence` before any restore.
- Source is decoded as strict UTF-8 (`StrictUtf8SourceDecoder`).
- The certifier compiles the candidate itself
  (`GateEmittedArtifactCompiler`) under closed-world `BrickCompileOptions`
  (C# 12, no unsafe, library, Release). The compiler ceiling is named in
  refusals (`CompilerCeiling`).
- Type discovery is metadata-only (`MetadataBrickDiscovery`). Constructors do
  not run to find out what the program is.
- `IlImportFence` inspects the emitted PE before anything loads it.
- `CertifiedBrickActivator` is the single disk-path activation site.
- The certificate records `gate-emitted-artifact`, `compile-options`,
  `certifier-identity`, `il-import-fence`, and `execution-mode` as signed
  inputs. Exporters write `gate-emitted-brick.dll` next to the record.
  `CertificationTrustVerifier.Verify(..., artifactBytes)` binds those bytes.
  `CertificationVerifyOptions.Strict` requires the artifact and the judge.

## Class B — certifier boundary

- `ci/certifier-boundary-inventory.tsv` is a shrink-only freeze of
  `Assembly.LoadFrom` / `LoadFromAssemblyPath` / `Activator.CreateInstance`
  inside `Ashlar.Infrastructure.Certification*`.
- `CertifierBoundaryScanTests` fails on a new site or a ghost row.

## Class C — corpus and docs

- `tests/adversarial-corpus/` holds fixtures with `expect.json`.
  `AdversarialCorpusTests` replays them inside the cert-gate.
- `scripts/verify-docs-published-version.sh` (and
  `PublishedVersionDocsLintTests`) keys published-version claims on
  `ci/published-version`, never `VERSION`.

## Known limits (accepted)

- In-process unit tests that construct a brick fixture still execute that
  fixture; they record `execution-mode=in-process-fixture` and do not ship.
- `Ashlar.CertifyBrick`'s unexpected-exception path prints raw exception text.
- On Linux a same-uid child can read `/proc/<pid>/environ`; the environment
  allowlist governs what a child is given, not what it can read.
