# Persistence Defaults

Nexo uses in-memory stores by default. For durable storage, configure LiteDB-backed persistence.

## Defaults

| Service | Default | Durable Option |
|---------|--------|----------------|
| `IUnitOfWork` | `InMemoryUnitOfWork` | Replace with adapter (e.g. SQLite) |
| `IPatternStore` | none (when `PatternStorePath` not set) | `LiteDbPatternStore` via `AddAdaptationInfrastructure(path)` |
| `IAdaptationLog` | in-memory | `LiteDbAdaptationLog` when pattern store path set |
| `ISanitizationAuditLog` | in-memory | `LiteDbDataDecisionAuditLog` when `NEXO_TRUST_AUDIT_DB` set |
| `IUserKnowledgeLogStore` | in-memory | `LiteDbUserKnowledgeLogStore` when `NEXO_KNOWLEDGE_LOG_PATH` set |

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

When set, `LiteDbDataDecisionAuditLog` persists redactions and decisions.

## Knowledge Log

Set `NEXO_KNOWLEDGE_LOG_PATH` for durable user knowledge log:

```bash
export NEXO_KNOWLEDGE_LOG_PATH=~/.nexo/knowledge.db
```

## LiteDB

All LiteDB-backed stores use pure managed C# and run on macOS, Linux, and Windows.
