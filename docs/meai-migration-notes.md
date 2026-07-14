# MEAI Migration Notes — Phase 0 Discovery

**Status:** Phase 0 complete (read-only discovery)  
**Date:** 2026-07-14  
**Repo TFM today:** host/library projects are **`net8.0`** (SDK pinned to `9.0.100` in `global.json`). Plan asks for **.NET 9**; Phase 1 should introduce `Nexo.AI.Pipeline` as `net9.0` (or dual-target) and confirm host upgrade scope separately.  
**MEAI today:** **none** — no `Microsoft.Extensions.AI*` packages, no `IChatClient`.

This document is the map for Phases 1–6. Later phases must update this file when discoveries invalidate assumptions.

---

## 1. Model invocation today

### Primary seam: `IModel` → `IProviderFactory`

```
Agents / ToolCallingAgent / Orchestration
        │
        ▼
   IModel.CompleteAsync(ModelInput)
        │
        OrchestrationRuntimeModelDecorator
          → HotSwappableModel
            → ProviderBackedModel
        │
        ▼
   IProviderFactory.ExecuteLLMAsync / Vision / Video
        │
        ├── openai / openai_compat / azure  → HttpClient chat completions
        ├── ollama                          → OllamaProvider → POST /api/chat
        ├── local                           → LocalModelProvider (LLamaSharp + GGUF)
        ├── video                           → VIDEO_SERVICE_URL HTTP
        └── mock / offline / echo           → MockScaffoldingResponder
```

| Type | Path | Role |
|------|------|------|
| `IModel` | `src/Nexo.Abstractions/IModel.cs` | Agent-facing completion API |
| `ModelInput` / `ModelOutput` | `src/Nexo.Abstractions/ModelInput.cs`, `ModelOutput.cs` | Message / completion DTOs |
| `IProviderFactory` | `src/Nexo.Infrastructure/Execution/IProviderFactory.cs` | Gateway for all provider HTTP / local calls |
| `ProviderFactory` | `src/Nexo.Infrastructure/Execution/ProviderFactory.cs` | **Central invoker** — OpenAI, Azure, openai_compat, Ollama, local, video, mock |
| `ProviderBackedModel` | `src/Nexo.Infrastructure/Execution/Models/ProviderBackedModel.cs` | `IModel` → parses `nexo.model.provider=` / `nexo.model.name=` → factory |
| `HotSwappableModel` | `src/Nexo.Infrastructure/Execution/Models/HotSwappableModel.cs` | Runtime swap; respects `NEXO_MODEL_PROVIDER` |
| `OrchestrationRuntimeModelDecorator` | `src/Nexo.Orchestration/Models/OrchestrationRuntimeModelDecorator.cs` | Outer `IModel`; injects orchestration runtime spec |
| `OrchestrationHotSwappableModel` | `src/Nexo.Orchestration/Models/OrchestrationHotSwappableModel.cs` | Primary/fallback swap (orchestration layer) |
| `AgentScopedModel` | `src/Nexo.Orchestration/Models/AgentScopedModel.cs` | Per-agent provider/name directives |
| `OllamaProvider` | `src/Nexo.Infrastructure/Execution/Ollama/OllamaProvider.cs` | Ollama HTTP client (`/api/chat`, health, tags) |
| `LocalModelProvider` | `src/Nexo.Infrastructure/Execution/LocalModelProvider.cs` | **In-process LLamaSharp GGUF** (`NEXO_LOCAL_MODEL_PATH`) — **not ONNX Runtime** |
| `OpenAiCompatibleEndpoint` | `src/Nexo.Infrastructure/Execution/OpenAiCompatibleEndpoint.cs` | URL normalization for `/v1/chat/completions` |
| `AdaptiveProviderFactory` | `src/Nexo.Infrastructure/Execution/AdaptiveProviderFactory.cs` | Chooses provider via `ILoadPolicy` |
| `PreferenceLoadPolicy` / `ILoadPolicy` | `src/Nexo.Infrastructure/Execution/LoadPolicy/` | Local-vs-cloud preference (`NEXO_LOAD_PREFERENCE`) |
| `MockScaffoldingResponder` | `src/Nexo.Infrastructure/Execution/MockScaffoldingResponder.cs` | Deterministic mock/offline responses |
| `OllamaEphemeralLifecycle` | `src/Nexo.Infrastructure/Execution/Ephemeral/OllamaEphemeralLifecycle.cs` | Ephemeral Docker Ollama per session |

