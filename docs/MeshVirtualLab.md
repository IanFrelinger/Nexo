# Virtual mesh lab (multi-node on one machine)

Use **`docker-compose.mesh-lab.yml`** to run **two fixed Nexo.API peers** on an isolated Docker bridge, plus an optional **scalable `worker` service** (Compose profile **`workers`**) for load / ramp testing without extra hardware.

## Prerequisites

- Docker Engine + Compose v2
- Enough RAM/disk to build **`Nexo.API`** twice (images are shared after first build)

## Start the lab

```bash
cd /path/to/Nexo
cp docs/config/mesh-lab.env.example .env.mesh-lab
# Edit Nexo__Security__ApiKey

docker compose -f docker-compose.mesh-lab.yml --env-file .env.mesh-lab up -d --build
```

Default **host** URLs (loopback):

| Service | URL |
|---------|-----|
| **peer-a** | `http://127.0.0.1:18081` |
| **peer-b** | `http://127.0.0.1:18082` |

Inside the lab network, containers resolve **`http://peer-a:8080`** and **`http://peer-b:8080`**.

With profile **`workers`**, **`http://worker:8080`** resolves to **N** replicas (Docker DNS round-robin). Start workers:

```bash
docker compose --profile workers -f docker-compose.mesh-lab.yml --env-file .env.mesh-lab up -d --scale worker=2 worker
```

## Dynamic scale / stress ramp

After **`peer-a`** / **`peer-b`** are healthy, ramp replica count and hit **`/health`** in parallel from inside the lab network:

```bash
chmod +x scripts/mesh-lab-stress-ramp.sh
# .env.mesh-lab max_workers step requests_per_step pause_seconds
./scripts/mesh-lab-stress-ramp.sh .env.mesh-lab 12 2 40 5
```

This uses **`docker compose up --scale worker=N`** between steps and **`curlimages/curl`** containers on the **`mesh_lab`** network. It stress-tests **HTTP + container scheduling**, not a built-in Nexo mesh control plane (unless you add one).

## Automated verify

```bash
chmod +x scripts/mesh-lab-verify.sh
./scripts/mesh-lab-verify.sh .env.mesh-lab
```

The script waits for **`/health`** on the published ports, then checks **cross-container** HTTP using a short-lived **`curlimages/curl`** container on the same Docker network (no dependency on `curl` inside the ASP.NET image).

## Try the mesh CLI against the lab

From the host (same API key in both peers):

```bash
export NEXO_MESH_DIRECTOR_BASE_URL=http://127.0.0.1:18081
export NEXO_MESH_API_KEY='your-key-from-.env.mesh-lab'
dotnet run --project src/Nexo.CLI -- mesh director get /health --json
dotnet run --project src/Nexo.CLI -- mesh hub health --url http://127.0.0.1:18082
```

## instances.json for discovery (optional)

Point **`NEXO_MESH_INSTANCES_PATH`** at a JSON file listing **`peer-a`** / **`peer-b`** using **host** URLs above (or use a host-only DNS name if you add one). Example:

```json
[
  {
    "peerId": "lab-a",
    "endpoint": "http://127.0.0.1:18081/",
    "capabilities": ["nexo-cli"],
    "trustTier": "Trusted",
    "admitted": true
  },
  {
    "peerId": "lab-b",
    "endpoint": "http://127.0.0.1:18082/",
    "capabilities": ["nexo-cli"],
    "trustTier": "Trusted",
    "admitted": true
  }
]
```

Then:

```bash
export NEXO_MESH_INSTANCES_PATH=/absolute/path/to/lab-instances.json
dotnet run --project src/Nexo.CLI -- mesh hub list
```

## Stop and clean

```bash
docker compose -f docker-compose.mesh-lab.yml --env-file .env.mesh-lab down -v
```

## CI

Workflow **`.github/workflows/mesh-lab-gate.yml`** runs **`compose config`**, **`up`**, and **`scripts/mesh-lab-verify.sh`** on **`ubuntu-latest`** (same pattern as the friend-mesh gate).

## Revision history

| Date | Change |
|------|--------|
| 2026-04-23 | Initial virtual mesh lab compose, verify script, and docs. |
| 2026-04-24 | Scalable worker profile + mesh-lab-stress-ramp.sh for dynamic replica ramp. |
