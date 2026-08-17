# Configuration Reference

Nexo configures via environment variables and optional `~/.nexo/config.json`. This document lists the primary configuration options. Additional options may be available via `appsettings.json` binding — see inline code comments for the full set.

## Core

| Variable | Description | Default |
|----------|-------------|---------|
| `NEXO_CONFIG_PATH` | Path to config file | `~/.nexo/config.json` |
| `NEXO_STATE_DIR` | Runtime-state directory for LiteDB stores and snapshots (see "Runtime state" below); absolute, or relative to the resolved repo/app root | `<repo or app root>/.nexo/state` |
| `NEXO_MESH_INSTANCES_PATH` | Path to **`instances.json`** for **`nexo mesh`** discovery | `~/.nexo/instances.json` |
| `NEXO_MESH_TRUST_POLICY` | Discovery filter: **`any`**, **`allowlist`** (only **`admitted: true`** peers), **`trusted-only`**, **`trusted-preferred`** | **`any`** for discovery (see **`MeshTrustPolicyConfiguration`**) |
| `NEXO_MESH_DIRECTOR_BASE_URL` | Base URL for **commercial mesh director CLI** (`dotnet run --project commercial/src/Nexo.Commercial.MeshDirector -- director ...`) HTTP calls | unset |
| `NEXO_MESH_API_KEY` | Optional **`X-Nexo-Api-Key`** for director CLI | unset |
| `NEXO_MESH_MUTATING_TOKEN` | Optional **`X-Nexo-Mesh-Token`** for mutating mesh routes on the hub | unset |
| `NEXO_MESH_PEER_REGISTRATION_KEY` | Per-peer fleet registration secret for **commercial mesh director CLI `register`** (when director requires distinct key) | unset |
| `NEXO_DEPLOYMENT_PROFILE` | Hosting dependency profile for `AddNexo()` module composition (`full`, `server`, `edge`, `air-gapped`, `system`) | `full` |
| `NEXO_STRICT_MODE` | `1` or `true` = enable strict mode (fail-fast + verbose diagnostics for dev/CI; disable for production) | `false` |
| `NEXO_AIRGAP` | `1` or `true` = air-gapped; no cloud calls | unset |
| `NEXO_AIRGAP_PROBE` | `1` = probe network to detect air-gap | unset |
| `NEXO_TRUST_ENABLED` | `1` = enable Trust & sanitization | `false` |
| `NEXO_MODEL_PROVIDER` | Default LLM provider | from config |
| `NEXO_LOOP_PARALLEL` | `1` = parallel loop kernel | `false` |
| `NEXO_LOOP_INSTRUMENT` | `1` = instrumented loop | `false` |
| `NEXO_LLM_RETRY_COUNT` | Retries for cloud LLM (5xx/429) | `3` |

## Strict Mode (`NEXO_STRICT_MODE`)

Strict mode is designed for development and CI environments. When enabled, Nexo fails fast and emits verbose diagnostics instead of silently falling back to defaults or retrying on errors. Flip it to permissive (disabled) once confident in the agentic layer for production.

**Master switch:** `NEXO_STRICT_MODE=1` enables all sub-flags below. Individual flags can override the master switch.

| Variable / Config Key | Description | Default |
|-----------------------|-------------|---------|
| `NEXO_STRICT_MODE` | Master switch — enables all sub-flags | `false` |
| `Nexo:StrictMode:FailFastOnValidationErrors` | Throw immediately on validation failures | follows master |
| `Nexo:StrictMode:FailFastOnProviderErrors` | Throw on provider misconfiguration instead of fallback | follows master |
| `Nexo:StrictMode:FailFastOnPipelineErrors` | Throw on pipeline stage errors instead of retrying | follows master |
| `Nexo:StrictMode:VerboseDiagnostics` | Emit debug-level logging and detailed error messages | follows master |
| `Nexo:StrictMode:FailOnConfigurationWarnings` | Treat missing config files / empty configs as hard errors | follows master |

**Usage examples:**

```bash
# Development / CI — fail fast and verbose
export NEXO_STRICT_MODE=1

# Production — permissive (default)
# unset NEXO_STRICT_MODE   (or NEXO_STRICT_MODE=0)

# Fine-grained: strict for providers only
export NEXO_STRICT_MODE=0
# then in appsettings.json:
# { "Nexo": { "StrictMode": { "FailFastOnProviderErrors": true } } }
```

## Runtime state (`NEXO_STATE_DIR`)

LiteDB stores and rollback snapshots that Nexo writes at runtime — `nexo-patterns.db`, `nexo-adaptation.db`, `nexo-adaptation-audit.db`, `nexo-copilot-tasks.db`, `nexo-execution.db`, `nexo-test-failures.db`, `nexo-snapshots/` — resolve through `RepoPathResolver.ResolveStateDirectory`:

1. `Nexo:PatternStorePath` (API / daemon) or `--store-path` (CLI) when set: everything is co-located with that file / directory.
2. Otherwise `NEXO_STATE_DIR` when set — absolute, or relative to the resolved repo/app root.
3. Otherwise **`<repo or app root>/.nexo/state/`** (the root is the nearest directory containing `Nexo.sln`, else the current directory). `.nexo/` is gitignored; the directory is created on first use.

Backward compatibility: an install that already has `nexo-*.db` files directly at the repo root **and no `.nexo/state/` yet** keeps using the root (nothing is moved or logged). To migrate, stop Nexo and move the `nexo-*.db` files and `nexo-snapshots/` into `.nexo/state/`.

Containers: the API and CLI images set `NEXO_STATE_DIR=/data/state` (owned by the non-root `app` user); the portal and agent-server compose stacks mount the `nexo-state` named volume there (`docs/DEPLOYMENT.md`, "Runtime state").

## Nexo.API host (`Nexo__Api__*`)

| Variable / config key | Description | Default |
|-----------------------|-------------|---------|
| `Nexo__Api__EnableSwagger` | Serve `/swagger` (UI) and `/swagger/v1/swagger.json`. The document enumerates every mapped route and schema, so it is off outside `Development` unless set | `true` in `Development`, else `false` |

`GET /health` (liveness, constant 200) and `GET /ready` (readiness: 503 while the host is starting or shutting down, 200 in between) are always mapped, unauthenticated, and outside `/api`.

## Centralized Defaults (`NexoDefaults`)

All tunable constants are centralized in `Nexo.Core.Domain.NexoDefaults`. This eliminates hard-coded magic numbers scattered across the codebase. Override any value via environment variables or `appsettings.json` — keys are listed in the relevant sections below.

## Nexo.API exposure (`Nexo__Security__*`)

The profile does not enforce firewalls or Tailscale ACLs, but it **fails closed**: `Lan`, `Tailnet` and `Public` refuse to start while `AuthorizationMode` is `None` (and the legacy flag is off) unless `AllowUnauthenticatedNetworkExposure=true` is set explicitly. See **`SECURITY.md`** ("Default posture and in-scope surfaces"), **`docs/TailscaleAndNexo.md`** and **`docs/config/security-exposure.env.example`**.

| Variable | Description | Default |
|----------|-------------|---------|
| `Nexo__Security__ExposureProfile` | `Localhost`, `Lan`, `Tailnet`, or `Public` (case-insensitive). Off-loopback values require built-in auth (see above) | `Localhost` in `appsettings.json` |
| `Nexo__Security__AllowUnauthenticatedNetworkExposure` | `true` — escape hatch: start with an off-loopback profile and no built-in auth (logs a warning). Only when an authenticating proxy or network ACL fronts the API | `false` |
| `Nexo__Security__CustomAdvisory` | Optional extra line shown in the Director portal advisory | unset |
| `Nexo__Security__ShowAdvisoryInPortal` | `true` / `false` — show advisory banner in portal | `true` |
| `Nexo__Security__RequireApiKeyForMutatingEndpoints` | `true` / `false` — legacy: enforce API key checks for POST/PUT/PATCH/DELETE under `/api/*`. Fails closed: with no `ApiKey` configured, mutating requests get 401 | `false` |
| `Nexo__Security__ApiKey` | Shared secret required for protected mutating requests | unset (disabled) |
| `Nexo__Security__ApiKeyHeaderName` | Header used for key checks | `X-Nexo-Api-Key` |
| `Nexo__Security__ExcludedApiKeyPaths` | Comma-separated API path prefixes exempted from key checks | none |
| `Nexo__Security__AuthorizationMode` | Built-in auth mode: `None`, `ApiKey`, `BearerToken`, `Basic`, `ApiKeyOrBearerToken`, `ApiKeyOrBasic`, `BearerTokenOrBasic`, `Any` | `None` |
| `Nexo__Security__AuthorizationScope` | Built-in auth scope: `MutatingApi` or `AllApi` | `MutatingApi` |
| `Nexo__Security__ExcludedAuthorizationPaths` | Comma-separated API path prefixes exempted from built-in auth checks | none |
| `Nexo__Security__BearerToken` | Shared secret for bearer token authorization | unset |
| `Nexo__Security__BearerTokenHeaderName` | Header used for bearer token checks | `Authorization` |
| `Nexo__Security__BearerTokenScheme` | Bearer scheme prefix when using `Authorization` header | `Bearer` |
| `Nexo__Security__BasicAuthUsername` | Username for built-in basic auth | unset |
| `Nexo__Security__BasicAuthPassword` | Password for built-in basic auth | unset |
| `Nexo__Security__BasicAuthHeaderName` | Header used for basic auth checks | `Authorization` |

