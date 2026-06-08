# Phase 8 — Operator hardening (mesh + hub)

This phase bundles **practical follow-ons** after the friend-mesh prefab and edge CLI work:

1. **Discovery admission** — `instances.json` may include **`admitted`** and **`drained`**. When **`NEXO_MESH_TRUST_POLICY=allowlist`**, non-admitted peers are omitted from **`IInstanceDiscovery`** results. **`drained: true`** peers are always omitted.
2. **Trust policy token** — **`allowlist`** is accepted as an alias for **`trusted-only`** in **`PeerTrustPolicyResolver`** (routing and capability requests).
3. **CLI** — **`nexo mesh hub list`** prints peers from discovery; **`nexo mesh hub health --url …`** probes **`GET /health`**. **commercial mesh director CLI** (`dotnet run --project commercial/src/Nexo.Commercial.MeshDirector -- director ...`) (get/post/patch) calls a remote **`Nexo.API`** using **`NEXO_MESH_DIRECTOR_BASE_URL`**, **`NEXO_MESH_API_KEY`**, and optional **`NEXO_MESH_MUTATING_TOKEN`**.
4. **TLS example** — **`docs/config/friend-mesh.Caddyfile.example`** shows host Caddy reverse-proxying to loopback **8080** (see **`docs/FriendMeshPrefab.md`**).

## Tests

- **`Nexo.Tests.Infrastructure`** — `FileBasedInstanceDiscoveryTests`, `NexoPeerBrickExecutorTrustTests`
- **`Nexo.Commercial.Tests.MeshDirector`** — `MeshDirectorCommandUriTests`

## Revision history

| Date | Change |
|------|--------|
| 2026-04-23 | Initial Phase 8 operator hardening doc and wiring. |
