# Phase 7 — Edge and headless alignment (MVP)

Phase 7 closes a practical gap from the Phase 0 **capability matrix**: **PC / Mac / Linux headless workers** and automation can participate in the **HTTP mesh director** without hosting **`Nexo.API`** in-process. **Lite / MAUI** remain thin clients; they should call the same director URLs from app code using the same headers.

**Depends on:** mesh director HTTP surface on **`Nexo.API`** (`/api/mesh/*`), mesh security headers (`Nexo:Security:Mesh`), and correlation id propagation where enabled.

## CLI: `nexo mesh director`

Subcommands **`get`**, **`post`**, **`patch`**, **`register`**, **`admit`**, and **`revoke`** perform HTTP calls against a director base URL.

### Environment variables

| Variable | Purpose |
|----------|---------|
| **`NEXO_MESH_DIRECTOR_BASE_URL`** | Director root (e.g. `https://nexo-hub.tailnet:8080`) — used when `--base-url` is omitted |
| **`NEXO_MESH_API_KEY`** | Optional **`X-Nexo-Api-Key`** when the API is configured with built-in API key auth |
| **`NEXO_MESH_MUTATING_TOKEN`** | Optional **`X-Nexo-Mesh-Token`** for mutating **`/api/mesh`** requests (see Phase 2) |
| **`NEXO_MESH_PEER_REGISTRATION_KEY`** | Per-peer fleet registration secret when the director requires **`peerRegistrationKey`** (must differ from operator API key) |

### Examples

```bash
export NEXO_MESH_DIRECTOR_BASE_URL=https://director.example:8080
export NEXO_MESH_MUTATING_TOKEN=your-long-secret

# List fleet nodes (GET is non-mutating for mesh security; token optional)
nexo mesh director get /api/mesh/fleet/nodes --json

# Register this host as a worker (includes peerRegistrationKey when director policy requires it)
export NEXO_MESH_PEER_REGISTRATION_KEY='long-secret-not-the-api-key'
nexo mesh director register worker-01 \
  --api-base-url https://worker-01:8080/ \
  --trust-tier Trusted

# Revoke / admit placement eligibility (Product 5.2 director governance)
nexo mesh director revoke worker-01
nexo mesh director admit worker-01

# Heartbeat with queue depth for elastic placement
nexo mesh director post /api/mesh/fleet/nodes/worker-01/heartbeat --body '{"queueDepth":2}'
```

Use **`--base-url`**, **`--api-key`**, and **`--mesh-token`** to override env for one-off runs. **`--body-file`** accepts a path for larger JSON payloads.

### Exit codes

The CLI exits **0** when the HTTP status is success (2xx), **1** otherwise (network errors or non-2xx).

## Edge / mobile

- **Nexo.Lite / MAUI:** Use your HTTP client of choice against **`{director}/api/mesh/*`**, sending **`X-Nexo-Correlation-Id`** (Phase 3), **`X-Nexo-Mesh-Token`** on mutating mesh calls (Phase 2), and **`X-Nexo-Api-Key`** if the hub requires it.
- **Headless CI:** Same as CLI — script **`nexo mesh director`** or `curl` with the same headers.

## Limitations

- No generated C# client or OpenAPI codegen in this phase — raw paths only.
- **GET** mesh routes still go through mesh security middleware on the server; if your deployment requires a mesh token for reads, extend env usage accordingly (today Phase 2 gates **mutating** mesh verbs).

## Revision history

| Date | Change |
|------|--------|
| 2026-04-22 | Initial Phase 7: `nexo mesh director` HTTP helper + env contract. |
| 2026-05-19 | `register` / `admit` / `revoke` + `NEXO_MESH_PEER_REGISTRATION_KEY`; lab verify via `mesh-lab-verify-director-cli.sh`. |
