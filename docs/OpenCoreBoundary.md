# Open-core boundary

This page is the source of truth for Ashlar's open-core split. It describes the boundary as it exists in code, not as an aspiration. Older phase and extraction docs should point here when they discuss open vs. commercial placement. Licensing tiers, the seven **covenants**, and the in-force **evaluation grant** live in [`/LICENSING.md`](../LICENSING.md); this page covers placement and enforcement.

## Boundary rule

- **Open projects** under `src/` and `application/src/` may not reference projects under `commercial/`.
- **Commercial projects** under `commercial/` may reference open projects.
- The open-to-commercial allowlist is intentionally empty; any future exception must carry a justification comment and pass review.

The rule is enforced by:

- `scripts/dependency-boundary-gate.sh`
- `scripts/verify-open-commercial-dependency-boundary.py`
- `.github/workflows/dependency-boundary.yml`
- `.github/workflows/layer-boundary.yml`

## Open surface

Open code is Apache-2.0 and covers:

- core runtime/orchestration contracts and implementation seams;
- brick authoring, SDK, clients, hosting, and packages;
- single-node `Ashlar.CLI` and `Ashlar.API` shells;
- local/offline mesh primitives: file-based discovery, local transport, capability advertisement, capability requester/fulfiller, artifact negotiation, and virtual mesh-lab worker clients;
- offline/air-gapped operation, trust primitives, policy packs, barrier identity, sanitization, and audit primitives.

`Ashlar.API` currently also contains lightweight product scoping primitives such as tenant resolution and organization membership checks for single-node/cloud-shaped API behavior. These open primitives do **not** introduce an open project reference to commercial code. Fleet-scale director, governance, and multi-tenant commercial control-plane composition live under `commercial/`.

## Commercial surface

Commercial code covers:

- `Ashlar.Commercial.Fleet.Contracts`
- `Ashlar.Commercial.Fleet.Infrastructure`
- `Ashlar.Commercial.Fleet.Api`
- `Ashlar.Commercial.Fleet.Host`
- `Ashlar.Commercial.MeshDirector`

Commercial concerns include fleet director behavior, fleet-scale mesh orchestration, commercial governance/RBAC, organization-scale control plane, leases/checkpoints, commercial worker execution, commercial API packaging, and vertical commercial app packaging.

## `apps/` classification

The `apps/` directories are agent-set/host **configuration** surfaces with no `.csproj`, so the project-path rule below does not classify them; `LICENSING.md` does, explicitly:

- **Open:** `apps/release-manager/` and `apps/runtime-studio/` — graduated commercial → open on 2026-08-31 (the one-way ratchet's trust-building direction), and scheduled for extraction to their own repositories as the first out-of-tree package consumers.
- The commercial game vertical (`apps/game-director/`, `apps/ashlar-forge/`, and the nine `commercial/` game projects) was removed in the 2026-08-31 native-responsibility slim; it is preserved on `archive/verticals-2026-08-31` for extraction to its own repository.

## Tracked boundary follow-ups

- **Extract the game/spatial residue still inside core projects.** The 2026-08-31 slim removed vertical *directories*; vertical *vocabulary* still compiled into core remains and should move out with the vertical's repo: `src/Ashlar.Core.Application/Environments/**` (MapData/MapVerification/MaterialIntelligence/VoxelChunkKey), `src/Ashlar.Infrastructure/Environments/ModelBackedMaterialIntelligenceService.cs`, `src/Ashlar.Infrastructure/Workflows/QuestPdfWorkflowExporter.cs`, the game-engine rows in `src/Ashlar.Core.Domain` (Export/ExportTarget.cs, Agents/Platform.cs), and the `/api/director/*` "dailies" vocabulary in the open API (generically implemented; rename or extract).

- **Extract the private-license gate from the open API host.** `application/src/Ashlar.API/Security/PrivateLicense*` is the default-off license-enforcement seam commercial deployments enable. It restricts nothing unless configured, and its lapsed floor (expired = read-only) is pinned in code — but per covenant 3 the wall should be architectural, so the gate belongs in the commercial host, behind an open extension seam.
- **Move commercial license files to Ed25519.** License signatures are HMAC-SHA256 today, so the verification key is also a signing key and can never be published. Re-signing licenses with the Ed25519 pattern from `src/Ashlar.Certification.Contracts/` lets the open tooling verify license files too, completing covenant 1's enumeration.

## Namespace and project convention

The enforceable boundary is project/path based, not only namespace based:

- project path `commercial/**` or `<AshlarCommercialProject>true</AshlarCommercialProject>` means commercial;
- project path `src/**` or `application/src/**` means open unless explicitly commercial-marked;
- historical namespaces may remain in commercial projects during migrations, but open projects still must not reference commercial projects.

## Residual open mesh classification

The residual open mesh files were reviewed as open-tier primitives. They are local, file-based, offline-friendly, single-node, lab, or HTTP-client primitives and do not reference `Ashlar.Commercial.*`.

| File | Classification | Rationale |
| --- | --- | --- |
| `src/Ashlar.Core.Application/Mesh/MeshTrustPolicyConfiguration.cs` | Open primitive | Trust-tier configuration model for local/open mesh placement. |
| `src/Ashlar.Core.Application/Mesh/Models/PeerInfo.cs` | Open primitive | Peer identity and endpoint DTO. |
| `src/Ashlar.Core.Application/Mesh/Models/MeshOptions.cs` | Open primitive | Local mesh configuration options. |
| `src/Ashlar.Core.Application/Mesh/Models/InstanceCapabilities.cs` | Open primitive | Capability DTO advertised by a local instance. |
| `src/Ashlar.Core.Application/Mesh/Models/CapabilityDescriptor.cs` | Open primitive | Capability descriptor DTO. |
| `src/Ashlar.Core.Application/Mesh/Models/Artifact.cs` | Open primitive | File/artifact DTO for local negotiation. |
| `src/Ashlar.Core.Application/Mesh/Ports/ILocalTransport.cs` | Open primitive | Local transport port. |
| `src/Ashlar.Core.Application/Mesh/Ports/IInstanceDiscovery.cs` | Open primitive | Instance discovery port. |
| `src/Ashlar.Core.Application/Mesh/Ports/IInstanceCapabilitiesProvider.cs` | Open primitive | Local capability provider port. |
| `src/Ashlar.Core.Application/Mesh/Ports/ICapabilityRequester.cs` | Open primitive | Capability request port. |
| `src/Ashlar.Core.Application/Mesh/Ports/ICapabilityFulfiller.cs` | Open primitive | Capability fulfillment port. |
| `src/Ashlar.Core.Application/Mesh/Ports/ICapabilityAdvertisement.cs` | Open primitive | Capability advertisement port. |
| `src/Ashlar.Core.Application/Mesh/Ports/IArtifactNegotiator.cs` | Open primitive | Artifact negotiation port. |
| `src/Ashlar.Infrastructure/Mesh/ArtifactNegotiator.cs` | Open primitive | File/artifact negotiation implementation. |
| `src/Ashlar.Infrastructure/Mesh/FileBasedCapabilityAdvertisement.cs` | Open primitive | File-based local capability advertisement. |
| `src/Ashlar.Infrastructure/Mesh/FileBasedInstanceDiscovery.cs` | Open primitive | File-based local peer discovery. |
| `src/Ashlar.Infrastructure/Mesh/FileBasedLocalTransport.cs` | Open primitive | File-based local transport. |
| `src/Ashlar.Infrastructure/Mesh/LocalAshlarInstanceCapabilitiesProvider.cs` | Open primitive | Local instance capability provider. |
| `src/Ashlar.Infrastructure/Mesh/MeshCapabilityFulfiller.cs` | Open primitive | Open capability fulfillment primitive. |
| `src/Ashlar.Infrastructure/Mesh/MeshCapabilityRequester.cs` | Open primitive | Open capability request primitive. |
| `src/Ashlar.Infrastructure/Mesh/Sdk/Extensions/MeshServiceCollectionExtensions.cs` | Open primitive | DI registration for open mesh primitives. |
| `src/Ashlar.Infrastructure/MeshLab/MeshLabServiceCollectionExtensions.cs` | Open lab primitive | Open virtual-lab worker wiring; talks to director HTTP API by URL only. |
| `src/Ashlar.Infrastructure/MeshLab/MeshLabTaskStatus.cs` | Open lab primitive | Worker task status DTO for lab execution. |
| `src/Ashlar.Infrastructure/MeshLab/MeshLabWorkerExecutorBackgroundService.cs` | Open lab primitive | Optional background worker for local mesh lab. |
| `src/Ashlar.Infrastructure/MeshLab/MeshLabWorkerExecutorClient.cs` | Open lab primitive | HTTP client for virtual-lab task polling/completion; no commercial project reference. |
| `src/Ashlar.Infrastructure/MeshLab/MeshLabWorkerExecutorOptions.cs` | Open lab primitive | Configuration options for lab worker client. |

## Current verification result

As of this sprint, the dependency-boundary verifier passes with:

```text
dependency-boundary: scanned 72 projects (55 open, 17 commercial, 12 open packable)
dependency-boundary: PASS
```

Related planning history: [`CommercialExtractionPlan.md`](CommercialExtractionPlan.md) and [`DistributionModels.md`](DistributionModels.md).
