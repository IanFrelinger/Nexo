# Distribution models

This page is for **maintainers and integrators**: how Nexo is shipped, how each channel is **pinned**, and which **automated gates** prove that channel still works.

For day-to-day embedding and extension work, start with **`docs/IntegratorGuide.md`**, **`docs/sdk.md`**, and **`docs/architecture/runtime-vs-application.md`**.

For the authoritative open-vs-commercial boundary, see **`docs/OpenCoreBoundary.md`**.

## Version spine

Ship the **same semantic version** across artifacts that belong together:

- **NuGet:** all `Nexo.*` packages for a release share one `PackageVersion`; consumers often reference **`Nexo.Hosting.Bundle`**. See **`docs/PUBLISHING.md`**.
- **Containers:** GHCR images tagged with the same **`vX.Y.Z`** (and digest pins for production). See **`docs/RELEASE.md`** and **`docs/DEPLOYMENT.md`**.

Release automation is summarized in **`docs/RELEASE.md`** (NuGet + GHCR from one tag when configured).

## Distribution channels

| Channel | Primary artifacts | How consumers pin | First success (manual) |
|--------|---------------------|-------------------|-------------------------|
| **NuGet host embed** | `Nexo.Hosting` graph, **`Nexo.Hosting.Bundle`** | Package version on a feed | Build and run **`docs/samples/StableSdkHostSample/`** (see **`docs/SdkIntegrationGuide.md`**) |
| **NuGet client** | **`Nexo.Client`** / **`Nexo.Sdk`** | Package version | **`docs/sdk.md`** (client quick start) |
| **HTTP-only** | Running **`Nexo.API`** | Base URL + TLS + API key policy | **`curl`** `GET /health`, `GET /api/status` (see **`docs/SelfHostedAgentServer.md`**) |
| **CLI** | **`Nexo.CLI`** binary or **GHCR `nexo-cli`** image | Image tag or digest; CLI `--version` / package | **`docs/GettingStarted.md`** (`doctor`, `pipeline`) |
| **Compose / operators** | Root **`docker-compose*.yml`** + operator docs | Compose file revision + image digests | **`docs/DEPLOYMENT.md`**, stack-specific guides |
| **Source / monorepo** | `ProjectReference` into **`src/`** | Git commit / branch | **`docs/IntegratorGuide.md`** (project reference example) |
| **Mesh / federation (open peers)** | Peer config, local mesh primitives, worker executor | `instances.json`, env vars | **`docs/IntegratorGuide.md`**, **`docs/FriendMeshPrefab.md`**, **`docs/MeshVirtualLab.md`** |
| **Mesh fleet director (commercial)** | `Nexo.Commercial.Fleet.Host`, `/api/mesh/*` director APIs | Image tag + API key + peer registration key | **`.docker/Dockerfile.fleet-host`**, **`scripts/commercial-fleet-host-smoke.sh`**, mesh-lab peer-a |

## Golden reference pins (copy/paste)

Use these as **stable entrypoints** when writing runbooks or samples; replace image digests and package versions with whatever you shipped (`docs/RELEASE.md`).

| Channel | Golden artifact / path |
|--------|-------------------------|
| **NuGet host sample (local pack)** | `docs/samples/StableSdkHostSample/package-consumer/StableSdkHostSample.Package.csproj` + **`scripts/verify-stable-sdk-host-sample-packages.sh`** |
| **NuGet metapackage (consumers)** | **`Nexo.Hosting.Bundle`** at the release semver (see **`docs/PUBLISHING.md`**) |
| **HTTP / API container** | Build **`.docker/Dockerfile.api`**; container listens on **`8080`** (`ASPNETCORE_URLS=http://+:8080`). Smoke script: **`scripts/ci/distribution-matrix-api-http-smoke.sh`**. |
| **CLI container (public)** | **`ghcr.io/ianfrelinger/nexo-cli:latest`** — smoke with **`--help`** and **`pipeline validate --help`** (not **`doctor`**, see **`docs/GettingStarted.md`**). |
| **Compose (operator lab)** | **`docker-compose.ephemeral.yml`** (light deps) or **`docker-compose.portal.yml`** (Director stack) — hub **`docs/DEPLOYMENT.md`**. |
| **Mesh prefab** | **`docker-compose.friend-mesh.yml`** + **`docs/FriendMeshPrefab.md`** |
| **Mesh lab (heterogeneous)** | **`docker-compose.mesh-lab.yml`** — peer-a = commercial fleet host, peer-b/worker = open `Nexo.API` | **`docs/MeshVirtualLab.md`**, **`scripts/mesh-lab-verify.sh`** |

## Contract boundaries

- **Host SDK (stable):** `Nexo.Hosting.Sdk` — register bricks/agents before **`AddNexo`**. See **`docs/sdk.md`**.
- **Client SDK (stable):** `Nexo.Client` / **`INexoClient`** — typed HTTP to a running API; use **`InvokeAsync`** for routes not yet wrapped. See **`docs/sdk.md`** and **`docs/api/index.md`**.
- **HTTP:** stable routes and behavior for external clients; breaking changes should follow your HTTP/versioning policy (same major concern as mobile clients).
- **CLI:** treat breaking flags as **consumer-facing** changes; document in release notes.

Breaking-change policy for packages: **`docs/SdkCompatibilityPolicy.md`**.

## Automated gates (CI)

The workflow **`.github/workflows/distribution-matrix-gate.yml`** runs **in parallel** on relevant pull requests, on pushes to the default branch (when touched paths match), on **`workflow_dispatch`**, and on a **weekly schedule** so path-filtered PRs do not miss cross-cutting breaks.

| Matrix job | What it proves |
|------------|----------------|
| **nuget-local-pack-consumer** | Local pack → **`scripts/verify-stable-sdk-host-sample-packages.sh`** (isolated NuGet cache, sample restores from folder feed + nuget.org). |
| **cli-image-smoke** | **`.docker/Dockerfile.cli`** builds; container runs **`--help`** and **`pipeline validate --help`** (runtime image has no git/curl, so **`doctor`** is not used here). |
| **api-image-http-smoke** | **`.docker/Dockerfile.api`** builds; container serves **`/health`** and **`/api/status`** (host **`curl`** — HTTP-only consumer path). Script: **`scripts/ci/distribution-matrix-api-http-smoke.sh`**. |
| **nexo-client-inprocess-test** | **`Nexo.Client`** `GetStatusAsync` against in-process **`Nexo.API`** (same pipeline as production; **`net9.0`** test filter). |
| **pack-hosting-graph-alignment** | **`scripts/verify-pack-nexo-hosting-graph-alignment.py`** — pack allowlist matches **`Nexo.Hosting`** MSBuild graph. |

**Post-publish NuGet** verification (nuget.org index + retries) stays in **`docs/PUBLISHING.md`**, **`docs/NuGetConsumerVerify.md`**, and the reusable workflow **`reusable-verify-nuget-consumer.yml`** (invoked from release flows).

Other related gates (not duplicated here): **`compose-gate.yml`**, **`devcontainer-gate.yml`**, **`container-image-gate.yml`**, **`pack-hosting-graph-alignment.yml`**, mesh prefab / mesh lab workflows under **`.github/workflows/`**.

## Integrator compatibility matrix

Release owners should keep the **version / runtime table** in **`docs/IntegratorGuide.md`** up to date when you cut a release (single source of truth for “which Nexo version on which .NET”).

## See also

- **`docs/DocsIndex.md`** — documentation hub.
- **`docs/RELEASE_RUNBOOK.md`** — ship decisions and checks after tag.
