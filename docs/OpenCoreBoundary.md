# Open-core boundary

This page is the source of truth for Nexo's open-core split. It describes the boundary as it exists in code, not as an aspiration. Older phase and extraction docs should point here when they discuss open vs. commercial placement.

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
- single-node `Nexo.CLI` and `Nexo.API` shells;
- local/offline mesh primitives: file-based discovery, local transport, capability advertisement, capability requester/fulfiller, artifact negotiation, and virtual mesh-lab worker clients;
- offline/air-gapped operation, trust primitives, policy packs, barrier identity, sanitization, and audit primitives.

`Nexo.API` currently also contains lightweight product scoping primitives such as tenant resolution and organization membership checks for single-node/cloud-shaped API behavior. These open primitives do **not** introduce an open project reference to commercial code. Fleet-scale director, governance, and multi-tenant commercial control-plane composition live under `commercial/`.

## Commercial surface

Commercial code covers:

- `Nexo.Commercial.Fleet.Contracts`
- `Nexo.Commercial.Fleet.Infrastructure`
- `Nexo.Commercial.Fleet.Api`
- `Nexo.Commercial.Fleet.Host`
- `Nexo.Commercial.MeshDirector`
- Game Director / Forge commercial verticals under `commercial/src/Nexo.Commercial.GameDirector.*`
- `Nexo.Commercial.GameDomain`

Commercial concerns include fleet director behavior, fleet-scale mesh orchestration, commercial governance/RBAC, organization-scale control plane, leases/checkpoints, commercial worker execution, commercial API packaging, and vertical commercial app packaging.

## Namespace and project convention

The enforceable boundary is project/path based, not only namespace based:

- project path `commercial/**` or `<NexoCommercialProject>true</NexoCommercialProject>` means commercial;
- project path `src/**` or `application/src/**` means open unless explicitly commercial-marked;
- historical namespaces may remain in commercial projects during migrations, but open projects still must not reference commercial projects.

## Residual open mesh classification

The residual open mesh files were reviewed as open-tier primitives. They are local, file-based, offline-friendly, single-node, lab, or HTTP-client primitives and do not reference `Nexo.Commercial.*`.

| File | Classification | Rationale |
| --- | --- | --- |
| `src/Nexo.Core.Application/Mesh/MeshTrustPolicyConfiguration.cs` | Open primitive | Trust-tier configuration model for local/open mesh placement. |
| `src/Nexo.Core.Application/Mesh/Models/PeerInfo.cs` | Open primitive | Peer identity and endpoint DTO. |
| `src/Nexo.Core.Application/Mesh/Models/MeshOptions.cs` | Open primitive | Local mesh configuration options. |
| `src/Nexo.Core.Application/Mesh/Models/InstanceCapabilities.cs` | Open primitive | Capability DTO advertised by a local instance. |
| `src/Nexo.Core.Application/Mesh/Models/CapabilityDescriptor.cs` | Open primitive | Capability descriptor DTO. |
| `src/Nexo.Core.Application/Mesh/Models/Artifact.cs` | Open primitive | File/artifact DTO for local negotiation. |
| `src/Nexo.Core.Application/Mesh/Ports/ILocalTransport.cs` | Open primitive | Local transport port. |
| `src/Nexo.Core.Application/Mesh/Ports/IInstanceDiscovery.cs` | Open primitive | Instance discovery port. |
| `src/Nexo.Core.Application/Mesh/Ports/IInstanceCapabilitiesProvider.cs` | Open primitive | Local capability provider port. |
| `src/Nexo.Core.Application/Mesh/Ports/ICapabilityRequester.cs` | Open primitive | Capability request port. |
| `src/Nexo.Core.Application/Mesh/Ports/ICapabilityFulfiller.cs` | Open primitive | Capability fulfillment port. |
| `src/Nexo.Core.Application/Mesh/Ports/ICapabilityAdvertisement.cs` | Open primitive | Capability advertisement port. |
| `src/Nexo.Core.Application/Mesh/Ports/IArtifactNegotiator.cs` | Open primitive | Artifact negotiation port. |
| `src/Nexo.Infrastructure/Mesh/ArtifactNegotiator.cs` | Open primitive | File/artifact negotiation implementation. |
| `src/Nexo.Infrastructure/Mesh/FileBasedCapabilityAdvertisement.cs` | Open primitive | File-based local capability advertisement. |
| `src/Nexo.Infrastructure/Mesh/FileBasedInstanceDiscovery.cs` | Open primitive | File-based local peer discovery. |
| `src/Nexo.Infrastructure/Mesh/FileBasedLocalTransport.cs` | Open primitive | File-based local transport. |
| `src/Nexo.Infrastructure/Mesh/LocalNexoInstanceCapabilitiesProvider.cs` | Open primitive | Local instance capability provider. |
| `src/Nexo.Infrastructure/Mesh/MeshCapabilityFulfiller.cs` | Open primitive | Open capability fulfillment primitive. |
| `src/Nexo.Infrastructure/Mesh/MeshCapabilityRequester.cs` | Open primitive | Open capability request primitive. |
| `src/Nexo.Infrastructure/Mesh/Sdk/Extensions/MeshServiceCollectionExtensions.cs` | Open primitive | DI registration for open mesh primitives. |
| `src/Nexo.Infrastructure/MeshLab/MeshLabServiceCollectionExtensions.cs` | Open lab primitive | Open virtual-lab worker wiring; talks to director HTTP API by URL only. |
| `src/Nexo.Infrastructure/MeshLab/MeshLabTaskStatus.cs` | Open lab primitive | Worker task status DTO for lab execution. |
| `src/Nexo.Infrastructure/MeshLab/MeshLabWorkerExecutorBackgroundService.cs` | Open lab primitive | Optional background worker for local mesh lab. |
| `src/Nexo.Infrastructure/MeshLab/MeshLabWorkerExecutorClient.cs` | Open lab primitive | HTTP client for virtual-lab task polling/completion; no commercial project reference. |
| `src/Nexo.Infrastructure/MeshLab/MeshLabWorkerExecutorOptions.cs` | Open lab primitive | Configuration options for lab worker client. |

## Current verification result

As of this sprint, the dependency-boundary verifier passes with:

```text
dependency-boundary: scanned 72 projects (55 open, 17 commercial, 12 open packable)
dependency-boundary: PASS
```

Related planning history: [`CommercialExtractionPlan.md`](CommercialExtractionPlan.md) and [`DistributionModels.md`](DistributionModels.md).
