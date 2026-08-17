# SDK compatibility policy

This document describes how Nexo classifies NuGet packages and public APIs, what the stability promise is for each tier, which mechanism enforces that promise in the build, and how breaking changes are handled.

The HTTP API has its own, separate policy: [`docs/api/versioning.md`](api/versioning.md).

## The v0.1.0 promise

Nexo launches as **v0.1.0 = "production-usable core, experimental autonomy"** (decision D5; owner to confirm). Concretely:

- **Stable tier: no breaking changes within `0.1.x`.** A breaking change to a stable-tier public API ships only in the next minor (`0.(x+1).0`), and only after the old shape carried an `[Obsolete]` deprecation in the prior minor. Additive changes (new types, new optional parameters, new overloads) may ship in any `0.1.x`.
- **Experimental tier may change at any time**, including in a patch release. Its use is a compile-time opt-in (see [NEXOEXP001](#nexoexp001)).
- **Internal tier carries no promise.**

Until `1.0.0`, "MAJOR" in the [Semantic Versioning 2.0.0](https://semver.org/) sense is the minor digit; from `1.0.0` the ordinary SemVer rules apply (breaking changes only in a MAJOR bump, MINOR is additive, PATCH is fixes).

## Package tiers

### Stable

These packages are intended for external integration and carry the promise above.

| Package | Enforced by |
|---------|-------------|
| `Nexo.Sdk` | `Microsoft.CodeAnalysis.PublicApiAnalyzers` (`PublicAPI.Shipped.txt` / `PublicAPI.Unshipped.txt` in the project) + `PublicApiGenerator` snapshot (`application/src/Nexo.Tests.CLI/PublicApi/Nexo.Sdk.approved.txt`) |
| `Nexo.Client` | `Microsoft.CodeAnalysis.PublicApiAnalyzers` |
| `Nexo.Brick.Contracts` | `Microsoft.CodeAnalysis.PublicApiAnalyzers` |
| `Nexo.Authoring` | `Microsoft.CodeAnalysis.PublicApiAnalyzers` + `PublicApiGenerator` snapshot (`Nexo.Authoring.approved.txt`) |
| `Nexo.Hosting.Bundle` (metapackage: references the `Nexo.Hosting` graph at a single version) | `Microsoft.CodeAnalysis.PublicApiAnalyzers` (declared surface is empty by design; the analyzer is there so it stays empty unless reviewed) |

`Nexo.Abstractions` is not a stable-tier package, but it is the contract assembly the stable packages transitively expose, so it runs the same analyzer with the same files. `Nexo.Framework.Sdk` is covered by a `PublicApiGenerator` snapshot only (`Nexo.Framework.Sdk.approved.txt`).

#### How the analyzer enforces the promise

Every project in the table references `Microsoft.CodeAnalysis.PublicApiAnalyzers` and declares its public surface in two text files next to the `.csproj`:

- `PublicAPI.Shipped.txt` - the surface that has been released under the promise. Removing or changing a line here is a breaking change: the analyzer reports RS0017 ("Symbol '...' is part of the declared API, but is either not public or could not be found") for a symbol whose declaration no longer matches its line.
- `PublicAPI.Unshipped.txt` - public surface added since the last release. A new public symbol that is in neither file fails the build with RS0016 ("Symbol '...' is not part of the declared public API"). Malformed or duplicated API files fail with RS0024/RS0025.

The build treats analyzer warnings as errors (`TreatWarningsAsErrors` in `Directory.Build.props`), so **an unreviewed public-API change fails every CI job that builds the project - `cert-gate` and `kernel-gate` for `Nexo.Abstractions`, `Nexo.Brick.Contracts` and `Nexo.Client` (through `Nexo.Tests.Infrastructure`), and the `Nexo.sln` build (`cross-platform-tests`) plus the release pack (`scripts/pack-nexo-hosting-graph.sh` via `reusable-release-nuget.yml`) for `Nexo.Sdk`, `Nexo.Authoring` and `Nexo.Hosting.Bundle` (the bundle is in no solution; the pack is its only CI build) - and it fails the local `dotnet build` that produced it**. To make a public-API change, edit the text file in the same PR: additions go to `Unshipped.txt`; a removal of a `Shipped.txt` symbol is recorded as a `*REMOVED*<symbol>` line in `Unshipped.txt` (the analyzer's convention; the `Shipped.txt` line is dropped at the next promotion) plus an entry under **Breaking** in `CHANGELOG.md`, and is only accepted in a `0.(x+1).0` PR. `dotnet format analyzers --diagnostics RS0016` applies the analyzer's code fix and appends the missing lines for you.

The analyzers' RS0036 (missing nullable annotation in the API file) and RS0037 (nullable context missing) are disabled (`NoWarn`) uniformly, matching the original `Nexo.Brick.Contracts` configuration; every API file starts with `#nullable enable`.

#### Release step: promote Unshipped -> Shipped on tag

Nothing has shipped yet, so **all** of the current surface (including the 438 lines `Nexo.Brick.Contracts` had accumulated in its `Shipped.txt` before this policy was enforced) lives in `PublicAPI.Unshipped.txt`, and every `PublicAPI.Shipped.txt` contains only the `#nullable enable` header. This is deliberate: the first tag freezes a **reviewed** baseline rather than whatever happened to be public the day the analyzer was switched on.

When tagging `v0.1.0` (and every release after it), as part of "Before you tag" in `docs/RELEASE_RUNBOOK.md`:

1. Review `PublicAPI.Unshipped.txt` in each stable-tier project. Anything that should not be promised gets made `internal` (or `[Experimental]`) **before** the tag.
2. Move every line except the `#nullable enable` header from `PublicAPI.Unshipped.txt` to `PublicAPI.Shipped.txt`; leave `Unshipped.txt` with the header only.
3. Commit as `chore(api): promote unshipped public API to shipped for vX.Y.Z` on the release commit.

From that point on, a change to a `Shipped.txt` line is a breaking change under this policy.

#### Code-brick authoring surface

The `nexo new brick` code-brick path references `Nexo.Authoring` and exposes the following authoring types as a stable contract. Their namespaces are preserved for source compatibility with existing consumers; their implementation is hosted in the stable brick contract assembly (`Nexo.Brick.Contracts`), so they are covered by its `PublicAPI.*.txt` files.

- `Nexo.Core.Domain.Bricks.Brick`
- `Nexo.Core.Domain.Bricks.BrickCategory`
- `Nexo.Core.Domain.Bricks.BrickInterface`
- `Nexo.Core.Domain.Bricks.BrickInputDefinition`
- `Nexo.Core.Domain.Bricks.BrickOutputDefinition`
- `Nexo.Core.Domain.Execution.BrickInput`
- `Nexo.Core.Domain.Execution.BrickOutput`
- `Nexo.Core.Domain.Bricks.ImplementationType`
- `Nexo.Core.Domain.Execution.IExecutionContext`

### Experimental

APIs are marked with the .NET [`Experimental`](https://learn.microsoft.com/dotnet/api/system.diagnostics.codeanalysis.experimentalattribute) attribute. They may change or be removed in any release, MINOR or PATCH, without a deprecation window. Consumers should treat them as preview-only and pin package versions if they take a dependency.

The compiler turns every use of an experimental API into an **error** carrying the diagnostic id below, so taking the dependency is always a visible, per-diagnostic opt-in rather than something a transitive reference can smuggle in.

#### NEXOEXP001

The one experimental diagnostic id today; new ids get their own row (and their own constant) rather than reusing this one.

| Diagnostic | Surface | Meaning | How to opt in |
|------------|---------|---------|---------------|
| `NEXOEXP001` | The **autonomy (self-extension) loop**: `Nexo.Core.Application.Autonomy.*` (`TouchSet`, `ObjectiveTierClassifier`, `TrustKernel`, `GenerationLineage`/`RecursionDiscipline`, `IProposalSource` and the proposal/repair records, `ICertificateRevocationList`, `ILineageAuthority`, `LoopPauseControl`, `ClusterBudget`, `RepairFeedbackPolicy`, `ObjectiveSource`); `Nexo.Infrastructure.Certification.HotSwap.*` (`AutonomousIterationHarness`, `CertifiedBrickHotSwapHost`, the swap/admission/provenance models, session build/execution backends, `AutonomyDigest`, `RepairFeedback`); `Nexo.Infrastructure.Autonomy.*` (`AddNexoAutonomy`, `NexoAutonomyOptions`, digest and reaper services); `Nexo.BackgroundAgents.Autonomy.*` (`AutonomyLoopService`, `AddAutonomyLoop`, `OllamaProposalSource`, `ObjectiveArtifacts`) plus `TelemetryObjectiveExtractor` and the `Source`/`Touch` members of `ObjectiveDocument`; `AutonomyLedgerScan`; the `TouchSet`/`Lineage` members of `CertificationRequest`; and `CertificationServiceCollectionExtensions.AddCertifiedBrickHotSwapHost`. | The trust-loop extension APIs are usable and tested, but their shapes are still being driven by the dogfood campaigns (`docs/certification-evidence.md`) and may change without a deprecation window. **The certification gate itself (`ICertificationGate`, `CertificationRequest` minus the two members above, the witness/mutation checks) is NOT experimental** - it is the product; only the self-extension surface around it is. | Per call site: `#pragma warning disable NEXOEXP001` / `restore`. Per project (you accept the whole surface): `<NoWarn>$(NoWarn);NEXOEXP001</NoWarn>` in the `.csproj`. The Nexo repo's own test projects do the latter in `Directory.Build.targets`. |

The diagnostic id and the help link it carries are defined once, in `Nexo.Core.Application.Autonomy.AutonomyExperimental` (`DiagnosticId`, `UrlFormat`), and applied as `[Experimental(AutonomyExperimental.DiagnosticId, UrlFormat = AutonomyExperimental.UrlFormat)]`. That holder type is deliberately not experimental itself (a member-level attribute binds its arguments in the containing type's scope and would otherwise trip the diagnostic it names).

`netstandard2.0` targets: `System.Diagnostics.CodeAnalysis.ExperimentalAttribute` is a `net8.0+` BCL type. `Nexo.Core.Application` (multi-targeted `netstandard2.0;net8.0`) compiles an internal polyfill of the attribute (`src/Nexo.Compat/Polyfills/ExperimentalAttribute.cs`, linked into every `.NETStandard` inner build by `Directory.Build.targets`); the compiler recognises the attribute by its full name, so a `netstandard2.0` consumer gets the same `NEXOEXP001` diagnostic as a `net8.0` one. Nothing is documented-only.

### Internal

All other assemblies and packages are internal to the Nexo repository and tooling. They are not covered by this compatibility promise unless explicitly promoted to a stable package. `Nexo.Infrastructure.*`, `Nexo.Core.Application.*` (outside the experimental namespace above) and `Nexo.Hosting` internals may change in any release; the supported way to reach them is through the stable packages' entry points (`AddNexo`, `INexoClient.InvokeAsync`, the authoring base types).

## Breaking change process

1. Prefer additive changes (new types, new optional parameters, new overloads) over modifying existing contracts. Additive changes go into `PublicAPI.Unshipped.txt` in the same PR and may ship in any patch.
2. For stable packages, deprecate first: mark the old shape `[Obsolete]` with a clear message and migration path in minor `0.x`; remove or change behavior only in `0.(x+1).0` (from `1.0.0`: only in the next MAJOR). The removal PR carries the `*REMOVED*` line in `PublicAPI.Unshipped.txt` and a **Breaking** entry in `CHANGELOG.md`.
3. Document notable changes in release notes and, when applicable, in migration notes for integrators.
4. Experimental APIs may change in MINOR or PATCH releases; announce significant shifts in release notes when practical. Promoting an experimental API to stable = removing the `[Experimental]` attribute in a PR that also adds it to `PublicAPI.Unshipped.txt` of a stable-tier package (it becomes promised on the next tag). Demoting a shipped stable API to experimental is a breaking change and follows step 2.

## CI

- The public-API analyzer runs in **every** build of the covered projects with `TreatWarningsAsErrors`; it is not a separate job and cannot be skipped by path filters. `cert-gate` and `kernel-gate` (`.github/workflows/`) build `Nexo.Abstractions`, `Nexo.Brick.Contracts` and `Nexo.Client`; the `Nexo.sln` build (`cross-platform-tests`) covers `Nexo.Sdk` and `Nexo.Authoring`, and the release pack (`reusable-release-nuget.yml`) is the one CI build of `Nexo.Hosting.Bundle`; `production-readiness-gate-v1` and `runtime-release-gate` build only the CLI graph. An unreviewed public-API change fails them, and fails the local `dotnet build` that produced it.
- The `PublicApiGenerator` snapshot test (`application/src/Nexo.Tests.CLI/Tests/Commands/BrickAuthoringPublicApiSnapshotTests.cs`) is a second, independent witness for `Nexo.Sdk`, `Nexo.Authoring` and `Nexo.Framework.Sdk` and fails on any surface change until the `.approved.txt` file is updated in the same PR.
