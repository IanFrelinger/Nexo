# Configuration Reference

Nexo configures via environment variables and optional `~/.nexo/config.json`. This document lists all supported options.

## Core

| Variable | Description | Default |
|----------|-------------|---------|
| `NEXO_CONFIG_PATH` | Path to config file | `~/.nexo/config.json` |
| `NEXO_AIRGAP` | `1` or `true` = air-gapped; no cloud calls | unset |
| `NEXO_AIRGAP_PROBE` | `1` = probe network to detect air-gap | unset |
| `NEXO_TRUST_ENABLED` | `1` = enable Trust & sanitization | `false` |
| `NEXO_MODEL_PROVIDER` | Default LLM provider | from config |
| `NEXO_LOOP_PARALLEL` | `1` = parallel loop kernel | `false` |
| `NEXO_LOOP_INSTRUMENT` | `1` = instrumented loop | `false` |
| `NEXO_LLM_RETRY_COUNT` | Retries for cloud LLM (5xx/429) | `3` |

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
| `OPENAI_BASE_URL` | Base URL | `https://api.openai.com/v1/chat/completions` |
| `OPENAI_VISION_MODEL` | Vision model | `OPENAI_MODEL` |

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
| `OLLAMA_MODEL` | Text model | `llama3.1` |
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
- Keep `NEXO_ENDPOINT_HEALTH_DEGRADED_LOG_THRESHOLD` > 1 in noisy environments to reduce transient probe warning noise.
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

## Mesh

| Variable | Description | Default |
|----------|-------------|---------|
| `NEXO_MESH_PEER_ID` | Mesh peer identifier | random GUID |

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

## Config File

`~/.nexo/config.json` (or path from `NEXO_CONFIG_PATH`):

```json
{
  "provider": "openai",
  "model": "gpt-4o-mini"
}
```

- `provider`: `mock`, `offline`, `openai`, `azure`, `ollama`, `video`
- `model`: override for the selected provider
