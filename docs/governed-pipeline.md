# Governed MEAI pipeline

Nexo’s default model path is **Microsoft.Extensions.AI (MEAI)** with a fixed governance stack.
Legacy `IProviderFactory` chat via `ProviderBackedModel` remains available as an **opt-out** only.

## Feature flag

| Source | Enable | Disable |
|--------|--------|---------|
| Default (Phase 6+) | **on** | — |
| Config | `Nexo:UseMeaiPipeline=true` | `false` / `0` |
| Env | `NEXO_USE_MEAI_PIPELINE=1\|true` | `0\|false` |
| Hosting | `NexoHostingOptions.UseMeaiPipeline = true` | `= false` |

When enabled, Hosting Phase 13b registers keyed/routed clients and Phase 13 uses `MeaiBackedModel` as the agentic leaf under `HotSwappableModel`.

## Stack order (do not reorder)

Per-target clients use `UseNexoGovernance(targetKey)`:

1. **PolicyGate** — allow/deny target (local allowed; cloud deny unless allow-listed)
2. **Sanitizing** — PII/secret disposition per target (`Pass` local; `BlockOnSecretRedactOnPii` cloud)
3. **Auditing** — counts/categories/latency only; never content
4. **Provider** — Ollama HTTP, LLamaSharp (`local:onnx`), or Bedrock MEAI

The **router** (`RoutingChatClient`) sits outside per-target stacks and is itself wrapped in `AuditingChatClient` (`router:default`).

Embeddings use the same AsyncLocal-aware nesting: **Sanitizing → Auditing → generator**.

## Target keys

| Key | Role |
|-----|------|
| `local:ollama` | Local Ollama HTTP |
| `local:onnx` | Local LLamaSharp GGUF (product key; not ONNX Runtime) |
| `cloud:bedrock:fast\|balanced\|heavy` | AWS Bedrock (policy allow-listed when Bedrock enabled) |

Capability hint: `ChatOptions.AdditionalProperties["nexo.route.capability"]` = `fast` / `balanced` / `heavy`.

Raw provider types (`OllamaHttpChatClient`, Bedrock SDK client, LLamaSharp session) are **never** registered in DI.

## RAG

- Default `IRAGService` → `MeaiVectorDataRagAdapter` over `VectorDataRagService` (in-process VectorData store + governed embeddings).
- Reindex CLI: `nexo background-agent rag reindex-meai <paths…>`
- Legacy `RAGService` / store types remain in-tree for opt-out / migration but are not the Hosting default.

## Operator notes

- Bedrock: `Nexo:Meai:Bedrock:Enabled` + region/model ids; credentials via the default AWS chain (same as DynamoDB ingress).
- Opting out restores `ProviderBackedModel` for `IModel` but keeps VectorData `IRAGService` unless Hosting phases are customized.
- Architecture tests: `make meai-pipeline-gate` (`src/Nexo.Tests.AI.Pipeline`).

See also: `docs/meai-migration-notes.md`.