**Important correction vs plan wording:** “ONNX / offline target” in docs/`BackendType.OnnxRuntime` is largely a **placeholder**. Real offline inference is **LLamaSharp + GGUF** via `LocalModelProvider`. Phase 1 should wrap **`local` (LLamaSharp)** as `local:onnx` *only if* product naming requires that key; prefer key **`local:llamasharp`** (or map `local:onnx` → LLamaSharp with a comment) so policy docs stay honest.

### Secondary seam: NCR `IModelServingBackend`

| Type | Path | Role |
|------|------|------|
| `IModelServingBackend` | `src/Nexo.Core.Application/NodeCapabilityRuntime/Ports/IModelServingBackend.cs` | `RunInferenceAsync`, load/unload/pull |
| `OllamaModelServingBackend` | `src/Nexo.Infrastructure/NodeCapabilityRuntime/Backends/OllamaModelServingBackend.cs` | Desktop NCR → Ollama HTTP |
| `NullModelServingBackend` | `src/Nexo.Infrastructure/NodeCapabilityRuntime/Backends/NullModelServingBackend.cs` | No-op; reports `BackendType.OnnxRuntime` |
| `BackendType` | `src/Nexo.Core.Application/NodeCapabilityRuntime/Models/BackendType.cs` | `Ollama`, `LlamaCppMobile`, `OnnxRuntime` (only Ollama implemented) |
| `NodeCapabilityRuntime` | `src/Nexo.Infrastructure/NodeCapabilityRuntime/NodeCapabilityRuntime.cs` | Model selection / ensure-ready |
| `NcrAgenticBrickEngine` | `src/Nexo.Infrastructure/Execution/Agentic/NcrAgenticBrickEngine.cs` | Agentic bricks → NCR lifecycle |

### Capability / job routing (not chat-client routing)

| Type | Path | Role |
|------|------|------|
| `ExecutionTarget` | `src/Nexo.Core.Application/Execution/Routing/ExecutionTarget.cs` | Local vs remote job target |
| `ICapabilityRouter` / `NcrCapabilityRouter` | Application port + `src/Nexo.Infrastructure/Execution/Routing/NcrCapabilityRouter.cs` | Local / peer / RunPod |
| `ProviderFactoryLocalExecutor` | `src/Nexo.Infrastructure/Execution/Routing/ProviderFactoryLocalExecutor.cs` | Local jobs → `ExecuteLLMAsync` |
| `IEndpointRouter` / `CompositeEndpointRouter` | Abstractions + Orchestration | Agent **transport** endpoints (not LLM providers) |

### Direct `IProviderFactory` callers (bypass `IModel`)

| Type | Path |
|------|------|
| `ProviderGeneratorModel` | `src/Nexo.Infrastructure/Adaptation/Generation/ProviderGeneratorModel.cs` |
| `ProviderCompositionGeneratorModel` | `src/Nexo.Infrastructure/Certification/Composition/ProviderCompositionGeneratorModel.cs` |
| `ContentGenerator` | `src/Nexo.Infrastructure/Export/ContentGenerator.cs` |
| `OWASPScannerBrick` | `src/Nexo.Bricks.Owasp/Security/OWASPScannerBrick.cs` |
| `ProviderFactoryLocalExecutor` | (above) |

### Explicitly absent

| Search | Result |
|--------|--------|
| AWS Bedrock | **Zero** code references |
| `Microsoft.Extensions.AI` / `IChatClient` | **Zero** |
| Anthropic HTTP | Domain enum only; not in `ProviderFactory` |
| Real ONNX Runtime GenAI | Enum/placeholder only |

---

## 2. Sanitization / PII / secret filtering

