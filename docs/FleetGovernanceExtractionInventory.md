# Fleet and mesh governance extraction inventory

This inventory is the next step after the open-core licensing boundary and vertical extraction work. It classifies fleet, mesh, and networking code before moving it so the commercial boundary stays reviewable.

Classification terms:

- **OPEN primitive** — single-node, inspectable, local-first, or trust primitive. May stay Apache-2.0.
- **COMMERCIAL fleet/governance** — multi-node control plane, fleet scheduling, director persistence, leases/checkpoints, knowledge replication, worker execution, or org-scale governance. Should move to commercial modules before commercial licensing is applied.
- **SPLIT / owner decision** — a file mixes open primitive behavior with fleet-scale behavior or needs an owner call on packaging.

## Summary recommendation

1. Keep low-level mesh primitives open: peer descriptors, local instance discovery/advertisement, artifact negotiation, local transport, and trust configuration primitives.
2. Extract all `Fleet` projects/files into commercial contracts/core/infrastructure modules.
3. Extract `Networking` knowledge-sync / network-bus / adaptive-cache surfaces into commercial fleet/governance modules unless the owner explicitly wants a smaller open networking substrate.
4. Split CLI/API surfaces so open CLI/API retain local mesh inspection and trust middleware, while `mesh director`, fleet registration/admission, and director operations move to commercial modules.
5. Move fleet/mesh governance tests with the commercial code they validate.

## Proposed commercial modules

| Module | Purpose |
|--------|---------|
| `commercial/src/Nexo.Commercial.Fleet.Contracts` | Fleet DTOs and ports that commercial infrastructure and control-plane hosts consume. |
| `commercial/src/Nexo.Commercial.Fleet.Core` | Task placement, fleet trust policy, elastic scheduling, lease/checkpoint policy, knowledge replication orchestration. |
| `commercial/src/Nexo.Commercial.Fleet.Infrastructure` | LiteDB registries, worker executor client/background service, director persistence, import/export implementations, sweep/rebalance services. |
| `commercial/src/Nexo.Commercial.Fleet.Api` | Commercial `/api/mesh` fleet/task/knowledge endpoint extension seeded from open `Nexo.API`. |
| `commercial/src/Nexo.Commercial.Fleet.Host` | Commercial operator host wiring `AddNexoCommercialFleetDirector()` and `MapCommercialFleetEndpoints()`. |
| `commercial/src/Nexo.Commercial.MeshDirector` | Mesh director CLI/API/control-plane operations and HTTP client surfaces. |
| `commercial/src/Nexo.Commercial.Governance` | Future RBAC/SSO, centralized policy management, aggregate tamper-evident audit, org-scale entitlements. |
| `commercial/tests/Nexo.Commercial.Tests.Fleet` | Fleet/director/governance tests moved out of open test projects. |

## OPEN primitive inventory

These files should remain open unless a later extraction finds unavoidable fleet-only semantics.

### Core mesh contracts and models

| Path | Reason |
|------|--------|
| `src/Nexo.Core.Application/Mesh/Models/Artifact.cs` | Portable artifact descriptor for local/import/export flows. |
| `src/Nexo.Core.Application/Mesh/Models/CapabilityDescriptor.cs` | Inspectable capability metadata used by local nodes and SDK consumers. |
| `src/Nexo.Core.Application/Mesh/Models/InstanceCapabilities.cs` | Local instance capability snapshot; useful without fleet control plane. |
| `src/Nexo.Core.Application/Mesh/Models/MeshOptions.cs` | Local mesh configuration primitive. |
| `src/Nexo.Core.Application/Mesh/Models/PeerInfo.cs` | Peer descriptor used by discovery and local operator inspection. |
| `src/Nexo.Core.Application/Mesh/MeshTrustPolicyConfiguration.cs` | Trust primitive; trust rules should remain inspectable/open. |
| `src/Nexo.Core.Application/Mesh/Ports/IArtifactNegotiator.cs` | Local artifact negotiation primitive. |
| `src/Nexo.Core.Application/Mesh/Ports/ICapabilityAdvertisement.cs` | Local capability advertisement primitive. |
| `src/Nexo.Core.Application/Mesh/Ports/ICapabilityFulfiller.cs` | Local capability fulfillment abstraction. |
| `src/Nexo.Core.Application/Mesh/Ports/ICapabilityRequester.cs` | Request-side abstraction for open mesh primitives. |
| `src/Nexo.Core.Application/Mesh/Ports/IInstanceCapabilitiesProvider.cs` | Local capability provider. |
| `src/Nexo.Core.Application/Mesh/Ports/IInstanceDiscovery.cs` | Local/discoverable peer primitive. |
| `src/Nexo.Core.Application/Mesh/Ports/ILocalTransport.cs` | Local transport primitive. |

