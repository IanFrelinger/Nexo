# Virtual mesh lab (heterogeneous images + auth)

The lab runs **multiple Nexo.API containers** on one Docker bridge so you can test **different images and security configurations** together without extra hardware.

## What runs by default

| Role | Dockerfile (override) | Runtime / env highlights | Auth (override) |
|------|-------------------------|---------------------------|-----------------|
| **peer-a** | `MESH_LAB_PEER_A_DOCKERFILE` → **`.docker/Dockerfile.api`** | `ASPNETCORE_ENVIRONMENT` = Production (default) | **ApiKey** (`Nexo__Security__ApiKey`) |
| **peer-b** | `MESH_LAB_PEER_B_DOCKERFILE` → **`.docker/Dockerfile.quickstart`** | **`NEXO_ALLOW_MOCK=1`**, Staging | **ApiKeyOrBearerToken** — same **`Nexo__Security__ApiKey`** *or* **`Nexo__Security__PeerB__BearerToken`** |
| **worker** (profile **`workers`**) | `MESH_LAB_WORKER_DOCKERFILE` → **`.docker/Dockerfile.api`** | Development, `ShowAdvisoryInPortal` off by default | **ApiKeyOrBasic** — API key *or* Basic (**`nexo`** + **`Nexo__Security__Worker__BasicAuthPassword`**) |

**Optional heavier worker image:** set `MESH_LAB_WORKER_DOCKERFILE=.docker/Dockerfile.agent-server` (SDK-based final image; slower CI/build, richer for local soak tests).

All **`Nexo__Security__*`** keys map to the same binding as production (`Nexo:Security`); see **`docs/Configuration.md`**.

## Prerequisites

- Docker Engine + Compose v2
- RAM for **two** full image builds (`api` + `quickstart` differ in final stage); workers reuse the **`api`** image by default

## Start

```bash
cp docs/config/mesh-lab.env.example .env.mesh-lab
# Set Nexo__Security__ApiKey, Nexo__Security__PeerB__BearerToken, Nexo__Security__Worker__BasicAuthPassword

docker compose -f docker-compose.mesh-lab.yml --env-file .env.mesh-lab up -d --build
./scripts/mesh-lab-verify.sh .env.mesh-lab
```

Host URLs: **`http://127.0.0.1:18081`** (peer-a), **`http://127.0.0.1:18082`** (peer-b).

## Workers + stress ramp

```bash
docker compose --profile workers -f docker-compose.mesh-lab.yml --env-file .env.mesh-lab up -d --scale worker=2 worker
./scripts/mesh-lab-stress-ramp.sh .env.mesh-lab 8 2 30 4
```

## Try the mesh CLI

```bash
export NEXO_MESH_DIRECTOR_BASE_URL=http://127.0.0.1:18081
export NEXO_MESH_API_KEY='your-key'
dotnet run --project application/src/Nexo.CLI -- mesh director get /health --json

export NEXO_MESH_DIRECTOR_BASE_URL=http://127.0.0.1:18082
# peer-b accepts Bearer OR same API key:
dotnet run --project application/src/Nexo.CLI -- mesh director get /health --json
```

## instances.json (optional)

Use host URLs from above; see previous revision of this doc for a JSON template (`mesh hub list`).

## Stop

```bash
docker compose --profile workers -f docker-compose.mesh-lab.yml --env-file .env.mesh-lab down -v
```

## CI

**`.github/workflows/mesh-lab-gate.yml`** writes **`Nexo__Security__ApiKey`**, **`PeerB__BearerToken`**, and **`Worker__BasicAuthPassword`**, then runs **`mesh-lab-verify.sh`**.

## Revision history

| Date | Change |
|------|--------|
| 2026-04-23 | Initial virtual mesh lab. |
| 2026-04-24 | Scalable workers + stress ramp. |
| 2026-04-24 | Heterogeneous Dockerfiles + auth modes per role. |