| Type | Path | Role |
|------|------|------|
| `ICloudSanitizationProxy` / `CloudSanitizationProxy` | `src/Nexo.BackgroundAgents/Trust/` | Sanitizes outbound prompts before cloud; PII block/redact + taxonomy |
| `SanitizingProviderFactory` | `src/Nexo.BackgroundAgents/Trust/SanitizingProviderFactory.cs` | **`IProviderFactory` decorator** — runs proxy then delegates |
| `OutgoingContext` / `SanitizationResult` | same folder | Input/result models |
| `SanitizationAuditEntry` (+ DTO) | Trust + `src/Nexo.Core.Application/Trust/Ports/` | Redaction audit row (counts/categories — no raw secrets by design) |
| `ISanitizationAuditLog` | Trust | `LogRedaction` / `GetRecent` |
| `ISensitiveContentFilter` / `SensitiveContentFilter` | `src/Nexo.BackgroundAgents/WebSearch/` | Regex email/phone/SSN/API-key/CC; `RedactPii` / `ShouldBlockQuery` |
| `IDataTaxonomy` / `DataTaxonomy` + JSON | `src/Nexo.BackgroundAgents/DataSensitivity/` | Data-type → sensitivity (e.g. api-keys → Secret) |
| `IDataSensitivityRegistry` / levels | same | Public→TopSecret; drives exfiltration + RAG filters |
| `DataExfiltrationPolicy` | `src/Nexo.BackgroundAgents/Security/DataExfiltrationPolicy.cs` | Tool-call policy: blocks LLM/search when sensitivity forbids |
| `SupportDiagnosticsExporter` | `application/src/Nexo.API/Security/` | Redacts sensitive **config** keys (not LLM prompts) |

**Behavior today (CloudSanitizationProxy):** air-gapped → pass-through; else PII detected → **block**; filterable PII → **redact**; taxonomy may further constrain. Not yet policy-pack-driven per destination target (Phase 2 must make this policy-driven: redact / block / pass by target).

**Noise:** `SanitizeXmlName`, `SanitizeIdentifier`, Unity `SanitizeClassName` — unrelated to LLM egress.

---

## 3. Audit sinks

### Barrier audit pipeline

| Type | Path | Role |
|------|------|------|
| `IBarrierAuditSink` / `IBarrierAuditLog` | `src/Nexo.Abstractions/Barriers/` | Pluggable barrier audit |
| `StructuredBarrierAuditLog` | `src/Nexo.Runtime/Barriers/` | Fans out to all sinks |
| `FileBarrierAuditSink` | `src/Nexo.Runtime/Barriers/Sinks/` | `Nexo:Audit:Sinks` contains `File` |
| `StructuredLogBarrierAuditSink` | same | ILogger sink |
| `NoOpBarrierAuditSink` | same | Default / discard |
| Registration | `src/Nexo.Runtime/RuntimeServiceCollectionExtensions.cs` → `AddBarrierAuditSinks` | Bound from `Nexo:Audit:*` |

### Trust / data-decision audit (LLM sanitization lives here)

| Type | Path | Role |
|------|------|------|
| `IDataDecisionAuditLog` | `src/Nexo.Core.Application/Trust/Ports/` | Unified: sanitization, boundary, classification, etc. |
| `DataDecisionAuditLog` / `LiteDbDataDecisionAuditLog` | `src/Nexo.BackgroundAgents/Trust/` | In-memory or LiteDB (`NEXO_TRUST_AUDIT_DB`) |
| CLI `TrustCommand.AuditAsync` | `application/src/Nexo.CLI/Commands/TrustCommand.cs` | Export/show audit |

### Related (not model-call audit)

- `IAdaptationAuditLog` / `LiteDbAdaptationAuditLog` — adaptation decisions  
- GameDirector `AuditRecord` / MCP `GetAuditTrailTool` — commercial activity feed  

**Phase 2 implication:** `AuditingChatClient` should write to **`IDataDecisionAuditLog` / sanitization audit** (and optionally emit a barrier correlation id). Do not invent a third audit store; barrier sinks are for barrier lifecycle, not model invocations.

---

## 4. Policy packs — load & evaluate

### Trust policy packs (observation / regulated packs)

