# Phase 2 — Uniform transport and auth for the mesh

Phase 2 tightens the **HTTP surface** used by the federated mesh program: **`/api/mesh`** (Phase 1 director) and **`POST /api/bricks/{id}/execute`** (remote brick delegation). It does **not** replace TLS or network ACLs; operators still terminate TLS at a reverse proxy or use Tailscale (see [TailscaleAndAshlar.md](TailscaleAndAshlar.md)).

**Depends on:** [MeshPhase0NorthStar.md](MeshPhase0NorthStar.md), [MeshPhase1ControlPlane.md](MeshPhase1ControlPlane.md).

## What is implemented

| Control | Implementation |
|---------|------------------|
| **Service tokens** | Optional shared secrets in **`MeshSecurityMiddleware`**: mutating **`/api/mesh`** requires **`Ashlar:Security:Mesh:MeshMutatingToken`** in **`MeshTokenHeaderName`**. Brick execute accepts **`BrickExecuteToken`** in **`BrickExecuteTokenHeaderName`**, or when only **`MeshMutatingToken`** is set, the same secret in **either** the brick header **or** the mesh header (so one worker secret works for both). |
| **Payload cap** | Rejects POST/PUT/PATCH when **`Content-Length`** exceeds **`MaxJsonBodyBytes`** (413). |
| **Rate limit** | Per **remote IP**, separate buckets for mesh vs brick execute; fixed window (**`RateLimitWindowSeconds`** / **`RateLimitPermitLimit`**). Returns 429. |

## Pipeline order (`Ashlar.API`)

1. Static files  
2. **`UseAshlarMeshSecurity()`** — mesh token, brick token, body size, rate limit  
3. **`UseAshlarApiKeyAuth()`** — existing global API key / bearer / basic  

Workers must send **both** configured mesh headers **and** global API credentials when both layers are enabled.

## Configuration

See [Configuration.md](Configuration.md) → **Mesh and brick HTTP hardening**. Example:

```bash
export Ashlar__Security__Mesh__MeshMutatingToken="$(openssl rand -hex 32)"
export Ashlar__Security__AuthorizationMode=ApiKey
export Ashlar__Security__ApiKey="$(openssl rand -hex 16)"
```

## Not in this phase (later)

- **mTLS** at ingress or sidecar (operator choice).
- **Per-brick ACL** beyond shared execute token.
- **Distributed rate limits** across API replicas (use a gateway or Redis).

## Revision history

| Date | Change |
|------|--------|
| 2026-04-22 | Initial Phase 2 doc and `Ashlar:Security:Mesh` options. |
