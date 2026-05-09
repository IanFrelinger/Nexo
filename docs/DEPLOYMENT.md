# Deployment paths (operator guide)

This document is the **default “what do I run in production?”** map. Other compose files exist for labs, mesh, and CI; this page names the **golden paths** and how to **pin versions** so `latest` does not drift under you.

## Golden path A — Portal + API + Ollama (single host)

**Use when:** you want the web portal, HTTP API on port 8080, and local Ollama on the same machine.

- **Compose file:** `docker-compose.portal.yml`
- **Build context:** repo root; API image builds from `.docker/Dockerfile.api`
- **Bind:** API and Ollama default to **loopback** (`127.0.0.1`) — good for laptop + Tailscale / reverse proxy patterns (see `docs/TailscaleAndNexo.md`)

**Pin a version (recommended):**

1. Build and tag images yourself in CI or locally, **or** use GHCR images once you publish them with **semver or digest** tags (see below).
2. In `docker-compose.portal.yml`, replace `build:` with `image: ghcr.io/<owner>/nexo-api:<tag>` (and pin `ollama/ollama` to a digest if you need full reproducibility).
3. Never rely on **`latest`** for production unless you accept silent upgrades.

## Golden path B — CLI only (agents, CI, minimal host)

**Use when:** you need `nexo` in a container with no portal stack.

- **Image:** `ghcr.io/<owner>/nexo-cli` (built by `.github/workflows/container-image-publish.yml` from `.docker/Dockerfile.cli`)
- **Pin:** use tag **`sha-<12-char-commit>`** (always pushed) or a **semver tag** `v1.2.3` / `1.2.3` when you cut a Git **annotated tag** `v1.2.3` on `master`/`main` (workflow publishes those tags in addition to `latest`).
- **Example:** `docker pull ghcr.io/ianfrelinger/nexo-cli:sha-abc123def456`

## Golden path C — Agent server (mounted workspace)

**Use when:** background agents with a host-mounted repo (see `docs/SelfHostedAgentServer.md`).

- **Compose file:** `docker-compose.agent-server.yml`
- **Pin:** same rules as A — prefer **immutable image references** for `nexo-api` (or your wrapper image).

## Other compose files (not default production)

| File | Role |
|------|------|
| `docker-compose.agent-server.yml` | Agent server + workspace |
| `docker-compose.mesh-lab.yml` | Multi-node lab |
| `docker-compose.friend-mesh.yml` | Friend mesh prefab |
| `docker-compose.ollama.yml` | Ollama sidecar only |
| `docker-compose.test.yml` | Test harness |
| `docker-compose.ephemeral.yml` | Ephemeral stacks |

## NuGet packages (embed or tool repos)

- **Hosting (full graph):** `Nexo.Hosting.Bundle` at one version — see `docs/PUBLISHING.md`
- **HTTP client:** `Nexo.Sdk` / `Nexo.Client` — pin the **same release** as your server image or API deployment when possible

## One-button release (recommended)

1. **Tag** `vX.Y.Z` on the commit you want to ship and **push the tag**.
2. GitHub runs **`.github/workflows/release.yml`**: **GHCR** `nexo-cli` + `nexo-api` (sha + semver tags) and **NuGet** pack/push (per `NUGET_PUBLISH_MODE`).
3. Open the workflow run **Summary** for copy-paste **pin lines** (sha + semver + NuGet version).

Step-by-step checklist: **`docs/RELEASE_RUNBOOK.md`**.

**NuGet-only** (e.g. hotfix packages without retagging images): **Actions → Release NuGet packages** (`release-nuget.yml`).

**Images on `master` without a release:** still driven by **Container Image Publish** on path-filtered pushes.

## Production-shaped dry run on Linux (containers)

For an operations-level dry run—**same Compose topology and images** as the golden paths above—see **`docs/prod-dry-run.md`** and run **`make prod-dry-run`** or **`./scripts/prod-dry-run.sh`**.

## CI vs production

- **Green on `master`** does not imply every optional workflow gate ran (path filters). For a release, run **`runtime-release-gate`** (and your own smoke) on the **tag** you intend to ship.
- **Forks** cannot push to GHCR with default `GITHUB_TOKEN`; image publish jobs may be skipped or fail until run in the upstream repo or with a PAT.

## Secrets checklist

| Secret | Purpose |
|--------|---------|
| `NUGET_USER` | NuGet.org **profile name** (not email) for **Trusted Publishing** + `NuGet/login@v1` |
| `NUGET_API_KEY` | Optional **fallback** long-lived key if Trusted Publishing is not configured yet |
| `GITHUB_TOKEN` | Provided by Actions; used for GHCR push in `container-image-publish` |

Configure **Trusted Publishing** on nuget.org for workflow file **`release.yml`** (filename only: `release.yml`) per [NuGet trusted publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing). The NuGet-only dispatch workflow **`release-nuget.yml`** is optional; OIDC is bound to **`release.yml`** for tag releases.