| Type | Path | Role |
|------|------|------|
| `ITrustPolicyPackRegistry` / `TrustPolicyPackRegistry` | Ports + `src/Nexo.Infrastructure/Trust/` | Load `*.json`, activate pack |
| `TrustPolicyPack` (+ info/status/rules models) | `src/Nexo.Core.Application/Trust/Models/` | Pack schema |
| On-disk packs | `config/trust-packs/{strict-enterprise,internal-only,air-gapped,active-pack}.json` | Pack content + activation |
| `IAccessBoundary` / `AccessBoundary` | Infrastructure Trust | `ApplyPolicyPack`; observation gates |
| `IObservationGate` / `ObservationGate` | same | `ShouldObserve` from active boundary |
| CLI | `TrustCommand` pack list/describe/apply | Operator UX |

**Env:** `NEXO_TRUST_POLICY_PACKS_PATH`, `NEXO_TRUST_ENABLED`, `NEXO_TRUST_AUDIT_DB`.

### Tool-call policy engine (separate from packs)

| Type | Path | Role |
|------|------|------|
| `IPolicy` | `src/Nexo.Abstractions/IPolicy.cs` | Approve/deny tool calls |
| `PolicyEngine` | `src/Nexo.Runtime/PolicyEngine.cs` | Evaluate all `IPolicy`, sign deltas |
| `BackgroundAgentPolicyEngineFactory` | `src/Nexo.BackgroundAgents/Security/` | Builds engine with `DataExfiltrationPolicy` |
| `AllowAllPolicy`, path/sandbox policies | `src/Nexo.Policies/`, `src/Nexo.Policies.Dev/` | Built-in tool policies |

**No `PolicyGate` type exists today.** Phase 2 `PolicyGateChatClient` is new; it should consult trust/data-classification → allowed execution targets (extend packs or add a new pack section for target keys — design in Phase 2).

### Trust tiers (mesh / fleet — not RAG trust tags)

| Type | Path | Role |
|------|------|------|
| `PeerTrustTier` | `src/Nexo.Core.Application/Mesh/Models/` | Unknown/Untrusted/Trusted |
| `MeshTrustPolicyConfiguration` | Mesh | `NEXO_MESH_TRUST_POLICY` |
| Fleet trust | commercial Fleet contracts | Placement eligibility |

RAG sensitivity is **`IDataSensitivityRegistry` levels**, not `PeerTrustTier`. Phase 5 “trust-tier tag” maps to sensitivity level names.

---

## 5. RAG / embedding / vector storage

| Type | Path | Role |
|------|------|------|
| `IEmbeddingGenerator` | `src/Nexo.BackgroundAgents/RAG/IEmbeddingGenerator.cs` | **Nexo-local** `GenerateAsync → float[]` — **name collision with MEAI** |
| `TokenEmbeddingGenerator` | same | Deterministic bag-of-words (dim 64 from `NexoDefaults`) |
| `IVectorStore` / `InMemoryVectorStore` | same | Default DI store |
| `SqliteVectorStore` | same | Implemented + tested; **not** registered in `AddBackgroundAgentsRAG` |
| `IRAGService` / `RAGService` | same | Embed + index/search façade |
| `IKnowledgeBaseIndexer` / `KnowledgeBaseIndexer` | same | File → RAG indexing |
| `RAGTool` | same | Agent tool `rag_search` |
| `RAGConfig` | `src/Nexo.BackgroundAgents/Configuration/RAGConfig.cs` | Docs mention sqlite/postgres/qdrant; only in-memory wired |
| `DecompositionRetriever` | `src/Nexo.Orchestration/Architect/` | Keyword “RAG” over examples — **no embeddings** |
| Tests | `src/Nexo.Tests.BackgroundAgents/RAG/*` | Coverage for stores, embeddings, tool |

**DI:** `AddBackgroundAgentsRAG()` → `TokenEmbeddingGenerator`, `InMemoryVectorStore`, `RAGService`, `KnowledgeBaseIndexer` (kernel Phase 11 when `IncludeBackgroundAgentRag`).

