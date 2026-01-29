# Background Agents: Performance Baselines

Phase 10 hardening tests encode baseline expectations. These are enforced by tests in `Nexo.Tests.BackgroundAgents.Performance` and related load/resilience tests.

## ExecuteOnceAsync (single agent, no LLM)

- **Latency**: Average per call &lt; 500 ms over 20 iterations (deterministic execution only).
- **Throughput**: 50 concurrent calls for the same agent complete successfully; execution and success counts match.

## RAG (in-memory store, token embedding)

- **Search**: Over 50 documents, 20 searches complete in &lt; 200 ms per search.
- **Index**: 200 documents index at &lt; 50 ms per document average.
- **Concurrency**: 30 concurrent search calls complete successfully.

## Load

- **Registry**: 10 agents registered; concurrent `ExecuteOnceAsync` for all complete with correct counts per agent.
- **Same-agent concurrency**: 15 concurrent `ExecuteOnceAsync` calls for one agent complete; execution/success count = 15.
- **Log store**: Bounded buffer (e.g. 100 entries) caps at configured size under 500 appends; 100 concurrent writers across 5 agents do not throw.

## Resilience

- **Unknown agent**: `ExecuteOnceAsync("nonexistent")` throws `InvalidOperationException`; registry state unchanged.
- **Two agents**: Concurrent `ExecuteOnceAsync` for two agents both succeed independently.
- **Cancelled token**: `ExecuteOnceAsync` with cancelled token completes (no hang).

## Running the tests

```bash
dotnet test src/Nexo.Tests.BackgroundAgents/Nexo.Tests.BackgroundAgents.csproj
```

To run only Phase 10–related tests:

```bash
dotnet test src/Nexo.Tests.BackgroundAgents/Nexo.Tests.BackgroundAgents.csproj \
  --filter "FullyQualifiedName~Performance|FullyQualifiedName~LoadTests|FullyQualifiedName~ConfigLoaderRegression|FullyQualifiedName~RegistryResilience"
```
