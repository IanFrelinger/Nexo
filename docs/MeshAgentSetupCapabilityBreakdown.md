# Mesh agent setup — capability breakdown (“tear sheet”)

This document **decomposes** everything that must exist—product, engineering, and operations—to run **agents (or any workload) across Ashlar mesh peers** today and when you grow to WAN. It maps to **ports in `Ashlar.Core.Application.Mesh`** and **infrastructure in `Ashlar.Infrastructure.Mesh`**.

---

## Tier 0 — Must exist before “mesh” means anything

| Capability | What it is | Today in Ashlar |
|------------|------------|----------------|
| **Stable peer identity** | Each runtime instance has a durable `peerId` for signing requests, inbox routing, and audit. | `ASHLAR_MESH_PEER_ID`; default random GUID in DI—**set explicitly** per agent/host. |
| **Instance registry** | Authoritative list of peers, endpoints, capabilities, trust, admission. | `instances.json` (`ASHLAR_MESH_INSTANCES_PATH` or `~/.ashlar/instances.json`). |
| **Shared transport plane** | Peers can exchange request/response bytes. | **File-based inboxes** under `~/.ashlar/mesh/` (same host or **shared filesystem** / synced folder). **No native WAN transport** in default stack. |

Without Tier 0, discovery and advertisement are inert.

---

## Tier 1 — Discovery, advertisement, negotiation

| Capability | What it is | Today in Ashlar |
|------------|------------|----------------|
| **Discovery** | Enumerate peers from registry with effective trust tier. | `IInstanceDiscovery` → `FileBasedInstanceDiscovery`; CLI `ashlar mesh --discover`. |
| **Advertisement** | Publish which capabilities this peer offers. | `ICapabilityAdvertisement` → `FileBasedCapabilityAdvertisement`; CLI `ashlar mesh --advertise` (CLI uses sample descriptors; apps register their own). |
| **Capability lookup** | Find peers that expose a given capability id. | `FindPeersWithCapabilityAsync`; CLI `ashlar mesh --capability <id>`. |
| **Artifact negotiation** | Agree format/handling for returned payloads. | `IArtifactNegotiator`; `ArtifactFormat` on request/fulfill path. |

---

## Tier 2 — Trust, admission, policy

| Capability | What it is | Today in Ashlar |
|------------|------------|----------------|
| **Trust tiers** | Label peers (trusted / untrusted / unknown) for routing decisions. | JSON `trustTier`; CLI `ashlar mesh --set-trust-tier peerId:tier`. |
| **Mesh-wide trust policy** | Open vs allowlist vs denylist for discovery and for requests. | `ASHLAR_MESH_TRUST_POLICY`, `ASHLAR_PEER_TRUST_POLICY`; `ASHLAR_TRUSTED_PEER_IDS` / `ASHLAR_UNTRUSTED_PEER_IDS`. |
| **Admission gate** | Explicit allow before a peer is treated as part of the mesh (operational control). | `admitted` in `instances.json`; `ashlar mesh admit` / `revoke`. |

Agents in a hostile or multi-tenant world **fail closed** here: wrong tier → requester filters peers out (`MeshCapabilityRequester` uses `PeerTrustPolicyResolver`).

---

## Tier 3 — Request / fulfill execution loop (the “agent” path)

| Capability | What it is | Today in Ashlar |
|------------|------------|----------------|
| **Connect transport** | Open inbox / session before send/receive. | `ILocalTransport.ConnectAsync` (file transport creates inbox dir). |
| **Send request** | Serialized request (capability, format, requestId, requesterId) to chosen peer inbox. | `MeshCapabilityRequester.RequestAsync` → `SendAsync`. |
| **Fulfill loop** | Peer polls/processes inbound messages and runs a **handler** for a capability id. | `ICapabilityFulfiller` → `MeshCapabilityFulfiller.ProcessOneAsync`; handlers registered in code (`RegisterHandler` in tests / your host). |
| **Respond** | Write response bytes back so requester’s `ReceiveAsync` can correlate by `requestId`. | Fulfiller writes response; requester polls with timeout (`~3s` default in requester). |

**“Agent”** in mesh terms = **a fulfiller process** (or thread) running `ProcessOneAsync` (or continuous loop) **plus** whatever your handler does (call LLM, run pipeline, etc.). There is **no** separate global “agent scheduler” in the mesh layer—it is **pull-based** message handling on top of transport.