### Infrastructure mesh primitives

| Path | Reason |
|------|--------|
| `src/Nexo.Infrastructure/Mesh/ArtifactNegotiator.cs` | Implements open artifact negotiation. |
| `src/Nexo.Infrastructure/Mesh/FileBasedCapabilityAdvertisement.cs` | Local file-based advertisement; inspectable and useful for single-node labs. |
| `src/Nexo.Infrastructure/Mesh/FileBasedInstanceDiscovery.cs` | File-based discovery primitive. |
| `src/Nexo.Infrastructure/Mesh/FileBasedLocalTransport.cs` | Local transport primitive. |
| `src/Nexo.Infrastructure/Mesh/LocalNexoInstanceCapabilitiesProvider.cs` | Local capability snapshot provider. |
| `src/Nexo.Infrastructure/Mesh/MeshCapabilityFulfiller.cs` | Capability fulfillment primitive. |
| `src/Nexo.Infrastructure/Mesh/MeshCapabilityRequester.cs` | Capability requester primitive. |
| `src/Nexo.Infrastructure/Mesh/Sdk/Extensions/MeshServiceCollectionExtensions.cs` | Open DI entrypoint for primitive mesh services. |

### Open CLI/API mesh and trust surfaces

| Path | Reason |
|------|--------|
| `application/src/Nexo.CLI/Commands/MeshCommand.cs` | Keep local `discover`, `advertise`, `capabilities`, `sync`, import/export, and local trust-tier edits open. Split only if future owner decision makes `sync` a commercial fleet feature. |
| `application/src/Nexo.API/Security/MeshCorrelationApplicationBuilderExtensions.cs` | Correlation ID middleware is an audit/trust primitive. |
| `application/src/Nexo.API/Security/MeshCorrelationMiddleware.cs` | Correlation ID middleware is an audit/trust primitive. |
| `application/src/Nexo.API/Security/MeshSecurityApplicationBuilderExtensions.cs` | Token/body/rate-limit middleware registration should remain inspectable. |
| `application/src/Nexo.API/Security/MeshSecurityMiddleware.cs` | Mesh/brick-execute protection is a trust primitive. |
| `application/src/Nexo.API/Security/MeshSecurityOptions.cs` | Open security configuration options. |

### Open tests that should remain open

| Path | Reason |
|------|--------|
| `src/Nexo.Tests.Infrastructure/Tests/Mesh/ArtifactNegotiatorTests.cs` | Tests open artifact negotiation primitive. |
| `src/Nexo.Tests.Infrastructure/Tests/Mesh/FileBasedCapabilityAdvertisementTests.cs` | Tests open local advertisement primitive. |
| `src/Nexo.Tests.Infrastructure/Tests/Mesh/FileBasedInstanceDiscoveryTests.cs` | Tests open local discovery primitive. |
| `application/src/Nexo.Tests.CLI/Tests/Commands/MeshCommandTests.cs` | Tests open CLI mesh primitive behavior. |

## COMMERCIAL fleet/governance inventory

These files should move to commercial modules.

### Fleet contracts and models

Target module: `commercial/src/Nexo.Commercial.Fleet.Contracts`.

| Path | Target / reason |
|------|-----------------|
| `src/Nexo.Core.Application/Fleet/Models/MeshFleetNodeState.cs` | Fleet node lifecycle model. |
| `src/Nexo.Core.Application/Fleet/Models/MeshFleetTrustTier.cs` | Fleet placement trust tier, not primitive trust policy. |
| `src/Nexo.Core.Application/Fleet/Models/MeshKnowledgeExportPayload.cs` | Fleet knowledge replication payload. |
| `src/Nexo.Core.Application/Fleet/Models/MeshTaskCreateSpec.cs` | Fleet task creation DTO. |
| `src/Nexo.Core.Application/Fleet/Models/MeshTaskState.cs` | Fleet task state DTO. |
| `src/Nexo.Core.Application/Fleet/Models/MeshTaskStatus.cs` | Fleet task lifecycle enum. |
| `src/Nexo.Core.Application/Fleet/Ports/IFleetNodeRegistry.cs` | Fleet registry port. |
| `src/Nexo.Core.Application/Fleet/Ports/IMeshTaskPlacementService.cs` | Commercial placement port. |
| `src/Nexo.Core.Application/Fleet/Ports/IMeshTaskRegistry.cs` | Fleet task registry port. |

