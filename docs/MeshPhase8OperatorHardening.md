# Phase 8 — Operator hardening (mesh + hub)

This phase bundles **practical follow-ons** after the friend-mesh prefab and edge CLI work:

1. **Discovery admission** — `instances.json` may include **`admitted`** and **`drained`**. When **`ASHLAR_MESH_TRUST_POLICY=allowlist`**, non-admitted peers are omitted from **`IInstanceDiscovery`** results. **`drained: true`** peers are always omitted.
2. **Trust policy token** — **`allowlist`** is accepted as an alias for **`trusted-only`** in **`PeerTrustPolicyResolver`** (routing and capability requests).
3. **CLI** — **Open:** **`ashlar mesh peers`** lists local `instances.json` peers; **`ashlar mesh health --url …`** probes **`GET /health`**. **Commercial:** **`dotnet run --project commercial/src/Ashlar.Commercial.MeshDirector -- director list-nodes`**, **`director health`**, **`director admit`**, etc. call the fleet director using **`ASHLAR_MESH_DIRECTOR_BASE_URL`**, **`ASHLAR_MESH_API_KEY`**, and optional **`ASHLAR_MESH_MUTATING_TOKEN`**.
4. **TLS example** — **`docs/config/friend-mesh.Caddyfile.example`** shows host Caddy reverse-proxying to loopback **8080** (see **`docs/FriendMeshPrefab.md`**).

## Tests

- **`Ashlar.Tests.Infrastructure`** — `FileBasedInstanceDiscoveryTests`, `AshlarPeerBrickExecutorTrustTests`
- **`Ashlar.Commercial.Tests.MeshDirector`** — `MeshDirectorCommandUriTests`

## Revision history

| Date | Change |
|------|--------|
| 2026-04-23 | Initial Phase 8 operator hardening doc and wiring. |
