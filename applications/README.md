# `applications/` — open products built on the Ashlar core

Three similarly named folders in this repository mean three different things:

| Folder | What lives there | License |
|--------|------------------|---------|
| **`application/`** (singular) | The two deployable hosts, `application/src/Ashlar.CLI` and `application/src/Ashlar.API`, plus `application/src/Ashlar.Tests.CLI`. Built by `application/Ashlar.Application.sln`. | Apache-2.0 |
| **`applications/`** (plural, this folder) | Products **on top of** the kernel: they reference `src/` and are never referenced by it. | Apache-2.0 |
| **`apps/`** | Agent-set / host **configuration** surfaces (`runtime-studio`, `ashlar-forge`, `game-director`, `release-manager`) — no `.csproj`. | Listed as commercial in [`LICENSING.md`](../LICENSING.md) |

## Licensing

Everything under `applications/` is **Apache-2.0** by rule: `Directory.Build.targets` sets `PackageLicenseExpression=Apache-2.0` for every project that does not declare `AshlarCommercialProject=true`, and no project here declares it. Commercial code lives only under `commercial/`.

## Boundary rules

- **Applications depend on the core; never the reverse.** `scripts/verify-open-commercial-dependency-boundary.py` (CI: `.github/workflows/dependency-boundary.yml`) fails when any `src/` project references an `applications/` project. Layout carries the autonomy tier: `TrustKernel.KernelPathPrefixes` is a list of `src/` prefixes, so a project dragged into `src/` would silently re-acquire kernel tiering. See [`docs/architecture/runtime-vs-application.md`](../docs/architecture/runtime-vs-application.md).
- **`layer-boundary`** (`.github/workflows/layer-boundary.yml`) governs the singular `application/` hosts, not this folder.
- All projects here are in `Ashlar.sln`; the provenance graph also has its own gate (`.github/workflows/provenance-graph-gate.yml`).

## Projects

| Project | Role |
|---------|------|
| `Ashlar.Certification.Physical/` | Physical-atom certificate schema, Ed25519 signing, standalone verification for digital-twin asset binding |
| `Ashlar.Provenance.Graph/` | Neo4j-backed read-only certification provenance projection for lineage queries (`samples/provenance-graph/` holds the sample bundle) |
| `Ashlar.Spatial.Contracts/` | Platform-agnostic spatial anchor contracts and headless fakes |
| `Ashlar.Spatial.Runtime/` | Certified atom pose binding — the identity/pose seam |
| `Ashlar.Spatial.Multiplayer/` | Host-authoritative scoped pose relay for LAN-local play |
| `Ashlar.Spatial.Platform.ARKit/`, `Ashlar.Spatial.Platform.VisionPro/`, `Ashlar.Spatial.Platform.XREAL/` | Platform `ISpatialAnchorProvider` implementations; headless hosts fail closed until native session wiring lands |
| `Ashlar.Applications.Tests/` | Tests for the physical-atom and spatial projects |
| `Ashlar.Provenance.Graph.Tests/` | Provenance graph tests (`Category=Integration` needs Neo4j) |

Full csproj-level map: [`docs/ProjectTiers.md`](../docs/ProjectTiers.md), Tier 3a.
