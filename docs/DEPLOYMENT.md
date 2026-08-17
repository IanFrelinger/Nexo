# Deployment paths (operator guide)

This document is the **default “what do I run in production?”** map. Other compose files exist for labs, mesh, and CI; this page names the **golden paths** and how to **pin versions** so `latest` does not drift under you.

## Golden path A — Portal + API + Ollama (single host)

**Use when:** you want the web portal, HTTP API on port 8080, and local Ollama on the same machine.

- **Compose file:** `deploy/compose/docker-compose.portal.yml`
- **Build context:** repo root; API image builds from `.docker/Dockerfile.api`
- **Bind:** API and Ollama default to **loopback** (`127.0.0.1`) — good for laptop + Tailscale / reverse proxy patterns (see `docs/TailscaleAndNexo.md`)
- **First boot:** the bundled Ollama ships with no models — `docker compose -f deploy/compose/docker-compose.portal.yml exec ollama ollama pull llama3.1:latest` (or the tag you set in `OLLAMA_MODEL`). The stack points every Ollama key family at the `ollama` service; see `docs/Configuration.md`, "Ollama".

**Pin a version (recommended):**

1. Build and tag images yourself in CI or locally, **or** use GHCR images once you publish them with **semver or digest** tags (see below).
2. In `deploy/compose/docker-compose.portal.yml`, replace `build:` with `image: ghcr.io/<owner>/nexo-api:<tag>` (and pin `ollama/ollama` to a digest if you need full reproducibility).
3. Never rely on **`latest`** for production unless you accept silent upgrades.

## Golden path B — CLI only (agents, CI, minimal host)

**Use when:** you need `nexo` in a container with no portal stack.

- **Image:** `ghcr.io/<owner>/nexo-cli` (built by `.github/workflows/container-image-publish.yml` from `.docker/Dockerfile.cli`)
- **Pin:** use tag **`sha-<12-char-commit>`** (always pushed) or a **semver tag** `v1.2.3` / `1.2.3` when you cut a Git **annotated tag** `v1.2.3` on `master`/`main` (workflow publishes those tags in addition to `latest`).
- **Example:** `docker pull ghcr.io/ianfrelinger/nexo-cli:sha-abc123def456`

## Golden path C — Agent server (mounted workspace)

**Use when:** background agents with a host-mounted repo (see `docs/SelfHostedAgentServer.md`).

- **Compose file:** `deploy/compose/docker-compose.agent-server.yml`
- **Pin:** same rules as A — prefer **immutable image references** for `nexo-api` (or your wrapper image).

## Other compose files (not default production)

| File | Role |
|------|------|
| `deploy/compose/docker-compose.agent-server.yml` | Agent server + workspace |
| `deploy/compose/docker-compose.mesh-lab.yml` | Multi-node lab |
| `deploy/compose/docker-compose.friend-mesh.yml` | Friend mesh prefab |
| `deploy/compose/docker-compose.ollama.yml` | Ollama sidecar only |
| `deploy/compose/docker-compose.test.yml` | Test harness |
| `deploy/compose/docker-compose.ephemeral.yml` | Disposable Ollama / Postgres + `nexo` CLI (`.docker/Dockerfile.cli`) for one-off `run --rm nexo ...` |
| `deploy/k8s/nexo-mesh-worker-deployment.yaml` | Kubernetes mesh-worker Deployment sample (`docs/WorkloadScaling.md`) |

## Container health, readiness and hardening

The API images (`.docker/Dockerfile.api`, `Dockerfile.quickstart`, `Dockerfile.fleet-host`) share one shape:

- **`HEALTHCHECK`** probes `GET /health` with bash's `/dev/tcp` (the `mcr.microsoft.com/dotnet/aspnet` runtime images ship no `curl`), so `docker ps` shows `(healthy)` and `docker compose up --wait` returns as soon as the API answers. `scripts/prod-dry-run.sh` no longer hides `--wait` failures.
- **`GET /health`** is liveness (constant 200 while the process serves HTTP). **`GET /ready`** is readiness: 200 once the host has finished starting (DI built, hosted services started) and 503 while starting or once shutdown begins (`IHostApplicationLifetime`), so orchestrators drain traffic before Kestrel closes. Both are unauthenticated and outside `/api`. Use `/ready` for Kubernetes `readinessProbe` / load-balancer checks and `/health` for `livenessProbe`; the k8s sample wires both.
- **Non-root:** the runtime stages run as the aspnet image's unprivileged `app` user (`USER $APP_UID`, uid 1654 in .NET 8+). `/app` is root-owned and never written; everything the process writes lives under `/data` (owned by `app`). Named volumes created by **older, root-running images** keep root ownership — fix once with `docker run --rm -v <volume>:/v alpine chown -R 1654:1654 /v` (e.g. `nexo-dailies`) or recreate the volume.
- **Swagger** (`/swagger`, `/swagger/v1/swagger.json`) is on only in the `Development` environment or when `Nexo__Api__EnableSwagger=true`; production images ship with it off.
- **Kubernetes:** `NEXO_DEPLOYMENT_PROFILE` must be one of `full`, `server`, `edge`, `air-gapped`, `system` (`AddNexo` refuses anything else at startup); the mesh-worker sample uses `server`.

