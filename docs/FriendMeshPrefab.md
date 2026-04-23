# Friend mesh prefab (Docker Compose)

This is a **small, opinionated** way to run **Nexo.API** so you and a friend can share a **single hub** over a **private path** (Tailscale, WireGuard, or a TLS reverse proxy). It is **not** a substitute for network access control: you still choose who can reach the TCP port.

## What you get

- **`docker-compose.friend-mesh.yml`** — builds **`Nexo.API`** from **`.docker/Dockerfile.api`** and enables **API key auth** for mutating `/api/*` routes by default.
- **`docs/config/friend-mesh.env.example`** — copy to **`.env.friend-mesh`** (gitignored), fill in secrets, pass to Compose.

## Quick start

```bash
cd /path/to/Nexo
cp docs/config/friend-mesh.env.example .env.friend-mesh
# Edit .env.friend-mesh: set Nexo__Security__ApiKey (and optional mesh tokens if your build supports them).

docker compose -f docker-compose.friend-mesh.yml --env-file .env.friend-mesh up -d --build
```

Health check: `curl -sS http://127.0.0.1:8080/health` (or your published host/port).

Mutating API example (replace key):

```bash
curl -sS -H "X-Nexo-Api-Key: YOUR_KEY" -H "Content-Type: application/json" \
  -d '{"task":"ping"}' \
  http://127.0.0.1:8080/api/copilot/task
```

## Recommended connectivity (in order of preference)

1. **Tailscale** — both of you on the same tailnet; ACLs limit who hits port 8080. See **`docs/TailscaleAndNexo.md`**.
2. **WireGuard** — same idea, manual peers.
3. **Public internet** — only with **TLS** (Caddy, nginx, Traefik, cloud LB) in front of Nexo; never expose plain HTTP to `0.0.0.0` on the raw internet.

Default **`NEXO_FRIEND_MESH_PORT_PUBLISH`** is **`127.0.0.1:8080`** so the container is not wide open until you change it on purpose.

## Workers and mesh director

If your Nexo build exposes **`/api/mesh/*`**, each friend can run a worker and register with the hub using the same auth headers your deployment expects. From a headless machine, **`nexo mesh director`** can call the hub when **`NEXO_MESH_*`** env vars are set — see **`docs/MeshPhase7EdgeAlignment.md`** (if present on your branch).

## Limitations

- **TLS is not inside this compose file** — terminate TLS at a proxy or use a VPN that already encrypts transport.
- **In-memory mesh state** (when enabled) lives in the director process; restarting the container clears fleet/task registries unless you add persistence later.
- **`Nexo.API` feature set** depends on your branch; this prefab only configures **security posture** for the shipped API.

## CI regression gate

GitHub Actions workflow **`.github/workflows/friend-mesh-prefab-gate.yml`** validates **`docker compose … config`**, builds the stack, waits for **`GET /health`**, and checks that **`POST /api/preferences`** returns **401** without **`X-Nexo-Api-Key`** and **200** with the configured key.

## Revision history

| Date | Change |
|------|--------|
| 2026-04-23 | Initial friend-mesh compose + env example + runbook. |
| 2026-04-23 | Add friend-mesh-prefab GitHub Actions gate. |