### Fleet infrastructure and director persistence

Target module: `commercial/src/Nexo.Commercial.Fleet.Infrastructure`.

| Path | Target / reason |
|------|-----------------|
| `src/Nexo.Infrastructure/Fleet/FleetServiceCollectionExtensions.cs` | Commercial DI entrypoint. |
| `src/Nexo.Infrastructure/Fleet/InMemoryFleetNodeRegistry.cs` | Fleet registry implementation. |
| `src/Nexo.Infrastructure/Fleet/InMemoryMeshTaskRegistry.cs` | Fleet task registry implementation. |
| `src/Nexo.Infrastructure/Fleet/LiteDbFleetNodeRegistry.cs` | Director/fleet persistence. |
| `src/Nexo.Infrastructure/Fleet/LiteDbMeshDirectorDocuments.cs` | Director persistence documents. |
| `src/Nexo.Infrastructure/Fleet/LiteDbMeshTaskRegistry.cs` | Fleet task persistence. |
| `src/Nexo.Infrastructure/Fleet/MeshCheckpointOptions.cs` | Lease/checkpoint policy. |
| `src/Nexo.Infrastructure/Fleet/MeshElasticSchedulingOptions.cs` | Commercial elastic scheduling. |
| `src/Nexo.Infrastructure/Fleet/MeshFleetRegistrationKeys.cs` | Fleet registration key management. |
| `src/Nexo.Infrastructure/Fleet/MeshFleetRegistrationOptions.cs` | Fleet registration configuration. |
| `src/Nexo.Infrastructure/Fleet/MeshFleetTrustPolicy.cs` | Fleet trust-tier placement policy. |
| `src/Nexo.Infrastructure/Fleet/MeshKnowledgeExportService.cs` | Fleet knowledge export. |
| `src/Nexo.Infrastructure/Fleet/MeshKnowledgeImportService.cs` | Fleet knowledge import. |
| `src/Nexo.Infrastructure/Fleet/MeshLeaseSweepBackgroundService.cs` | Lease management. |
| `src/Nexo.Infrastructure/Fleet/MeshPeerKnowledgePullBackgroundService.cs` | Peer knowledge replication. |
| `src/Nexo.Infrastructure/Fleet/MeshPeerKnowledgeSyncOptions.cs` | Peer knowledge replication options. |
| `src/Nexo.Infrastructure/Fleet/MeshPendingTaskRebalancerBackgroundService.cs` | Commercial task rebalancing. |
| `src/Nexo.Infrastructure/Fleet/MeshPersistenceOptions.cs` | Fleet persistence options. |
| `src/Nexo.Infrastructure/Fleet/MeshPlacementTrustOptions.cs` | Fleet placement trust options. |
| `src/Nexo.Infrastructure/Fleet/MeshTaskExecutionService.cs` | Fleet task execution. |
| `src/Nexo.Infrastructure/Fleet/MeshTaskPlacementService.cs` | Fleet task placement implementation. |
| `src/Nexo.Infrastructure/Fleet/MeshLab/MeshLabWorkerExecutorBackgroundService.cs` | Fleet worker loop for lab/operator use. |
| `src/Nexo.Infrastructure/Fleet/MeshLab/MeshLabWorkerExecutorClient.cs` | Fleet worker executor client. |
| `src/Nexo.Infrastructure/Fleet/MeshLab/MeshLabWorkerExecutorOptions.cs` | Fleet worker executor configuration. |

### Networking / knowledge sync

Target module: `commercial/src/Nexo.Commercial.Fleet.Contracts` for ports/models and `commercial/src/Nexo.Commercial.Fleet.Infrastructure` for HTTP/in-memory implementations unless the owner defines a smaller open networking primitive.

