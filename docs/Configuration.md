# Configuration Reference

Nexo configures via environment variables and optional `~/.nexo/config.json`. This document lists the primary configuration options. Additional options may be available via `appsettings.json` binding — see inline code comments for the full set.

## Core

| Variable | Description | Default |
|----------|-------------|---------|
| `NEXO_CONFIG_PATH` | Path to config file | `~/.nexo/config.json` |
| `NEXO_MESH_INSTANCES_PATH` | Path to **`instances.json`** for **`nexo mesh`** discovery | `~/.nexo/instances.json` |
| `NEXO_MESH_TRUST_POLICY` | Discovery filter: **`any`**, **`allowlist`** (only **`admitted: true`** peers), **`trusted-only`**, **`trusted-preferred`** | **`any`** for discovery (see **`MeshTrustPolicyConfiguration`**) |
| `NEXO_MESH_DIRECTOR_BASE_URL` | Base URL for **`nexo mesh director`** HTTP calls | unset |
| `NEXO_MESH_API_KEY` | Optional **`X-Nexo-Api-Key`** for director CLI | unset |
| `NEXO_MESH_MUTATING_TOKEN` | Optional **`X-Nexo-Mesh-Token`** for mutating mesh routes on the hub | unset |
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

## Centralized Defaults (`NexoDefaults`)

All tunable constants are centralized in `Nexo.Core.Domain.NexoDefaults`. This eliminates hard-coded magic numbers scattered across the codebase. Override any value via environment variables or `appsettings.json` — keys are listed in the relevant sections below.

## Nexo.API exposure (`Nexo__Security__*`)

Advisory only — does not enforce firewalls or Tailscale ACLs. See **`docs/TailscaleAndNexo.md`** and **`docs/config/security-exposure.env.example`**.

| Variable | Description | Default |
|----------|-------------|---------|
| `Nexo__Security__ExposureProfile` | `Localhost`, `Lan`, `Tailnet`, or `Public` (case-insensitive) | `Localhost` in `appsettings.json` |
| `Nexo__Security__CustomAdvisory` | Optional extra line shown in the Director portal advisory | unset |
| `Nexo__Security__ShowAdvisoryInPortal` | `true` / `false` — show advisory banner in portal | `true` |
| `Nexo__Security__RequireApiKeyForMutatingEndpoints` | `true` / `false` — enforce API key checks for POST/PUT/PATCH/DELETE under `/api/*` | `false` |
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
- `RequireApiKeyForMutatingEndpoints` remains for backward compatibility with existing deployments.

## Mesh director CLI (`NEXO_MESH_*`, Phase 7)

Used by **`nexo mesh director`** when a worker or script talks to a remote **`Nexo.API`** mesh control plane. See **`docs/MeshPhase7EdgeAlignment.md`**.

| Variable | Description |
|----------|-------------|
| `NEXO_MESH_DIRECTOR_BASE_URL` | Director base URL (e.g. `https://hub:8080`) |
| `NEXO_MESH_API_KEY` | Optional **`X-Nexo-Api-Key`** when the hub enforces API key auth |
| `NEXO_MESH_MUTATING_TOKEN` | Optional **`X-Nexo-Mesh-Token`** for mutating **`/api/mesh`** requests |

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

| Variable | Description | Default |
|----------|-------------|---------|
| `OLLAMA_BASE_URL` | Base URL | `http://localhost:11434` |
| `OLLAMA_MODEL` | Text model | `llama3.1:latest` |
| `OLLAMA_VISION_MODEL` | Vision model | `richardyoung/smolvlm2-2.2b-instruct` |
| `OLLAMA_TIMEOUT_SECONDS` | Request timeout | `300` |

**Docker (models in containers):** `docker compose -f docker-compose.ollama.yml up -d`, then `scripts/run-ollama-docker.ps1` / `scripts/run-ollama-docker.sh` to pull a tag. **Host-run Nexo.API with Ollama in Docker (all platforms):** `scripts/start-nexo-api-dev.ps1` or `scripts/start-nexo-api-dev.sh` (waits for Ollama, sets `OLLAMA_*` + NCR URL, runs `dotnet run`). Use `-Pull` / `--pull` when the model is not yet local. **Phone / another device on the same LAN:** `-ListenLan` / `--listen-lan` binds `http://0.0.0.0:<port>`; browse `http://<host-LAN-IP>:8080` and allow the port in the host firewall. Default bind is loopback-only (`127.0.0.1`). Stop: `scripts/stop-nexo-api-dev.ps1` / `.sh`.

### Node Capability Runtime (NCR) Ollama

Desktop NCR uses its own options-bound Ollama endpoint for model serving.

| Key / Variable | Description | Default |
|----------------|-------------|---------|
| `Nexo:NodeCapabilityRuntime:Ollama:BaseUrl` (`Nexo__NodeCapabilityRuntime__Ollama__BaseUrl`) | NCR Ollama backend base URL used by desktop policy registrations | `http://127.0.0.1:11434` |

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
| `NEXO_OBSERVATION_DEGRADED_MODE` | `1` = start observation pipeline in degraded mode | unset |
| `NEXO_OBSERVATION_FAIL_OPEN` | `1` = observation pipeline continues on store errors | unset |
| `NEXO_BARRIER_MIDDLEWARE_ENABLED` | `1` = enable HTTP barrier context middleware | unset |
| `BING_SEARCH_KEY` | API key for Bing web search provider | unset (falls back to mock) |

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