**Phase 5 note:** rename or alias Nexo’s `IEmbeddingGenerator` when adopting MEAI’s `IEmbeddingGenerator<string, Embedding<float>>` to avoid type collisions (qualify namespaces).

---

## 6. DI registration points (CLI + API)

### Hosts

| Host | Project | Entry |
|------|---------|-------|
| CLI | `application/src/Nexo.CLI/` | `Program.cs` → `AddNexoRuntimeRouting` + `AddNexo()` |
| API | `application/src/Nexo.API/` | `Program.cs` → same + API-only ingress (SNS/DynamoDB) |

No `Startup.cs`. Feature flags are **Options + env**, not Microsoft.FeatureManagement.

### Shared composition root

| File | Role |
|------|------|
| `src/Nexo.Hosting/NexoServiceCollectionExtensions.cs` | `AddNexo` / `AddNexoProfile` |
| `src/Nexo.Hosting/NexoKernelRegistrar.cs` + `.Phases.cs` | Ordered phases |
| `src/Nexo.Hosting/ModuleSelection.cs` | Profile gates (`IncludeBackgroundAgentRag`, `IncludeTrustServices`, …) |
| `src/Nexo.Hosting/NexoHostingOptions.cs` | `TrustEnabled`, hosted-agent flags, etc. |

### AI-relevant kernel phases

| Phase | What |
|-------|------|
| **11** | `AddBackgroundAgents` + optional `AddBackgroundAgentsRAG` |
| **13** | `HotSwappableModel` + `IModel` = `OrchestrationRuntimeModelDecorator` |
| **14** | Optional `IEphemeralModelLifecycle` → Ollama ephemeral |
| **15** | Trust + `IProviderFactory` 3-way branch (adaptive / sanitizing / plain) |

### Other DI

| Extension | Path |
|-----------|------|
| `AddTrustServices` | `src/Nexo.BackgroundAgents/ServiceCollectionExtensions.cs` |
| `AddAccessBoundary` | `src/Nexo.Infrastructure/Trust/Sdk/Extensions/TrustServiceCollectionExtensions.cs` |
| `AddBarrierAuditSinks` | `src/Nexo.Runtime/RuntimeServiceCollectionExtensions.cs` (`Nexo:Audit:*`) |
| NCR + Ollama backend | `src/Nexo.Hosting/NexoServiceCollectionExtensions.NodeCapabilityRuntime.cs` |

### Planned feature flag

`Nexo:UseMeaiPipeline` — **does not exist yet**. Follow existing pattern: bind bool from config section + optional env override; default **off** until Phase 6. Suggested env alias: `NEXO_USE_MEAI_PIPELINE=1`.

### AWS credentials (for Phase 4 Bedrock reuse)

| Piece | Path | Notes |
|-------|------|-------|
| DynamoDB store | `src/Nexo.Ingress.DynamoDb/` | `new AmazonDynamoDBClient()` — **default credential/region chain** |
| Options | `src/Nexo.Contracts/SmsIngressDynamoDbOptions.cs` | Table name only (`Nexo:SmsIngressDynamoDb`) |
| SNS | `src/Nexo.Ingress.AwsSns/` | Signature verify only — **no AWS SDK client** |
| Packages | `Directory.Packages.props` | `AWSSDK.DynamoDBv2` 3.7.400, Core/S3/Lambda 3.7.305.12 — **no Bedrock** |

---

## Proposed mapping: existing type → MEAI concept