| Path | Target / reason |
|------|-----------------|
| `src/Nexo.Core.Application/Networking/Models/AdaptiveBrickCacheStats.cs` | Fleet/adaptive cache metric. |
| `src/Nexo.Core.Application/Networking/Models/BrickUsageRecord.cs` | Cross-node usage tracking. |
| `src/Nexo.Core.Application/Networking/Models/BrickUsageStats.cs` | Cross-node usage stats. |
| `src/Nexo.Core.Application/Networking/Models/KnowledgeChunk.cs` | Knowledge replication payload. |
| `src/Nexo.Core.Application/Networking/Models/KnowledgeSyncStatus.cs` | Knowledge replication status. |
| `src/Nexo.Core.Application/Networking/Models/NetworkAgentEntry.cs` | Network agent directory entry. |
| `src/Nexo.Core.Application/Networking/Models/NetworkEvent.cs` | Network bus event. |
| `src/Nexo.Core.Application/Networking/Models/NetworkEventTypes.cs` | Network bus event types. |
| `src/Nexo.Core.Application/Networking/Models/PlasticityMetrics.cs` | Adaptive/fleet plasticity metric. |
| `src/Nexo.Core.Application/Networking/Ports/IAdaptiveBrickCache.cs` | Cross-node adaptive cache. |
| `src/Nexo.Core.Application/Networking/Ports/IBrickUsageTracker.cs` | Cross-node usage tracking. |
| `src/Nexo.Core.Application/Networking/Ports/IKnowledgeChunkStore.cs` | Knowledge replication store. |
| `src/Nexo.Core.Application/Networking/Ports/IKnowledgeSyncService.cs` | Knowledge sync service. |
| `src/Nexo.Core.Application/Networking/Ports/INetworkAgentDirectory.cs` | Network/fleet directory. |
| `src/Nexo.Core.Application/Networking/Ports/INetworkBus.cs` | Network bus. |
| `src/Nexo.Core.Application/Networking/Ports/INetworkNegotiationService.cs` | Cross-node negotiation. |
| `src/Nexo.Core.Application/Networking/Ports/IPlasticityService.cs` | Adaptive plasticity service. |
| `src/Nexo.Infrastructure/Networking/HttpKnowledgeSyncService.cs` | HTTP knowledge sync implementation. |
| `src/Nexo.Infrastructure/Networking/HttpNetworkAgentDirectory.cs` | HTTP network directory implementation. |
| `src/Nexo.Infrastructure/Networking/HttpNetworkBus.cs` | HTTP network bus implementation. |
| `src/Nexo.Infrastructure/Networking/InMemoryKnowledgeChunkStore.cs` | Knowledge chunk store implementation. |
| `src/Nexo.Infrastructure/Networking/KnowledgeSyncServiceOptions.cs` | Commercial knowledge sync options. |
| `src/Nexo.Infrastructure/Networking/NetworkAgentDirectoryOptions.cs` | Commercial network directory options. |
| `src/Nexo.Infrastructure/Networking/NetworkBusOptions.cs` | Commercial network bus options. |
| `src/Nexo.Infrastructure/Networking/NetworkNegotiationService.cs` | Cross-node negotiation implementation. |
| `src/Nexo.Infrastructure/Networking/PlasticityOptions.cs` | Commercial plasticity options. |
| `src/Nexo.Infrastructure/Networking/PlasticityService.cs` | Adaptive plasticity implementation. |

### CLI/API control plane surfaces

| Path | Classification |
|------|----------------|
| `commercial/src/Nexo.Commercial.MeshDirector/MeshDirectorCommand.cs` | **COMMERCIAL** — direct client for fleet director API. The open CLI duplicate has been removed after mesh-lab scripts/operator packaging moved to the commercial module. |
| `application/src/Nexo.CLI/Commands/MeshHubCommand.cs` | **SPLIT / owner decision** — `health` can remain open as generic remote health probe; `list` over admitted peers and hub/fleet semantics should move commercial or be renamed as local-only inspection. |

### Fleet tests to move commercial

Target module: `commercial/tests/Nexo.Commercial.Tests.Fleet`.

| Path group | Target / reason |
|------------|-----------------|
| `src/Nexo.Tests.Infrastructure/Tests/Fleet/*.cs` | Move all fleet registry, placement, persistence, lease/checkpoint, knowledge replication, worker executor, and trust policy tests with fleet code. |
| `commercial/tests/Nexo.Commercial.Tests.MeshDirector/MeshDirectorCommandUriTests.cs` | **COMMERCIAL** — URI-building tests for the commercial mesh director client. The open CLI duplicate has been removed. |

## SPLIT / owner decision inventory

| Path | Decision needed |
|------|-----------------|
| `application/src/Nexo.CLI/Commands/MeshCommand.cs` | Keep local discovery/advertise/capabilities/import/export open. Decide whether `sync`, `admit`, `revoke`, and `--set-trust-tier` remain open local trust tools or become commercial fleet operations. |
| `application/src/Nexo.CLI/Commands/MeshHubCommand.cs` | Split generic health probe from hub/fleet peer-listing behavior, or move whole command commercial. |
| `src/Nexo.Tests.Infrastructure/Tests/Mesh/MeshLabDockerFixture.cs`, `MeshLabDockerEnv.cs`, `MeshLabDockerE2ETests.cs` | Virtual lab may remain open if it validates primitive two-node behavior; move commercial if it validates fleet worker/director placement. |
| `src/Nexo.Tests.Infrastructure/Tests/Networking/NetworkBusOptionsTests.cs` | Options-only test can remain open only if owner keeps a minimal open network bus. Otherwise move with commercial networking. |
| `src/Nexo.Tests.Infrastructure/Tests/Networking/InfrastructureNetworkingGapCoverageTests.cs` | Move if networking is commercial; split if any low-level open networking primitive remains. |

