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

### Node Capability Runtime (NCR) Ollama

Desktop NCR uses its own options-bound Ollama endpoint for model serving.

| Key / Variable | Description | Default |
|----------------|-------------|---------|
| `Nexo:NodeCapabilityRuntime:Ollama:BaseUrl` (`Nexo__NodeCapabilityRuntime__Ollama__BaseUrl`) | NCR Ollama backend base URL used by desktop policy registrations | `http://127.0.0.1:11434` |

Behavior notes:
- On startup, NCR runs a health probe against the configured Ollama backend and logs a degraded warning if unreachable.
- A degraded startup does not crash the host; agentic tasks may escalate until Ollama becomes reachable.

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
