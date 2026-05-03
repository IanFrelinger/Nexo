# Persistence Defaults

Nexo uses in-memory stores by default. For durable storage, configure LiteDB-backed persistence.

## Defaults

| Service | Default | Durable Option |
|---------|--------|----------------|
| Forge session / macros (`Nexo.API`) | `InMemoryForgeStateService` | `LiteDbForgeStateService` when `Nexo:ForgeSession:LiteDbPath` is set (relative paths resolve under the API content root) |
| `IUnitOfWork` | `InMemoryUnitOfWork` | Replace with adapter (e.g. SQLite) |
| `IPatternStore` | none (when `PatternStorePath` not set) | `LiteDbPatternStore` via `AddAdaptationInfrastructure(path)` |
| `IAdaptationLog` | in-memory | `LiteDbAdaptationLog` when pattern store path set |
| `IAdaptationAuditLog` | in-memory | `LiteDbAdaptationAuditLog` when adaptation infrastructure is registered |
| `ISanitizationAuditLog` / `IDataDecisionAuditLog` | in-memory | `LiteDbDataDecisionAuditLog` (in `Nexo.BackgroundAgents`) when `NEXO_TRUST_AUDIT_DB` set |
| `IUserKnowledgeLogStore` | in-memory | `LiteDbUserKnowledgeLogStore` when `NEXO_KNOWLEDGE_LOG_PATH` set |
| `ICopilotTaskStore` | in-memory | `LiteDbCopilotTaskStore` when hosting registers copilot services |
| `IPipelineRunStore` | in-memory | `LiteDbPipelineRunStore` when `NEXO_PIPELINE_STORE_PROVIDER=LiteDb` |
| `IExecutionTracer` | in-memory | `LiteDbExecutionTracer` when pattern store path is set |
| `ITestFailureStore` | in-memory | `LiteDbTestFailureStore` when self-improvement infrastructure is registered |

## AddNexoPersistence

`AddNexoPersistence()` registers **in-memory** only:

- `IUnitOfWork` (scoped) → `InMemoryUnitOfWork`
- Data does not persist across requests

For durable unit-of-work, use an adapter package and register its `IUnitOfWork` instead.

## Pattern Store

When `AddAdaptationInfrastructure(patternStorePath)` is called with a path:

- `IPatternStore` → `LiteDbPatternStore` (file path or connection string)
- `IAdaptationLog` → `LiteDbAdaptationLog` when path provided
- `IContextAssembler` uses the pattern store for observation context

Example: `~/.nexo/patterns.db` or `Filename=patterns.db`.

## Trust Audit

Set `NEXO_TRUST_AUDIT_DB` to a file path for durable audit logging:

```bash
export NEXO_TRUST_AUDIT_DB=~/.nexo/trust-audit.db
```

When set, `LiteDbDataDecisionAuditLog` (located in `Nexo.BackgroundAgents.Trust`) persists redactions and decisions.

## Knowledge Log

Set `NEXO_KNOWLEDGE_LOG_PATH` for durable user knowledge log:

```bash
export NEXO_KNOWLEDGE_LOG_PATH=~/.nexo/knowledge.db
```

## Copilot Task Store

When hosting registers copilot services (via `AddNexo()`), `LiteDbCopilotTaskStore` persists copilot tasks. The store path is derived from the pattern store directory.

## Pipeline Run Store

Set `NEXO_PIPELINE_STORE_PROVIDER=LiteDb` and optionally `NEXO_PIPELINE_STORE_PATH` for durable pipeline run history:

```bash
export NEXO_PIPELINE_STORE_PROVIDER=LiteDb
export NEXO_PIPELINE_STORE_PATH=~/.nexo/pipeline-runs.db
```

## LiteDB

All LiteDB-backed stores use pure managed C# and run on macOS, Linux, and Windows.
