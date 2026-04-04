# Self-hosted Nexo agent server (portal + cluster)

This is the **self-hosted** setup for running Nexo as a small **agent server**: a browser **Director portal**, HTTP APIs, **Ollama**, and the **Runtime Studio** background agent cluster (planner + optimizer + tester) described in `apps/runtime-studio/README.md`.

It is **not** the Cursor IDE remote stack; Nexo is the framework that hosts agents and exposes them over HTTP.

## What you get

| Surface | Purpose |
|--------|---------|
| `http://<host>:8080/` | Director portal (static UI) |
| `POST /api/director/run` | One directorial iteration → daily JSON |
| `GET /api/director/dailies` | List dailies |
| `POST /api/agent`, `POST /api/orchestrate` | On-demand agent / orchestration calls |
| `GET /api/status` | API + aggressiveness mode |
| Background agents | Loaded from JSON; default path is Runtime Studio’s `agent_set.local.json` |

The API process registers the same **dogfood runners** as `nexo background-agent daemon` (analysis, tests, self-extend), so scheduled agents can act on the **mounted repository**.

## Prerequisites

- Docker with Compose v2
- This repository checked out on the host (the compose file **bind-mounts** it into the container at `/work`)
- Enough disk for Ollama models

## Start

From the **repository root**:

```bash
docker compose -f docker-compose.agent-server.yml up -d --build
```

Pull the model referenced by `apps/runtime-studio/config/agent_set.local.json` (default **llama3.1**):

```bash
docker compose -f docker-compose.agent-server.yml exec ollama ollama pull llama3.1
```

Smoke checks:

```bash
curl -s http://localhost:8080/api/status
```

Open the portal: `http://localhost:8080`

## Configuration

| Variable | Role |
|----------|------|
| `NEXO_REPO_ROOT` | Host path to mount as `/work` (default `.` = current directory). |
| `NEXO_BACKGROUND_AGENTS_CONFIG` | Optional JSON with `BackgroundAgents:Agents` (set in compose to Runtime Studio config). |
| `OLLAMA_BASE_URL` / `OLLAMA_MODEL` | Provider for LLM-backed agents. |
| `NEXO_DAILIES_PATH` | Where director dailies JSON is stored (`/data/dailies` in compose). |
| `Nexo__Barriers__RequireExplicitBarrier` | Set `false` for hosted runs without full barrier config (see compose). |
| `NEXO_OBSERVATION_DEGRADED_MODE` | `1` avoids brittle observation stores on some volume mounts. |

Customize the cluster by editing `apps/runtime-studio/config/agent_set.local.json` on the host (mounted into the container).

## Image choice

`docker-compose.agent-server.yml` builds from `.docker/Dockerfile.agent-server`, which uses the **.NET SDK** in the final image so **test** and **build** style agents can run `dotnet` against `/work`. The lighter `docker-compose.portal.yml` image (`Dockerfile.api`) is still suitable when you only need the portal + API **without** a full agent cluster on a mounted repo.

## Hardening

For anything beyond a trusted LAN, put **TLS + auth** in front of port `8080`, restrict network access, and treat the mounted repo as **read/write** within agent policy. See also `docs/SelfHostedGameServerPortal.md`.

## Stop

```bash
docker compose -f docker-compose.agent-server.yml down
```
