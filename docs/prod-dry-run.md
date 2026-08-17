# Production-shaped container dry run (Linux)

This is an **operations-level** dry run: you use the **same Docker Compose files and images** as production-shaped deployments (API + Ollama + volumes), then verify the HTTP surface comes alive—without replacing that with `dotnet test` alone.

Unit and integration tests (`Category=ProdStyle`, `VirtualProductionNcrRoutingHost`, etc.) validate **code paths**. This flow validates **how the shipped container behaves** on your Linux host.

## Prerequisites

- Docker Engine + **Compose V2** (`docker compose`).
- From repository root (paths in compose files assume this).
- On **Apple Silicon**, builds default to `DOCKER_DEFAULT_PLATFORM=linux/amd64` (avoids grpc `protoc` segfault on `linux_arm64`; same as mesh-lab).

## Quick path — portal stack (minimal prod shape)

Matches **Golden path A** in [`docs/DEPLOYMENT.md`](DEPLOYMENT.md): `deploy/compose/docker-compose.portal.yml`, image from `.docker/Dockerfile.api`.

```bash
make prod-dry-run
# or
./scripts/prod-dry-run.sh --portal
```

The script **builds**, **starts** services, waits until **`GET /health`** succeeds, checks **`GET /api/status`**, then **`docker compose down`** unless you pass **`--keep-up`**.

## Fuller path — agent server (mounted workspace + background agents)

Matches **Golden path C**: `deploy/compose/docker-compose.agent-server.yml`, image from `.docker/Dockerfile.agent-server`, optional **`NEXO_BACKGROUND_AGENTS_CONFIG`**.

```bash
make prod-dry-run-agent-server
# or
./scripts/prod-dry-run.sh --agent-server
```

**`NEXO_REPO_ROOT`** (host tree bind-mounted at `/work`) needs no setting for the default layout: the script defaults it to the repo root, and the compose file itself defaults to `../..` relative to `deploy/compose/` (also the repo root, whatever the shell CWD). Set it only to mount a different tree. Optional env file: [`docs/config/agent-server.env.example`](config/agent-server.env.example) (pass with `--env-file`, or save as `deploy/compose/.env`; Compose does not read a repo-root `.env` when the compose file lives under `deploy/compose/`).

## First-time Ollama models

On first boot the API may wait while **Ollama** starts. If `/health` times out, pull a model once:

```bash
docker compose -f deploy/compose/docker-compose.portal.yml exec ollama ollama pull llama3.1:latest
```

(Use the same compose file you chose for the dry run.)

## Environment variables (script)

| Variable | Purpose |
|----------|---------|
| `NEXO_AGENT_SERVER_HTTP_PORT` | Host port (default **8080**) |
| `NEXO_PROD_DRY_RUN_HOST` | Bind address for curls (default **127.0.0.1**) |
| `NEXO_REPO_ROOT` | Host path to repo for **agent-server** bind-mount |

## Options

| Flag | Meaning |
|------|---------|
| `--portal` | Use `deploy/compose/docker-compose.portal.yml` (default for `make prod-dry-run`) |
| `--agent-server` | Use `deploy/compose/docker-compose.agent-server.yml` |
| `--keep-up` | Do not run `compose down` after checks |
| `--no-build` | Skip `docker compose build` (reuse images) |

## CI

Use the same commands in a Linux job after `docker build` caching: one job runs **`./scripts/prod-dry-run.sh --portal`**, another can run **`--agent-server`** with `NEXO_REPO_ROOT=$GITHUB_WORKSPACE` and **`--keep-up`** omitted so the stack is torn down automatically.

## Relationship to tests

| Mechanism | What it proves |
|-----------|----------------|
| `make test-prime-time` / `Category=ProdStyle` | Framework + DI + HTTP factories in the test process |
| **`scripts/prod-dry-run.sh`** | Published **Nexo.API** image and Compose **network + volumes + sidecars** |

Use **both** for “as close to prod as we can” on Linux.