---

## Tier 4 — Data movement beyond one-shot artifacts

| Capability | What it is | Today in Ashlar |
|------------|------------|----------------|
| **Shared adaptations** | Propagate validated components across peers. | `ASHLAR_SHARED_ADAPTATIONS_PATH`; `ashlar mesh sync` (pull + validate + adopt). |
| **Disconnected transfer** | Move packages without live mesh connectivity. | `ashlar mesh export` / `import` (`.nxpkg`, sneakernet). |

Useful for **air-gapped** or **high-latency** meshes.

---

## Tier 5 — Operations and hardening (what you add for production)

| Capability | What it is | Status |
|------------|------------|--------|
| **WAN transport** | mTLS / QUIC / message bus instead of shared directory. | **You implement** another `ILocalTransport` (or bridge file drop to S3/NATS). |
| **Discovery beyond static JSON** | DNS, K8s endpoints, gossip, control plane API. | **You extend** `IInstanceDiscovery` or generate `instances.json` from your orchestrator. |
| **Secrets & identity** | mTLS peer certs, SPIFFE, API keys per peer—not just peerId strings. | **You layer** on transport and registry generation. |
| **Observability** | Metrics for queue depth, fulfill latency, trust denials. | **Your** logging/metrics around handlers and transport. |
| **Backpressure & DLQ** | When fulfiller overloaded or malicious traffic. | **Your** policy; defaults are simple timeouts and polling. |

---

## Minimal “two agents on one machine” checklist

1. **One** `instances.json` path both processes use (`ASHLAR_MESH_INSTANCES_PATH`).  
2. **Two** distinct `ASHLAR_MESH_PEER_ID` values (e.g. `peer-fulfiller`, `peer-requester`).  
3. **Both** peers listed in `instances.json` with endpoints (can be placeholders for file transport) and `admitted: true` after review.  
4. **Fulfiller** side: `AddMeshInfrastructure`, `AdvertiseAsync` for capability ids your agents implement, `RegisterHandler`, background loop calling `ProcessOneAsync`.  
5. **Requester** side: `AddMeshInfrastructure`, `RequestAsync(capabilityId, format)`.  
6. **Trust policy** set to match your test (`open` or allowlist both peer ids).

Reference implementation: `DogfoodBlock9LocalIpcTests` in `src/Ashlar.Tests.Infrastructure/Tests/Dogfood/DogfoodBlock9LocalIpcTests.cs`.

---

## CLI quick reference

| Command | Role |
|---------|------|
| `ashlar mesh --discover` | List peers from registry |
| `ashlar mesh --advertise` | Publish sample capabilities (CLI demo) |
| `ashlar mesh --capability <id>` | Find peers advertising capability |
| `ashlar mesh capabilities` | Show **this** host’s capability summary |
| `ashlar mesh sync` | Pull shared adaptations (needs `ASHLAR_SHARED_ADAPTATIONS_PATH` etc.) |
| `ashlar mesh export` / `import` | Sneakernet packages |
| `ashlar mesh admit` / `revoke` | Toggle `admitted` |
| `ashlar dogfood block9` | Automated gate: mesh discover/advertise |

---

## Related configuration

See **Mesh** section in [`Configuration.md`](./Configuration.md) (`ASHLAR_MESH_*`, trust env vars, `ASHLAR_SHARED_ADAPTATIONS_PATH`).

---

## Summary diagram (logical layers)

```
[Tier 5: WAN / bus / certs / ops]     ← you add for decentralized fleet
        ↓
[Tier 4: sync / export-import]      ← ashlar mesh sync, export, import
        ↓
[Tier 3: request ↔ fulfill loop]    ← ICapabilityRequester / Fulfiller + handlers = "agents"
        ↓
[Tier 2: trust + admit]             ← policy env vars, admit/revoke
        ↓
[Tier 1: discover + advertise]      ← instances.json + advertisement
        ↓
[Tier 0: peerId + registry + transport] ← file inboxes or your transport
```

If you meant **tier prioritization for a roadmap**: build **Tier 0–3** for one use case, then **Tier 4** if adaptations matter, then **Tier 5** for real multi-site agents.