Notes:
- If `AuthorizationMode` is set to anything except `None`, built-in auth mode takes precedence over legacy `RequireApiKeyForMutatingEndpoints`.
- `RequireApiKeyForMutatingEndpoints` remains for backward compatibility with existing deployments. Like every other mode it fails closed when its credential is missing.
- `Nexo__Security__ApiKey` / `BearerToken` / `BasicAuthPassword` are compared in constant time against the configured plaintext value; they are not hashed at rest. Keep them in environment / secret stores, not in committed `appsettings.json`.

## Remote execution surface (`Nexo__Execution__*`)

`POST /api/execution/build` and `POST /api/execution/run` let a `RemoteExecutionPlatform` caller (`NEXO_EXECUTION_REMOTE_URL`) build images and run containers on this host's Docker daemon, including host bind mounts. They are **not mapped** unless opted in, and refuse `AuthorizationMode=None` (403) even when opted in.

| Variable / config key | Description | Default |
|------------------------|-------------|---------|
| `Nexo__Execution__ServeRemoteExecution` | `true` maps `/api/execution/build` and `/api/execution/run`. Requires a built-in `Nexo__Security__AuthorizationMode` other than `None`; otherwise every request is refused with 403 | `false` (routes return 404) |
| `Nexo__Execution__AllowedVolumeMountRoot` | Single host directory under which `VolumeMounts` host paths on `/api/execution/run` are accepted (the remote caller mounts only its test-results directory). Unset = any request carrying volume mounts is rejected with 400 | unset |

## Mesh and brick HTTP hardening (`Nexo__Security__Mesh__*`, Phase 2)

Optional middleware runs **before** built-in API auth. It applies to **`/api/mesh/*`** and **`POST /api/bricks/*/execute`**. When all options are unset or zero, behavior matches previous releases (no extra mesh checks).

| Variable / config key | Description | Default |
|------------------------|-------------|---------|
| `Nexo__Security__Mesh__MeshMutatingToken` | When set, **POST/PATCH/DELETE** under `/api/mesh` must send this exact value in the mesh token header | unset |
| `Nexo__Security__Mesh__MeshTokenHeaderName` | Header for mesh mutating token | `X-Nexo-Mesh-Token` |
| `Nexo__Security__Mesh__BrickExecuteToken` | When set, brick execute requires this value in the brick header only | unset |
| `Nexo__Security__Mesh__BrickExecuteTokenHeaderName` | Header for brick execute token | `X-Nexo-Brick-Execute-Token` |
| `Nexo__Security__Mesh__MaxJsonBodyBytes` | Reject POST/PUT/PATCH when `Content-Length` exceeds this (0 = off) | `524288` |
| `Nexo__Security__Mesh__RateLimitPermitLimit` | Max mutating requests per client IP per window for mesh + brick execute (0 = off) | `120` |
| `Nexo__Security__Mesh__RateLimitWindowSeconds` | Window length in seconds | `60` |

When **`BrickExecuteToken`** is unset but **`MeshMutatingToken`** is set, brick execute accepts the mesh secret in **`BrickExecuteTokenHeaderName`** *or* **`MeshTokenHeaderName`**.

Combine with **`Nexo__Security__AuthorizationMode`** and TLS termination for production meshes. See **`docs/MeshPhase2TransportAndAuth.md`**.

## Mesh correlation header (Phase 3)

For **`/api/mesh/*`** and **`POST /api/bricks/*/execute`**, the API assigns or echoes **`X-Nexo-Correlation-Id`** (see [MeshPhase3DistributedExecution.md](MeshPhase3DistributedExecution.md)). Clients may send their own correlation id to align logs across hops.

## Mesh knowledge sync (`Nexo__Mesh__KnowledgeSync__*`, Phase 4)

Only active when **`AddNexo`** registers adaptation (Full/Server/AirGapped profiles with pattern store). Binds section **`Nexo:Mesh:KnowledgeSync`**.

| Variable / config key | Description | Default |
|------------------------|-------------|---------|
| `Nexo__Mesh__KnowledgeSync__Enabled` | `true` to run periodic peer pull | `false` |
| `Nexo__Mesh__KnowledgeSync__PeerBaseUrls__0` | First peer API base URL (https, trailing slash optional) | unset |
| `Nexo__Mesh__KnowledgeSync__IntervalMinutes` | Minutes between pull rounds | `15` |
| `Nexo__Mesh__KnowledgeSync__SinceLookbackMultiplier` | `since = now - interval * multiplier` for export window | `2` |
| `Nexo__Mesh__KnowledgeSync__MaxAdaptations` | Cap per export GET | `500` |
| `Nexo__Mesh__KnowledgeSync__MaxPatterns` | Cap per export GET | `500` |

See [MeshPhase4KnowledgeSync.md](MeshPhase4KnowledgeSync.md).

## Mesh elastic scheduling (`Nexo__Mesh__Elastic__*`, Phase 5)