## Updated extraction sequence

### PR 4 — fleet inventory split

This document. No code moves.

### PR 5 — commercial fleet contracts/core

Create the commercial contracts module and seed it with copied fleet contract DTOs/ports while open consumers are still being migrated:

- `commercial/src/Nexo.Commercial.Fleet.Contracts`
- Seed from `src/Nexo.Core.Application/Fleet/**`
- Keep the original open files temporarily until commercial infrastructure/API consumers move in later PRs.

Then move:

- commercial-classified `src/Nexo.Core.Application/Networking/**` ports/models, if owner confirms those are not part of a smaller open networking substrate.

### PR 6 — commercial fleet infrastructure

Create the commercial fleet infrastructure module and seed it with copied fleet implementation code while open consumers are still being migrated:

- `commercial/src/Nexo.Commercial.Fleet.Infrastructure`
- Seed from `src/Nexo.Infrastructure/Fleet/**`
- Keep the original open files temporarily until commercial API/CLI consumers move in later PRs.

Then move:

- commercial-classified `src/Nexo.Infrastructure/Networking/**`
- fleet tests from `src/Nexo.Tests.Infrastructure/Tests/Fleet/**`

Create:

- `commercial/src/Nexo.Commercial.Fleet.Infrastructure`
- `commercial/tests/Nexo.Commercial.Tests.Fleet`

### PR 7 — commercial mesh director / CLI surface

Seed commercial module and then move or split:

- `MeshDirectorCommand` (commercial module owns this now)
- commercial portions of `MeshHubCommand`
- related URI tests (commercial module owns these now)

Create:

- `commercial/src/Nexo.Commercial.MeshDirector`

### PR 8 — commercial fleet API baseline

Seed commercial endpoint module:

- `commercial/src/Nexo.Commercial.Fleet.Api`
- copy `/api/mesh/fleet/**`, `/api/mesh/tasks/**`, and `/api/mesh/knowledge/**` endpoint mappings/handlers/DTOs from open `Nexo.API`;
- use commercial fleet contracts/infrastructure namespaces.

### PR 9 — commercial fleet host wiring (done)

Added:

- `commercial/src/Nexo.Commercial.Fleet.Host` — operator host registering `AddNexoCommercialFleetDirector()` and mapping `MapCommercialFleetEndpoints()`;
- `commercial/src/Nexo.Commercial.Fleet.Api/CommercialFleetHostExtensions.cs` — shared DI/endpoint wiring helper;
- `commercial/tests/Nexo.Commercial.Tests.Fleet.Host` and `scripts/commercial-fleet-host-smoke.sh` — host build/smoke validation;
### PR 10 — open fleet endpoint cleanup (done)

Completed:

- removed open `/api/mesh/fleet/**`, `/api/mesh/tasks/**`, and `/api/mesh/knowledge/**` endpoint handlers from `Nexo.API`;
- migrated mesh-lab **peer-a** to `.docker/Dockerfile.fleet-host` (`Nexo.Commercial.Fleet.Host`);
- mesh-lab verify scripts unchanged (same HTTP paths; director is now the commercial host).

### PR 11 — open fleet infrastructure cleanup (done)

Completed:

- removed `src/Nexo.Core.Application/Fleet/**` and `src/Nexo.Infrastructure/Fleet/**`;
- extracted mesh-lab worker executor to `src/Nexo.Infrastructure/MeshLab/**` (open HTTP client only);
- moved fleet unit tests to `commercial/tests/Nexo.Commercial.Tests.Fleet`;
- kept mesh-lab worker executor tests under `src/Nexo.Tests.Infrastructure/Tests/MeshLab/**`.

### PR 12 — dependency-boundary scanner

Add a script that:

- classifies open vs commercial projects;
- fails on open project references to commercial projects;
- checks commercial directories include `COMMERCIAL-LICENSE.md`;
- verifies open packable projects resolve `PackageLicenseExpression=Apache-2.0`.

Wire to CI only after owner approval.

## Validation for this inventory PR

- Markdown link check.
- Docs lint.
- No source or workflow changes.