| Existing type | MEAI concept | Notes |
|---------------|--------------|-------|
| `IModel` / `ProviderBackedModel` | Consumer of `IChatClient` (adapter) | Keep `IModel` until Phase 6; impl can call MEAI when flag on |
| `IProviderFactory` / `ProviderFactory` | Provider `IChatClient`s behind keyed DI | Split per target; do not register raw factory/clients publicly |
| `OllamaProvider` | `IChatClient` via **OllamaSharp** (or thin adapter) | Key: `local:ollama` |
| `LocalModelProvider` (LLamaSharp) | Custom `IChatClient` adapter | Key: `local:onnx` alias or `local:llamasharp` — see §1 |
| `SanitizingProviderFactory` + `ICloudSanitizationProxy` | `SanitizingChatClient : DelegatingChatClient` | Move policy-driven redact/block/pass here |
| `ISensitiveContentFilter` | Used inside `SanitizingChatClient` | Reuse; don’t rewrite filters |
| `IDataDecisionAuditLog` / sanitization audit | `AuditingChatClient : DelegatingChatClient` | Emit counts/categories only |
| `ITrustPolicyPackRegistry` + target allow-list (new) | `PolicyGateChatClient : DelegatingChatClient` | New gate over (caller, target, model) |
| `AdaptiveProviderFactory` / `ILoadPolicy` / `NcrCapabilityRouter` | `RoutingChatClient : IChatClient` | Phase 3; local-first + policy × availability |
| *(none)* Bedrock | `BedrockChatClient : IChatClient` | Phase 4; keys `cloud:bedrock:{fast,balanced,heavy}` |
| Nexo `IEmbeddingGenerator` / `TokenEmbeddingGenerator` | MEAI `IEmbeddingGenerator<string, Embedding<float>>` | Phase 5; rename Nexo interface or fully qualify |
| `IVectorStore` / `InMemoryVectorStore` / `SqliteVectorStore` | `VectorStore` / `VectorStoreCollection` (Microsoft.Extensions.VectorData) | Keep old read-only until Phase 6 |
| `RAGService` / `KnowledgeBaseIndexer` | Facades over VectorData + embedding generator | Preserve sensitivity ≤ caller filter |
| Raw `OllamaApiClient` / Bedrock / ONNX session | **Never** resolve from DI | Only decorated `IChatClient` pipeline is public |

### Fixed governance composition (Phase 2)

```
UseNexoGovernance() →
  PolicyGate → Sanitizing → Auditing → [UseFunctionInvocation()] → provider IChatClient
```

Router (Phase 3) sits **outside** per-target stacks and is itself wrapped in Auditing.

---

## Gaps & risks for later phases

1. **TFM mismatch:** plan = .NET 9; repo libraries/hosts = `net8.0`. `Microsoft.Extensions.*` already at **10.0.8** in CPM — MEAI packages should align carefully.
2. **Dual invocation paths:** `IModel`/`IProviderFactory` and NCR `IModelServingBackend` — decide whether NCR remains parallel or folds into MEAI (recommend: Phase 1 wraps chat path only; NCR later).
3. **Bypass surface:** many direct `IProviderFactory` callers — flag must route them or Phase 6 cleanup will leave holes.
4. **“ONNX” naming** vs LLamaSharp reality — document in policy keys to avoid operator confusion.
5. **Sanitization not per-target today** — Proxy is cloud-oriented; local pass-through must become explicit policy.
6. **Two audit models** — prefer trust data-decision audit for MEAI middleware; barrier sinks for barriers.
7. **Package asks (plan allows these):**
   - Phase 1: `Microsoft.Extensions.AI.Abstractions`, `Microsoft.Extensions.AI`, `OllamaSharp`
   - Phase 4: `AWSSDK.BedrockRuntime` (+ AWS MEAI adapter if available)
   - Phase 5: `Microsoft.Extensions.VectorData.Abstractions` + one concrete store
8. **No Bedrock / no Anthropic** yet; Amazon credentials path is default chain only.
9. **RAG sensitivity ≠ mesh trust tier** — map Phase 5 tags to `IDataSensitivityRegistry`.

---

## Neighbor projects for `src/Nexo.AI.Pipeline`

| Project | Why |
|---------|-----|
| `Nexo.Abstractions` | `IModel`, barriers, tools |
| `Nexo.BackgroundAgents` | Trust sanitization, RAG (reuse; avoid circular refs — prefer ports/interfaces) |
| `Nexo.Infrastructure` | ProviderFactory, Ollama, LLamaSharp (adapters wrap, don’t rewrite) |
| `Nexo.Hosting` | Feature-flagged `ChatClientBuilder` registration |
| `Nexo.Core.Application` | Trust ports, NCR ports |
| `Nexo.Adapters.Models` | Lightweight model adapters peer |
| Tests: `Nexo.Tests.BackgroundAgents`, new `Nexo.Tests.AI.Pipeline` | Unit + composition + architecture tests |