## Runtime state (`NEXO_STATE_DIR`)

LiteDB stores and snapshots (`nexo-patterns.db`, `nexo-adaptation.db`, `nexo-adaptation-audit.db`, `nexo-copilot-tasks.db`, `nexo-execution.db`, `nexo-test-failures.db`, `nexo-snapshots/`) default to **`<repo or app root>/.nexo/state/`** (gitignored) unless `Nexo:PatternStorePath` / `--store-path` names an explicit location. Set **`NEXO_STATE_DIR`** (absolute, or relative to that root) to move the whole directory. The images set `NEXO_STATE_DIR=/data/state`, and the portal and agent-server stacks mount the **`nexo-state`** named volume there, so state survives `docker compose up --force-recreate` and never lands in a bind-mounted repo. Existing installs that already have `nexo-*.db` at the repo root keep using them until you move the files into `.nexo/state/` (see `docs/Configuration.md`, "Runtime state").

## NuGet packages (embed or tool repos)

- **Hosting (full graph):** `Nexo.Hosting.Bundle` at one version — see `docs/PUBLISHING.md`
- **HTTP client:** `Nexo.Sdk` / `Nexo.Client` — pin the **same release** as your server image or API deployment when possible

## One-button release (recommended)

1. **Tag** `vX.Y.Z` on the commit you want to ship and **push the tag**.
2. GitHub runs **`.github/workflows/release.yml`**: **GHCR** `nexo-cli` + `nexo-api` (sha + semver tags) and **NuGet** pack/push (per `NUGET_PUBLISH_MODE`).
3. Open the workflow run **Summary** for copy-paste **pin lines** (sha + semver + NuGet version), NuGet **manifest** artifact, and optional **GHCR re-pull smoke** result.

**Which workflow?** **`docs/RELEASE.md`** (hub) → **`docs/RELEASE_RUNBOOK.md`** (checklist + decision table).

**NuGet-only** (e.g. hotfix packages without retagging images): **Actions → Release NuGet packages** (`release-nuget.yml`).

**Images on `master` without a release:** still driven by **Container Image Publish** on path-filtered pushes.

## Production-shaped dry run on Linux (containers)

For an operations-level dry run—**same Compose topology and images** as the golden paths above—see **`docs/prod-dry-run.md`** and run **`make prod-dry-run`** or **`./scripts/prod-dry-run.sh`**.

## CI vs production

- **Green on `master`** does not imply every optional workflow gate ran (path filters). For a release, run **`runtime-release-gate`** (and your own smoke) on the **tag** you intend to ship.
- **Forks** — Default **`GITHUB_TOKEN`** in a fork typically **cannot** publish to **`ghcr.io/<upstream-owner>/...`**. Use the **upstream** repo for release workflows, retarget images to your fork’s GHCR org, or add a **PAT** with `packages: write` and matching `docker/login-action` credentials. Same idea applies if you expect NuGet OIDC from a fork (usually run releases upstream).

## Secrets checklist

| Secret | Purpose |
|--------|---------|
| `NUGET_USER` | NuGet.org **profile name** (not email) for **Trusted Publishing** + `NuGet/login@v1` |
| `NUGET_API_KEY` | Optional **fallback** long-lived key if Trusted Publishing is not configured yet |
| `GITHUB_TOKEN` | Provided by Actions; used for GHCR push in `container-image-publish` |

Configure **Trusted Publishing** on nuget.org per [NuGet trusted publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing): register **`release.yml`** for tag releases and **`release-nuget.yml`** if you use NuGet-only dispatch with OIDC. See **`docs/PUBLISHING.md`** for the full matrix and **`docs/RELEASE_RUNBOOK.md`** for operator steps.