| Variable / config key | Description | Default |
|------------------------|-------------|---------|
| `Nexo__Mesh__Elastic__Enabled` | `true` to run periodic re-placement for stale **Pending** tasks | `false` |
| `Nexo__Mesh__Elastic__IntervalMinutes` | Minutes between rebalancer rounds | `2` |
| `Nexo__Mesh__Elastic__PendingStaleSeconds` | Pending tasks older than this get `TryScheduleAsync` again | `120` |

Workers should POST heartbeat with **`queueDepth`** (local backlog) so placement prefers idle nodes. See [MeshPhase5ElasticScheduling.md](MeshPhase5ElasticScheduling.md).

## Mesh execution leases (`Nexo__Mesh__Checkpoint__*`, Phase 6)

Binds section **`Nexo:Mesh:Checkpoint`**. See [MeshPhase6LeasesAndCheckpoints.md](MeshPhase6LeasesAndCheckpoints.md).

| Variable / config key | Description | Default |
|------------------------|-------------|---------|
| `Nexo__Mesh__Checkpoint__LeaseSeconds` | Default lease duration after assignment (seconds) when schedule body omits **`leaseSeconds`** | `1800` |
| `Nexo__Mesh__Checkpoint__SweepEnabled` | `true` to periodically move **Assigned**/**Running** tasks with expired leases to **Pending** | `false` |
| `Nexo__Mesh__Checkpoint__SweepIntervalMinutes` | Minutes between sweep passes | `1` |

## Mesh director persistence (`Nexo__Mesh__Persistence__*`, Phase 9)

Binds section **`Nexo:Mesh:Persistence`**. See [MeshPhase9DirectorPersistence.md](MeshPhase9DirectorPersistence.md).

| Variable / config key | Description | Default |
|------------------------|-------------|---------|
| `Nexo__Mesh__Persistence__Provider` | `InMemory` or `LiteDb` | `InMemory` |
| `Nexo__Mesh__Persistence__DatabasePath` | LiteDB file path when provider is LiteDb | `mesh-director.db` |

## Mesh director CLI (`NEXO_MESH_*`, Phase 7)

Used by **commercial mesh director CLI** (`dotnet run --project commercial/src/Nexo.Commercial.MeshDirector -- director ...`) when a worker or script talks to a remote **`Nexo.API`** mesh control plane. See **`docs/MeshPhase7EdgeAlignment.md`**. Discovery-related `NEXO_MESH_*` values also appear under **Core** above; this section summarizes hub HTTP access from the CLI.

| Variable | Description |
|----------|-------------|
| `NEXO_MESH_DIRECTOR_BASE_URL` | Director base URL (e.g. `https://hub:8080`) |
| `NEXO_MESH_API_KEY` | Optional **`X-Nexo-Api-Key`** when the hub enforces API key auth |
| `NEXO_MESH_MUTATING_TOKEN` | Optional **`X-Nexo-Mesh-Token`** for mutating **`/api/mesh`** requests |
| `NEXO_MESH_PEER_REGISTRATION_KEY` | Per-peer registration secret for **commercial mesh director CLI `register`** |

## Pipelines (`NEXO_PIPELINE_*`)

Pipeline options resolve in this order: defaults, config (`Nexo:Pipelines:*`), then environment variables.

| Variable | Description | Default |
|----------|-------------|---------|
| `NEXO_PIPELINE_MAX_RETRIES` | Max stage retry attempts before failure | `3` |
| `NEXO_PIPELINE_RETRY_DELAY_MS` | Delay between retries in milliseconds | `100` |
| `NEXO_PIPELINE_RESUME_FAILED` | `1`/`true` to resume failed stages by default | `false` |
| `NEXO_PIPELINE_ALLOW_MISSING_RESUME_SOURCE` | `1`/`true` to continue when source run is missing | `false` |
| `NEXO_PIPELINE_ENABLE_TEST_HOOKS` | Enables deterministic failure/test hooks for gate scenarios | `false` |
| `NEXO_PIPELINE_COMPLETION_POLICY` | Completion policy override (for example `AllowNonCriticalStageFailures`) | `Strict` |
| `NEXO_PIPELINE_STORE_PROVIDER` | Pipeline run store provider (for example `LiteDb`) | in-memory |
| `NEXO_PIPELINE_STORE_PATH` | Store path when using file-backed providers | unset |
| `NEXO_PIPELINE_DETERMINISTIC_ADAPTER` | Override deterministic adapter identifier | framework default |
| `NEXO_PIPELINE_AGENTIC_ADAPTER` | Override agentic adapter identifier | framework default |

## OpenAI

| Variable | Description | Default |
|----------|-------------|---------|
| `OPENAI_API_KEY` | API key | required for `openai` provider |
| `OPENAI_MODEL` | Model name | `gpt-4o-mini` |
| `OPENAI_BASE_URL` | Chat completions URL or API root (`https://api.openai.com`, `https://api.openai.com/v1`, or full `.../v1/chat/completions`); normalized to `POST .../v1/chat/completions` | `https://api.openai.com/v1/chat/completions` |
| `OPENAI_VISION_MODEL` | Vision model | `OPENAI_MODEL` |

## OpenAI-compatible (`openai_compat` provider)

Use **`openai_compat`** when the backend implements OpenAI-style **`POST /v1/chat/completions`** with **`Authorization: Bearer`** (vLLM, LiteLLM, llama.cpp server, etc.). Separate from **`openai`** so local gateways do not overwrite official OpenAI env vars.

| Variable | Description | Default |
|----------|-------------|---------|
| `OPENAI_COMPAT_API_KEY` | Bearer token (required for this provider; use a placeholder if the server ignores auth) | unset |
| `OPENAI_COMPAT_BASE_URL` | Origin (`http://127.0.0.1:8000`), API root (`.../v1`), or full completions URL; normalized like `OPENAI_BASE_URL` | unset |
| `OPENAI_COMPAT_MODEL` | Text model id | `default` (see `NexoDefaults.OpenAiCompatDefaultModel`) |
| `OPENAI_COMPAT_VISION_MODEL` | Vision model when `OPENAI_COMPAT_MODEL` is not suitable for image input | `OPENAI_COMPAT_MODEL` |

Per-request **`config.model`** overrides the vision model when set.

## Azure OpenAI

| Variable | Description | Default |
|----------|-------------|---------|
| `AZURE_OPENAI_ENDPOINT` | Endpoint URL | required |
| `AZURE_OPENAI_API_KEY` | API key | required |
| `AZURE_OPENAI_DEPLOYMENT` | Deployment name | required |
| `AZURE_OPENAI_API_VERSION` | API version | `2024-06-01` |

## Ollama

Three code paths reach Ollama — the default MEAI model path, the NCR serving backend, and the legacy provider factory — and each has its own key family. They all resolve in the same order, so setting the legacy pair alone is enough on a single host; inside Compose set all three (the shipped stacks do) so no path can fall back to `localhost:11434`, which is the container itself.

**Resolution order** (first non-empty value wins): `NEXO_OLLAMA_BASE_URL` / `NEXO_OLLAMA_MODEL` env → the path's own config key (`Nexo:Meai:*` or `Nexo:NodeCapabilityRuntime:Ollama:BaseUrl`) → legacy `OLLAMA_BASE_URL` / `OLLAMA_MODEL` env → the path's default (`http://localhost:11434` / `llama3.1:latest`; NCR uses `http://127.0.0.1:11434`). Blank values are treated as unset.

| Variable / Key | Path | Description | Default |
|----------------|------|-------------|---------|
| `NEXO_USE_MEAI_PIPELINE` (`Nexo:UseMeaiPipeline`) | selector | `0`/`false` opts out of the default Microsoft.Extensions.AI (MEAI) model path and uses the legacy provider factory instead. Default: on. | on |
| `NEXO_OLLAMA_BASE_URL` | MEAI + NCR | Highest-precedence Ollama base URL override. | unset |
| `NEXO_OLLAMA_MODEL` | MEAI | Highest-precedence Ollama model override. | unset |
| `Nexo:Meai:OllamaBaseUrl` (`Nexo__Meai__OllamaBaseUrl`) | MEAI | Base URL for the default `IModel` / `IChatClient` path (`local:ollama` target). | falls through |
| `Nexo:Meai:OllamaModel` (`Nexo__Meai__OllamaModel`) | MEAI | Model for the default `IModel` / `IChatClient` path. | falls through |
| `Nexo:NodeCapabilityRuntime:Ollama:BaseUrl` (`Nexo__NodeCapabilityRuntime__Ollama__BaseUrl`) | NCR | Base URL for the NCR serving backend, startup health probe and `/api/tags` model catalog (see below). | falls through |
| `OLLAMA_BASE_URL` | all (legacy) | Base URL. The provider-factory path (`provider=ollama`, `/api/onboarding/status`, IDE endpoints) reads **only** this family; MEAI and NCR fall back to it. | `http://localhost:11434` |
| `OLLAMA_MODEL` | all (legacy) | Text model. | `llama3.1:latest` |
| `OLLAMA_VISION_MODEL` | provider factory | Vision model | `richardyoung/smolvlm2-2.2b-instruct` |
| `OLLAMA_TIMEOUT_SECONDS` | provider factory | Request timeout | `300` |

`GET /api/onboarding/status` reports `meaiOllamaBaseUrl` / `meaiOllamaModel` (what the default model path will dial) next to the provider-factory `ollama` availability, and `scripts/prod-dry-run.sh` fails when the former is a loopback address inside Compose.

**Docker (models in containers):** `docker compose -f deploy/compose/docker-compose.ollama.yml up -d`, then `scripts/run-ollama-docker.ps1` / `scripts/run-ollama-docker.sh` to pull a tag. **Host-run Nexo.API with Ollama in Docker (all platforms):** `scripts/start-nexo-api-dev.ps1` or `scripts/start-nexo-api-dev.sh` (waits for Ollama, sets `OLLAMA_*` + NCR URL, runs `dotnet run`). Use `-Pull` / `--pull` when the model is not yet local. **Phone / another device on the same LAN:** `-ListenLan` / `--listen-lan` binds `http://0.0.0.0:<port>`; browse `http://<host-LAN-IP>:8080` and allow the port in the host firewall. Default bind is loopback-only (`127.0.0.1`). Stop: `scripts/stop-nexo-api-dev.ps1` / `.sh`.

### Node Capability Runtime (NCR) Ollama

Desktop NCR uses its own options-bound Ollama endpoint for model serving.

| Key / Variable | Description | Default |
|----------------|-------------|---------|
| `Nexo:NodeCapabilityRuntime:Ollama:BaseUrl` (`Nexo__NodeCapabilityRuntime__Ollama__BaseUrl`) | NCR Ollama backend base URL used by desktop policy registrations. Overridden by `NEXO_OLLAMA_BASE_URL`; falls back to legacy `OLLAMA_BASE_URL` when unset (see resolution order above). | `http://127.0.0.1:11434` |

Behavior notes:
- On startup, NCR runs a health probe against the configured Ollama backend and logs a degraded warning if unreachable.
- A degraded startup does not crash the host; agentic tasks may escalate until Ollama becomes reachable.
- NCR records metrics for model resolution, model load, and Ollama endpoint latencies/error rates via `IMetricsCollector` keys under `ncr.*`.

### NCR Capability Freshness

Remote brick catalogs now use an in-memory stale capability snapshot fallback:
- Fresh `/api/capabilities` responses are cached per remote base URL.
- If a later capability fetch fails, the last known manifest is reused and marked stale internally.
- Consumers should treat stale manifests as routing hints (not hard guarantees), and retry capability refresh periodically.
- `Nexo:Execution:RemoteCapabilities:MaxStaleAge` (`Nexo__Execution__RemoteCapabilities__MaxStaleAge`) bounds stale fallback age (default `00:10:00`). If stale data exceeds this age, fallback is rejected.

### NCR Telemetry SLO Suggestions (v1)

Suggested starting SLOs/alerts using `ncr.*` metrics:
- `ncr.model_resolution.target.Escalate`: alert if escalation ratio > 20% over 15 minutes for user-facing workloads.
- `ncr.model_load.error` and `ncr.ollama.*.error`: alert on sustained non-zero error rate over 5 minutes.
- `ncr.ollama.chat.duration`: track p95/p99; alert if p95 exceeds your interactive budget for 10+ minutes.
- `ncr.profile.constraint_change`: watch for bursty spikes that correlate with thermal/memory pressure and escalation increases.

Operational guidance:
- Treat stale capability fallback as degraded mode; prefer conservative routing and periodic refresh attempts.
- Set `NEXO_OBSERVATION_FAIL_OPEN=1` for production-style hosts that must continue serving even if observation store permissions are restricted.

## Video (SmolVLM2)

| Variable | Description | Default |
|----------|-------------|---------|
| `VIDEO_SERVICE_URL` | Video analysis service URL | required for `video` provider |

## Trust & Audit

| Variable | Description | Default |
|----------|-------------|---------|
| `NEXO_TRUST_AUDIT_DB` | Path to LiteDB audit log | in-memory |
| `NEXO_KNOWLEDGE_LOG_PATH` | Path to user knowledge log | in-memory |
| `NEXO_ACCESS_BOUNDARY_CONFIG` | Path to access boundary JSON | unset |
| `NEXO_TRUST_POLICY_PACKS_PATH` | Directory containing trust policy pack JSON files | `config/trust-packs` (repo root) |
| `NEXO_ACTIVE_TRUST_POLICY_PACK_PATH` | Path to active pack selection file | `active-pack.json` in packs dir |

## Mesh

For a **layered breakdown** of mesh capabilities (identity, registry, transport, trust, request/fulfill, sync, WAN gaps), see [`MeshAgentSetupCapabilityBreakdown.md`](./MeshAgentSetupCapabilityBreakdown.md).

| Variable | Description | Default |
|----------|-------------|---------|
| `NEXO_MESH_PEER_ID` | Mesh peer identifier | random GUID |
| `NEXO_MESH_INSTANCES_PATH` | Path to file-based mesh instance registry | unset |
| `NEXO_TRUSTED_PEER_IDS` | Comma-separated peer IDs trusted for execution | unset (all peers trusted) |
| `NEXO_UNTRUSTED_PEER_IDS` | Comma-separated peer IDs blocked from execution | unset |
| `NEXO_MESH_TRUST_POLICY` | Mesh trust policy (`open`, `allowlist`, `denylist`) | `open` |
| `NEXO_PEER_TRUST_POLICY` | Per-peer trust policy override | unset |
| `NEXO_SHARED_ADAPTATIONS_PATH` | Path for shared adaptation artifacts across mesh | unset |

## RunPod + Capability Routing (`Nexo:RunPod:*`)

Generation execution routing uses NCR + peer network + RunPod cloud. These options are bound from `Nexo:RunPod:*` and can be set with environment variables (`__` separator).

| Key / Variable | Description | Default |
|----------------|-------------|---------|
| `Nexo:RunPod:ApiKey` (`Nexo__RunPod__ApiKey`) | RunPod API key | empty |
| `Nexo:RunPod:BaseUrl` (`Nexo__RunPod__BaseUrl`) | RunPod API base URL | `https://api.runpod.io` |
| `Nexo:RunPod:PreferredGpuTier` (`Nexo__RunPod__PreferredGpuTier`) | Preferred GPU tier for cloud jobs | `NVIDIA_A4000` |
| `Nexo:RunPod:Timeout` (`Nexo__RunPod__Timeout`) | Max remote job execution duration before timeout/teardown | `00:10:00` |
| `Nexo:RunPod:PollingInterval` (`Nexo__RunPod__PollingInterval`) | RunPod status polling interval | `00:00:02` |
| `Nexo:RunPod:OutputStagingPath` (`Nexo__RunPod__OutputStagingPath`) | Staged output path for remote artifacts | temp path (`nexo-runpod`) |
| `Nexo:RunPod:QueueDepthThreshold` (`Nexo__RunPod__QueueDepthThreshold`) | Local queue threshold before remote routing | `4` |
| `Nexo:RunPod:EnablePeerNetworkRouting` (`Nexo__RunPod__EnablePeerNetworkRouting`) | Enables routing to peer Nexo nodes | `false` |
| `Nexo:RunPod:PreferPeerNetworkOverCloud` (`Nexo__RunPod__PreferPeerNetworkOverCloud`) | System default preference when remote routing is required | `true` |
| `Nexo:RunPod:PeerCapabilityId` (`Nexo__RunPod__PeerCapabilityId`) | Capability identifier required for peer eligibility | `generation.capability-routing` |
| `Nexo:RunPod:PeerRoutingBrickId` (`Nexo__RunPod__PeerRoutingBrickId`) | Brick id invoked on peer nodes | `generation.capability-routing` |
| `Nexo:RunPod:PeerRequestTimeout` (`Nexo__RunPod__PeerRequestTimeout`) | Per-peer request timeout before failover | `00:00:30` |
| `Nexo:RunPod:PeerDiscoveryInterval` (`Nexo__RunPod__PeerDiscoveryInterval`) | Peer capability snapshot refresh interval | `00:00:10` |

Routing behavior:
- `CapabilityRoutingBrick` is the default generation entry point.
- `RemoteExecutionPreference` (job-level) can force or prefer peer/cloud routing (`UseSystemDefault`, `CloudOnly`, `PreferPeerNetwork`, `PeerNetworkOnly`).
- Peer execution includes candidate ranking, timeout handling, and failover across eligible peers.

See `docs/runtime/ExecutionRouting.md` for detailed execution flow and resilience behavior.

## Ephemeral Execution

| Variable | Description | Default |
|----------|-------------|---------|
| `NEXO_EPHEMERAL` | `1` = enable ephemeral models (Ollama in container) when supported | unset |
| `NEXO_EPHEMERAL_MODELS` | `1` = use ephemeral Ollama container for LLM; container removed when session ends | unset |
| `NEXO_EPHEMERAL_DB` | `postgres` = use ephemeral Postgres container for workflows/tests | unset |
| `NEXO_TEST_EPHEMERAL` | `1` = run tests in ephemeral containers (no volume mounts) | unset |

## Artifact Cleanup

| Variable | Description | Default |
|----------|-------------|---------|
| `NEXO_CLEAN_BEFORE_TEST` | `1` = run test-artifacts cleanup before `nexo test local` | unset |
| `NEXO_CLEAN_AFTER_TEST` | `1` = run test-artifacts cleanup after `nexo test local` | unset |
| `NEXO_ARTIFACT_CLEANUP_REPO_ROOT` | Repo root for cleanup; unset = auto-detect | unset |
| `NEXO_INCOMPLETE_BLOB_PATH` | Path to content-addressed blob storage for `incomplete-blobs` strategy | unset |
| `NEXO_BLOB_LIFECYCLE` | `docker` = pause Docker Desktop before incomplete-blob cleanup | unset |

## Background Agents

| Variable | Description | Default |
|----------|-------------|---------|
| `NEXO_BACKGROUND_AGENTS_CONFIG` | Path to background agent set JSON configuration | unset |
| `NEXO_AGENT_MODE_PATH` | Path to file-based aggressiveness mode store | unset |
| `NEXO_EXTENSION_MAX_LINEAGE_DEPTH` | Extender ceiling (SX-AUDIT invariant D): max `ParentId` hops below a human-authored root an extender may sit and still extend. May only LOWER the built-in default. | 1 |
| `NEXO_EXTENSION_MAX_UNATTENDED_CYCLES` | Extender ceiling: extend cycles since a human last armed the agent before it holds (re-arm: restart or `RearmExtension`). May only LOWER the default. | 8 |
| `NEXO_EXTENSION_MAX_CYCLES_PER_HOUR` | Extender ceiling: extend cycles in any trailing hour. May only LOWER the default. | 4 |
| `NEXO_OBSERVATION_DEGRADED_MODE` | `1` = start observation pipeline in degraded mode | unset |
| `NEXO_OBSERVATION_FAIL_OPEN` | `1` = observation pipeline continues on store errors | unset |
| `BING_SEARCH_KEY` | API key for Bing web search provider | unset (falls back to mock) |

## Autonomy sessions (`Nexo__Autonomy__*`)

Bound from the `Nexo:Autonomy` section (see `NexoAutonomyOptions` for the full set; the loop is off unless `Enabled=true`).

| Variable / config key | Description | Default |
|-----------------------|-------------|---------|
| `Nexo__Autonomy__SessionImage` | Container image proposal sessions start from; required when `UseSandboxSessions=true`. Must already be present on the engine — sessions run with `--pull never` and never fetch an image | unset |
| `Nexo__Autonomy__SessionImageDigest` | Optional pin on the session image's identity: the engine image ID (`sha256:…`) that `SessionImage` must resolve to — the same value attestation records and certificates carry as their `image-digest` input, so pin by copying it from a certificate you have read. When set, a session whose image resolves to anything else refuses to start (checked before `docker run` and again at attestation); a value not of the form `sha256:…` fails validation at boot | unset (capture only) |

## Barriers (`Nexo__Barriers__*`)

Bound from the `Nexo:Barriers` section (`appsettings.json` or `Nexo__Barriers__*` environment variables).

| Variable | Description | Default |
|----------|-------------|---------|
| `Nexo__Barriers__Levels__0`, `__1`, … | Ordered barrier levels, lowest (floor) to highest | `public, internal, confidential, restricted` in `Nexo.API/appsettings.json` |
| `Nexo__Barriers__RequireExplicitBarrier` | `true` = an agent invocation with no explicit barrier context is refused and surfaces as escalation / `errorCode` `BARRIER_CONTEXT_MISSING` (the response says why `0 agent(s) executed`); `false` = missing context defaults to the floor level and records a `DefaultApplied` audit event | `false` (code default, `Nexo.API/appsettings.json`, agent-server compose, CLI daemon) |
| `Nexo__Barriers__HostCeiling` | Highest level this host may process; unset disables the ceiling check | `confidential` in `Nexo.API/appsettings.json` |

`Nexo.API` does not register an HTTP barrier-context middleware, so its requests never carry an explicit barrier context: leave `RequireExplicitBarrier` at `false` there, or opt in only from a host that establishes the context itself (`IBarrierContextAccessor.Initialize`, e.g. `nexo orchestrate --barrier <level>`). `HttpBarrierContextMiddleware` (gated by `NEXO_BARRIER_MIDDLEWARE_ENABLED`) exists in `Nexo.Runtime` but is not wired into any shipped host.

## Routing & Execution

| Variable | Description | Default |
|----------|-------------|---------|
| `NEXO_ALLOW_MOCK` | `1` = enable mock/offline/mock-json/echo providers | unset |
| `NEXO_LOCAL_MODEL_PATH` | Path to local ONNX/LLamaSharp model for `local` provider | unset |
| `NEXO_LOCAL_QUEUE_DEPTH` | Local execution queue depth for routing decisions | unset (auto) |
| `NEXO_GPU_COMPUTE_CLASS` | GPU compute class label for NCR capability matching | unset |
| `NEXO_LOAD_PREFERENCE` | Default load balancing preference | unset |
| `NEXO_EXECUTION_REMOTE_URL` | Remote execution endpoint URL for hosting | unset |

## Config File

`~/.nexo/config.json` (or path from `NEXO_CONFIG_PATH`):

```json
{
  "provider": "openai",
  "model": "gpt-4o-mini"
}
```

- `provider`: `openai`, `azure`, `ollama`, `local`, `video`, `mock`, `offline`, `mock-json`, `echo` (mock variants require `NEXO_ALLOW_MOCK=1`)
- `model`: override for the selected provider
