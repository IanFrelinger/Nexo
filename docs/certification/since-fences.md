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
- `IlImportFence` inspects the emitted PE before anything loads it. It is an
  **allowlist** (inventory `allowlist-v3`) of the API surface a deterministic
  brick needs (primitives and math, collections, LINQ, text, globalization,
  tasks, the compiler's async and interpolation plumbing, and the Ashlar brick
  contracts). The walk covers method bodies, signatures, fields, `ldtoken`,
  P/Invoke metadata, `calli`, and a named set of load/calling-convention
  attributes (`DllImport`, `LibraryImport`, `UnmanagedCallersOnly`,
  `ModuleInitializer`). Reflection, I/O, process, environment, interop, and
  assembly loading are refusals. Round 10 beat the type-denylist five ways;
  round 11 beat the v2 call-only allowlist via P/Invoke, module initializers,
  and `ldtoken`. Each of those attacks is now a corpus fixture.
- The loader refuses a second author `.cs` file (judged would not have been
  the project the author handed over) and compiles under
  `BrickCompileOptions` (C# 12, no unsafe, Release). Mutants, the analyzer
  fence, and hot-swap rematerialize use the same parse/emit options and the
  same `CandidateSourceWrapper`. The in-session `dotnet build` path pins the
  same MSBuild properties.
- `CertifiedBrickActivator` is the single disk-path activation site. The
  autonomy harness activates the gate-emitted artifact for in-process legs
  and passes those bytes into the first hot-swap so the host does not
  recompile.
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

- **Author logic executes inside the certifier process** during the witness,
  determinism, and mutation legs. The IL import fence is the boundary between
  that logic and the process holding the signing key, which is why the fence is
  an allowlist and why widening it re-stamps certifier identity. OS-level
  containment of the in-process legs is deferred.
- In-process unit tests that construct a brick fixture still execute that
  fixture; they record `execution-mode=in-process-fixture` and do not ship.
- Default `CertificationTrustVerifier.Verify` still accepts HMAC-era records
  that omit the artifact hash. `CertificationVerifyOptions.Strict` is the
  preset that means "this certificate names the bytes it judged."
- Parent-directory `Directory.Build.props` is not inspected. The certifier
  never runs MSBuild on author files, so ancestor props are inert; a
  regression that reintroduced `dotnet build` on an in-repo sample would also
  see the repo's own Directory.Build files.
- Session-built PE bytes (when the in-session build/execute legs are on) are
  not the same image as the host `GateEmittedArtifact`. The certificate binds
  source + toolchain (`session-build`) separately from the host emit.
- `Ashlar.CertifyBrick`'s unexpected-exception path prints raw exception text.
- On Linux a same-uid child can read `/proc/<pid>/environ`; the environment
  allowlist governs what a child is given, not what it can read.
- Constructor / module-initializer *hangs* (infinite loops with no forbidden
  import) are still activation-time. Discovery is metadata-only; load is not.