**Suggested dependency direction:** `Nexo.AI.Pipeline` depends on Abstractions + Application ports + MEAI packages; Infrastructure/BackgroundAgents provide adapters registered from Hosting. Avoid Pipeline → Hosting.

---

## Phase checklist

| Phase | Deliverable | Status |
|-------|-------------|--------|
| 0 | This notes file | **Done** |
| 1 | `Nexo.AI.Pipeline` + Ollama/LLamaSharp `IChatClient` + flag off | **Done** |
| 2 | PolicyGate / Sanitizing / Auditing middleware + DI architecture tests | **Done** |
| 3 | `RoutingChatClient` + policy × availability matrix tests | Pending |
| 4 | Bedrock tiered targets + env-gated integration test | Pending |
| 5 | VectorData RAG + embedding middleware + reindex CLI | Pending |
| 6 | Flag default on; delete legacy; `docs/governed-pipeline.md` | Pending |

---

## Phase 1 implementation notes (2026-07-14)

Landing branch: `cursor/meai-phase1-pipeline-5a04`

### Delivered
- New project `src/Nexo.AI.Pipeline` (TFMs `net8.0;net9.0`) + tests `src/Nexo.Tests.AI.Pipeline`
- Keyed `IChatClient` targets: `local:ollama` (`OllamaHttpChatClient`), `local:onnx` (`LlamaSharpChatClient`)
- Hosting Phase **13b** registers the pipeline only when `Nexo:UseMeaiPipeline` / `NEXO_USE_MEAI_PIPELINE` / `NexoHostingOptions.UseMeaiPipeline` is true (**default off**)
- Raw `OllamaHttpChatClient` / `LlamaSharpChatClient` are **not** registered in DI — only keyed `IChatClient` via `AddKeyedChatClient`
- Packages: `Microsoft.Extensions.AI` + `Abstractions` **10.7.0**; CPM bumped related `Microsoft.Extensions.*` / `System.Text.*` **10.0.8 → 10.0.9** for MEAI

### Discovery changes for later phases
1. **OllamaSharp deferred:** package 5.4.25 ships a Roslyn 5 analyzer incompatible with this repo's pinned C# 12 / compiler 4.14. Phase 1 uses a thin `OllamaHttpChatClient` over `/api/chat` instead (plan-allowed). Revisit OllamaSharp when the repo moves to a Roslyn 5-capable toolchain.
2. **`local:onnx` = LLamaSharp GGUF** confirmed in code comments + options; not ONNX Runtime GenAI.
3. Host libraries remain **net8.0**; Pipeline dual-targets so Hosting can consume net8 while still shipping net9.
4. Governance middleware (`UseNexoGovernance`) is **Phase 2** — Phase 1 registers bare keyed clients through `ChatClientBuilder` with no policy/sanitize/audit stack yet.

---

## Phase 2 implementation notes (2026-07-14)

Landing branch: `cursor/meai-phase2-governance-5a04`

### Delivered
- `UseNexoGovernance(targetKey)` — fixed order **PolicyGate → Sanitizing → Auditing → provider**
- `PolicyViolationException` with structured `Code` / target / details (no raw secrets)
- Ports: `IChatTargetAccessPolicy`, `IChatMessageSanitizer`, `ITargetSanitizePolicy`, `IChatInvocationAuditor`
- Defaults: local allow / cloud deny; local sanitize=Pass; cloud sanitize=BlockOnSecretRedactOnPii
- `AddNexoMeaiPipeline` always applies `UseNexoGovernance` (hosts cannot register ungoverned keyed clients through this API)
- Unit tests: deny short-circuit, PII redact before spy, audit on success/fault/cancel, composition order, architecture (resolved client is `PolicyGateChatClient`)

### Follow-ups for later phases
- Wire adapters to existing `ICloudSanitizationProxy` / `IDataDecisionAuditLog` / trust packs (ports are ready)
- Phase 3 router wraps governed per-target pipelines and audits route decisions
