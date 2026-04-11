# Execution Routing (NCR + Peer Network + RunPod)

Nexo generation execution uses a capability-driven router that chooses one of three targets:

- local execution on the current node
- peer execution on another Nexo node in the mesh
- cloud execution through RunPod

The default brick entry point is `generation.capability-routing`.

## Components

- `CapabilityRoutingBrick`: default generation entry point, delegates to resolved target.
- `NcrCapabilityRouter`: resolves local vs remote target using NCR and job requirements.
- `NCRCapabilityPoller`: keeps local capability snapshot current (VRAM, compute class, queue depth).
- `PeerCapabilitySnapshotPoller`: discovers peer Nexo nodes and snapshots peer capabilities.
- `NexoPeerBrickExecutor`: dispatches routed jobs to peer nodes with retry/failover behavior.
- `RunPodBrick`: handles full RunPod lifecycle (spin up, dispatch, poll, pull, teardown).

## Routing Inputs

The router evaluates `JobRequirements`:

- `MinimumVramBytes`
- `ComputeClass`
- `EstimatedDuration`
- `ModelId`
- `IsOvernightOrBackground`
- `RemoteExecutionPreference`

`RemoteExecutionPreference` values:

- `UseSystemDefault`
- `CloudOnly`
- `PreferPeerNetwork`
- `PeerNetworkOnly`

## Decision Rules

1. Explicit remote preference is evaluated first.
2. If local NCR capabilities satisfy VRAM, compute class, and queue threshold, route local.
3. If local constraints are not met, route remote.
4. Remote resolution chooses peer vs cloud according to:
   - job preference (`RemoteExecutionPreference`)
   - system preference (`PreferPeerNetworkOverCloud`)
   - peer routing toggle (`EnablePeerNetworkRouting`)
   - eligible peer availability

Overnight/background jobs force remote execution.

## Peer-to-Peer Robustness

Peer execution includes:

- endpoint normalization and deduplication
- eligibility filtering (VRAM, compute class, queue depth)
- deterministic candidate ranking
- per-peer timeout (`PeerRequestTimeout`)
- sequential failover across eligible peers
- aggregated error diagnostics when all peers fail (`peer-routing.all_peers_failed`)

## Configuration

RunPod and routing options live under `Nexo:RunPod:*`:

- cloud options (`ApiKey`, `BaseUrl`, `PreferredGpuTier`, `Timeout`, `PollingInterval`, `OutputStagingPath`)
- local routing threshold (`QueueDepthThreshold`)
- peer routing options (`EnablePeerNetworkRouting`, `PreferPeerNetworkOverCloud`, `PeerCapabilityId`, `PeerRoutingBrickId`, `PeerRequestTimeout`, `PeerDiscoveryInterval`)

See `docs/Configuration.md` for the full table.

## Validation and Smoke Coverage

Key infrastructure tests:

- `CapabilityRoutingBrickTests`
- `PeerToPeerRoutingSmokeTests`

Stress smoke scenarios include:

- burst concurrency
- intermittent outages
- latency spikes / timeouts
- fallback recovery and success-rate thresholds
