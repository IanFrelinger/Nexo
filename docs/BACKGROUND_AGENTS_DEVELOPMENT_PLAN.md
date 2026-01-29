# Background Agents Development Plan

## Overview

This document outlines the development plan for implementing optional embedded background agents with configurable commands, roles, hierarchies, models, data sensitivity classification, RAG integration, and web search capabilities.

## Development Phases

### Phase 1: Core Infrastructure & Data Sensitivity (Weeks 1-2)

**Goal:** Establish foundation with data sensitivity classification system.

#### Week 1: Data Sensitivity System

**Tasks:**
1. Create `Nexo.BackgroundAgents` project
2. Implement `IDataSensitivityLevel` interface
3. Implement primitive sensitivity levels (Public, Internal, Confidential, Secret, TopSecret)
4. Implement `DataSensitivityRegistry` for managing levels
5. Implement `DataSensitivityMarker` for marking and checking data
6. Add unit tests for sensitivity system

**Deliverables:**
- `src/Nexo.BackgroundAgents/DataSensitivity/IDataSensitivityLevel.cs`
- `src/Nexo.BackgroundAgents/DataSensitivity/DataSensitivityLevels.cs` (primitives)
- `src/Nexo.BackgroundAgents/DataSensitivity/DataSensitivityRegistry.cs`
- `src/Nexo.BackgroundAgents/DataSensitivity/DataSensitivityMarker.cs`
- `src/Nexo.BackgroundAgents/DataSensitivity/CustomSensitivityLevelFactory.cs`
- Unit tests

**Dependencies:** None

**Estimated Effort:** 3-4 days

#### Week 2: Background Agent Service & Registry

**Tasks:**
1. Implement `BackgroundAgentService` (BackgroundService)
2. Implement `BackgroundAgentRegistry` for agent lifecycle
3. Implement `BackgroundAgentConfig` model
4. Implement `BackgroundAgentConfigLoader` with validation
5. Implement `BackgroundAgentSpecBuilder` for creating AgentSpawnSpec
6. Add integration with existing `AgentFactory` and `Orchestrator`
7. Add unit tests

**Deliverables:**
- `src/Nexo.BackgroundAgents/Services/BackgroundAgentService.cs`
- `src/Nexo.BackgroundAgents/Registry/BackgroundAgentRegistry.cs`
- `src/Nexo.BackgroundAgents/Configuration/BackgroundAgentConfig.cs`
- `src/Nexo.BackgroundAgents/Configuration/BackgroundAgentConfigLoader.cs`
- `src/Nexo.BackgroundAgents/Configuration/BackgroundAgentSpecBuilder.cs`
- Unit tests

**Dependencies:** Phase 1 Week 1

**Estimated Effort:** 4-5 days

**Testing:**
- Unit tests for each component
- Integration test for agent creation and registration
- Test with existing AgentFactory and Orchestrator

---

### Phase 2: Exfiltration Prevention & Scheduling (Week 3)

**Goal:** Implement security policies and agent scheduling.

#### Week 3: Security & Scheduling

**Tasks:**
1. Implement `DataExfiltrationPolicy` (IPolicy)
2. Integrate with existing `PolicyEngine`
3. Implement schedule execution (continuous, interval, cron)
4. Add cron expression parsing (use library like NCrontab)
5. Implement agent execution loops
6. Add policy enforcement in agent execution
7. Add unit tests

**Deliverables:**
- `src/Nexo.BackgroundAgents/Security/DataExfiltrationPolicy.cs`
- `src/Nexo.BackgroundAgents/Scheduling/AgentScheduler.cs`
- `src/Nexo.BackgroundAgents/Scheduling/ScheduleExecutor.cs`
- Unit tests

**Dependencies:** Phase 1

**Estimated Effort:** 3-4 days

**Testing:**
- Test policy enforcement with different sensitivity levels
- Test schedule execution (interval, cron)
- Test exfiltration blocking scenarios

---

### Phase 3: RAG Integration (Week 4)

**Goal:** Implement RAG (Retrieval Augmented Generation) for knowledge base access.

#### Week 4: RAG Infrastructure

**Tasks:**
1. Define `IVectorStore` interface
2. Implement in-memory vector store
3. Implement SQLite vector store
4. Implement `IEmbeddingGenerator` interface
5. Implement basic embedding generator (or integrate with existing library)
6. Implement `RAGService` for retrieval operations
7. Implement `RAGTool` (ITool) for agent access
8. Implement `KnowledgeBaseIndexer` for indexing documents
9. Add sensitivity-aware vector search
10. Add unit tests

**Deliverables:**
- `src/Nexo.BackgroundAgents/RAG/IVectorStore.cs`
- `src/Nexo.BackgroundAgents/RAG/InMemoryVectorStore.cs`
- `src/Nexo.BackgroundAgents/RAG/SqliteVectorStore.cs`
- `src/Nexo.BackgroundAgents/RAG/IEmbeddingGenerator.cs`
- `src/Nexo.BackgroundAgents/RAG/RAGService.cs`
- `src/Nexo.BackgroundAgents/RAG/RAGTool.cs`
- `src/Nexo.BackgroundAgents/RAG/KnowledgeBaseIndexer.cs`
- Unit tests

**Dependencies:** Phase 1, Phase 2

**Estimated Effort:** 5-6 days

**Testing:**
- Test vector store operations (index, search)
- Test embedding generation
- Test sensitivity filtering in search
- Test knowledge base indexing

**Note:** For embedding generation, consider:
- Using existing .NET libraries (e.g., ML.NET, ONNX Runtime)
- Or integrating with external services (OpenAI embeddings, Azure OpenAI)
- Or providing a simple token-based similarity as fallback

---

### Phase 4: Web Search Integration (Week 5)

**Goal:** Implement web search capabilities for agents.

#### Week 5: Web Search

**Tasks:**
1. Define `IWebSearchProvider` interface
2. Implement Bing web search provider
3. Implement Google web search provider (optional)
4. Implement DuckDuckGo web search provider (optional)
5. Implement `WebSearchTool` (ITool) for agent access
6. Implement sensitive content filtering
7. Implement domain allowlist/blocklist filtering
8. Add unit tests

**Deliverables:**
- `src/Nexo.BackgroundAgents/WebSearch/IWebSearchProvider.cs`
- `src/Nexo.BackgroundAgents/WebSearch/BingWebSearchProvider.cs`
- `src/Nexo.BackgroundAgents/WebSearch/GoogleWebSearchProvider.cs` (optional)
- `src/Nexo.BackgroundAgents/WebSearch/DuckDuckGoWebSearchProvider.cs` (optional)
- `src/Nexo.BackgroundAgents/WebSearch/WebSearchTool.cs`
- `src/Nexo.BackgroundAgents/WebSearch/SensitiveContentFilter.cs`
- Unit tests

**Dependencies:** Phase 1, Phase 2

**Estimated Effort:** 3-4 days

**Testing:**
- Test web search with different providers
- Test domain filtering
- Test sensitive content filtering
- Test integration with exfiltration policies

---

### Phase 5: CLI Implementation (Week 6)

**Goal:** Implement CLI commands for configuring and managing background agents.

#### Week 6: CLI Commands

**Tasks:**
1. Implement `BackgroundAgentCommand` base class
2. Implement `ListBackgroundAgentsCommand`
3. Implement `ShowBackgroundAgentCommand`
4. Implement `AddBackgroundAgentCommand` (interactive + file-based)
5. Implement `UpdateBackgroundAgentCommand`
6. Implement `RemoveBackgroundAgentCommand`
7. Implement `StartBackgroundAgentCommand`
8. Implement `StopBackgroundAgentCommand`
9. Implement `RestartBackgroundAgentCommand`
10. Register commands in `Program.cs`
11. Add CLI tests

**Deliverables:**
- `src/Nexo.CLI/Commands/BackgroundAgent/BackgroundAgentCommand.cs`
- `src/Nexo.CLI/Commands/BackgroundAgent/ListBackgroundAgentsCommand.cs`
- `src/Nexo.CLI/Commands/BackgroundAgent/ShowBackgroundAgentCommand.cs`
- `src/Nexo.CLI/Commands/BackgroundAgent/AddBackgroundAgentCommand.cs`
- `src/Nexo.CLI/Commands/BackgroundAgent/UpdateBackgroundAgentCommand.cs`
- `src/Nexo.CLI/Commands/BackgroundAgent/RemoveBackgroundAgentCommand.cs`
- `src/Nexo.CLI/Commands/BackgroundAgent/StartBackgroundAgentCommand.cs`
- `src/Nexo.CLI/Commands/BackgroundAgent/StopBackgroundAgentCommand.cs`
- `src/Nexo.CLI/Commands/BackgroundAgent/RestartBackgroundAgentCommand.cs`
- CLI tests

**Dependencies:** Phase 1, Phase 2

**Estimated Effort:** 4-5 days

**Testing:**
- Test each CLI command
- Test interactive mode
- Test JSON output format
- Test error handling

---

### Phase 6: Sensitivity & RAG CLI (Week 7)

**Goal:** Implement CLI commands for sensitivity levels and RAG management.

#### Week 7: Extended CLI

**Tasks:**
1. Implement `SensitivityCommand` with subcommands (list, show, add, update, remove)
2. Implement `RAGCommand` with subcommands (index, search, stats, clear)
3. Implement `WebSearchCommand` with subcommands (configure, test)
4. Implement `ConfigCommand` with subcommands (show, validate, export, import)
5. Add CLI tests

**Deliverables:**
- `src/Nexo.CLI/Commands/BackgroundAgent/SensitivityCommand.cs`
- `src/Nexo.CLI/Commands/BackgroundAgent/RAGCommand.cs`
- `src/Nexo.CLI/Commands/BackgroundAgent/WebSearchCommand.cs`
- `src/Nexo.CLI/Commands/BackgroundAgent/ConfigCommand.cs`
- CLI tests

**Dependencies:** Phase 3, Phase 4, Phase 5

**Estimated Effort:** 4-5 days

**Testing:**
- Test sensitivity level management
- Test RAG indexing and search
- Test web search configuration
- Test config import/export

---

### Phase 7: Monitoring & Execution CLI (Week 8)

**Goal:** Implement CLI commands for monitoring and manual execution.

#### Week 8: Monitoring CLI

**Tasks:**
1. Implement `ExecuteBackgroundAgentCommand`
2. Implement `LogsBackgroundAgentCommand`
3. Implement `MetricsBackgroundAgentCommand`
4. Add logging infrastructure for agents
5. Add metrics collection for agents
6. Add CLI tests

**Deliverables:**
- `src/Nexo.BackgroundAgents/Logging/AgentLogger.cs`
- `src/Nexo.BackgroundAgents/Metrics/AgentMetrics.cs`
- `src/Nexo.CLI/Commands/BackgroundAgent/ExecuteBackgroundAgentCommand.cs`
- `src/Nexo.CLI/Commands/BackgroundAgent/LogsBackgroundAgentCommand.cs`
- `src/Nexo.CLI/Commands/BackgroundAgent/MetricsBackgroundAgentCommand.cs`
- CLI tests

**Dependencies:** Phase 5

**Estimated Effort:** 3-4 days

**Testing:**
- Test manual execution
- Test log retrieval
- Test metrics collection

---

### Phase 8: Self-Management Meta-Agent (Week 9)

**Goal:** Implement meta-agent that manages other background agents (advanced dog-fooding).

#### Week 9: Meta-Agent

**Tasks:**
1. Implement `BackgroundAgentManagerAgent` (BaseDomainAgent)
2. Implement agent management tools (EnableAgentTool, DisableAgentTool, etc.)
3. Implement `AgentManagementToolbox`
4. Configure meta-agent as a background agent
5. Add unit tests

**Deliverables:**
- `src/Nexo.BackgroundAgents/Agents/BackgroundAgentManagerAgent.cs`
- `src/Nexo.BackgroundAgents/Tools/EnableAgentTool.cs`
- `src/Nexo.BackgroundAgents/Tools/DisableAgentTool.cs`
- `src/Nexo.BackgroundAgents/Tools/RestartAgentTool.cs`
- `src/Nexo.BackgroundAgents/Tools/UpdateAgentConfigTool.cs`
- `src/Nexo.BackgroundAgents/Tools/AgentManagementToolbox.cs`
- Unit tests

**Dependencies:** Phase 1, Phase 2, Phase 5

**Estimated Effort:** 3-4 days

**Testing:**
- Test meta-agent managing other agents
- Test agent management tools
- Test self-configuration scenarios

---

### Phase 9: Integration & Documentation (Week 10)

**Goal:** Complete integration, documentation, and end-to-end testing.

#### Week 10: Integration & Polish

**Tasks:**
1. End-to-end integration tests
2. Performance testing
3. Documentation updates
4. Example configurations
5. Migration guide
6. Bug fixes and polish

**Deliverables:**
- E2E tests
- Performance benchmarks
- Updated documentation
- Example config files
- Migration guide

**Dependencies:** All previous phases

**Estimated Effort:** 3-4 days

**Testing:**
- Full system integration test
- Performance benchmarks
- Load testing with multiple agents
- Security testing

---

### Phase 10: Hardening (Week 11)

**Goal:** Harden the background agents system with performance benchmarks, load testing, security validation, and resilience improvements.

#### Week 11: Performance, Load & Security

**Tasks:**
1. **Performance benchmarks**
   - Benchmark single-agent execution (ExecuteOnceAsync) latency and throughput
   - Benchmark RAG search (InMemory vs SQLite) with varying corpus sizes and concurrency
   - Benchmark embedding generation and vector store indexing
   - Document baseline metrics and add optional benchmark project or test category
2. **Load testing**
   - Run multiple agents (e.g. 5–20) concurrently with interval/cron schedules
   - Stress registry (register/start/stop/execute) under concurrent access
   - Verify log store (InMemoryAgentLogStore) under high volume and bounded buffer behavior
   - Measure memory usage and scheduler behavior with many agents
3. **Security & regression**
   - Validate DataExfiltrationPolicy under concurrent tool calls and multiple sensitivity levels
   - Regression tests for config loading (malformed JSON, missing required fields, invalid schedule)
   - Verify sensitivity enforcement across RAG search and web search tool usage
4. **Resilience**
   - Test cancellation (ExecuteOnceAsync, Start/Stop) and graceful shutdown
   - Verify failure isolation (one agent failing does not stop others; error logging and metrics)
   - Optional: add circuit breaker or retry policy for agent execution failures

**Deliverables:**
- `src/Nexo.Tests.BackgroundAgents/Performance/` (or equivalent) benchmark/integration tests
- Documented performance baselines (e.g. in `docs/BACKGROUND_AGENTS_ARCHITECTURE.md` or `docs/METRICS.md`)
- Load and security test scenarios; any resilience fixes or configuration knobs

**Dependencies:** Phase 9 complete

**Estimated Effort:** 3–5 days

**Testing:**
- Automated performance benchmarks (repeatable)
- Load tests with configurable agent count and schedule mix
- Security tests for exfiltration policy and sensitivity boundaries
- Resilience tests for cancellation and failure isolation

---

## Project Structure

```
src/Nexo.BackgroundAgents/
├── DataSensitivity/
│   ├── IDataSensitivityLevel.cs
│   ├── DataSensitivityLevels.cs
│   ├── DataSensitivityRegistry.cs
│   ├── DataSensitivityMarker.cs
│   └── CustomSensitivityLevelFactory.cs
├── Configuration/
│   ├── BackgroundAgentConfig.cs
│   ├── BackgroundAgentConfigLoader.cs
│   ├── BackgroundAgentSpecBuilder.cs
│   ├── RAGConfig.cs
│   └── WebSearchConfig.cs
├── Services/
│   └── BackgroundAgentService.cs
├── Registry/
│   └── BackgroundAgentRegistry.cs
├── Security/
│   └── DataExfiltrationPolicy.cs
├── Scheduling/
│   ├── AgentScheduler.cs
│   └── ScheduleExecutor.cs
├── RAG/
│   ├── IVectorStore.cs
│   ├── InMemoryVectorStore.cs
│   ├── SqliteVectorStore.cs
│   ├── IEmbeddingGenerator.cs
│   ├── RAGService.cs
│   ├── RAGTool.cs
│   └── KnowledgeBaseIndexer.cs
├── WebSearch/
│   ├── IWebSearchProvider.cs
│   ├── BingWebSearchProvider.cs
│   ├── WebSearchTool.cs
│   └── SensitiveContentFilter.cs
├── Agents/
│   └── BackgroundAgentManagerAgent.cs
├── Tools/
│   ├── EnableAgentTool.cs
│   ├── DisableAgentTool.cs
│   ├── RestartAgentTool.cs
│   ├── UpdateAgentConfigTool.cs
│   └── AgentManagementToolbox.cs
├── Logging/
│   └── AgentLogger.cs
├── Metrics/
│   └── AgentMetrics.cs
└── Nexo.BackgroundAgents.csproj
```

## Dependencies

### External Libraries

- **NCrontab** - Cron expression parsing (NuGet: `NCrontab`)
- **SQLite** - For SQLite vector store (already in framework)
- **System.Text.Json** - JSON processing (already in framework)
- **Microsoft.Extensions.Hosting** - BackgroundService (already in framework)
- **Microsoft.Extensions.Configuration** - Configuration (already in framework)

### Optional (for embeddings)

- **ML.NET** - For local embedding generation (optional)
- **ONNX Runtime** - For model inference (optional)
- Or use external API (OpenAI, Azure OpenAI) for embeddings

## Testing Strategy

### Unit Tests
- Each component tested in isolation
- Mock dependencies where appropriate
- Test edge cases and error scenarios

### Integration Tests
- Test component interactions
- Test with real AgentFactory and Orchestrator
- Test configuration loading and validation

### E2E Tests
- Full agent lifecycle (create, start, execute, stop, remove)
- RAG indexing and search
- Web search with filtering
- Sensitivity enforcement
- Exfiltration prevention

### Performance Tests
- Agent execution performance
- RAG search performance
- Concurrent agent execution
- Memory usage with multiple agents

## Risk Mitigation

### Risks

1. **Complexity of RAG Implementation**
   - **Mitigation:** Start with simple in-memory vector store, add SQLite later
   - **Fallback:** Use token-based similarity if embedding generation is complex

2. **Web Search API Costs**
   - **Mitigation:** Make web search optional, use free providers where possible
   - **Fallback:** Support DuckDuckGo (free) as default

3. **Sensitivity Classification Accuracy**
   - **Mitigation:** Provide clear guidelines and examples
   - **Fallback:** Default to most restrictive if uncertain

4. **Performance with Many Agents**
   - **Mitigation:** Implement efficient scheduling and resource management
   - **Fallback:** Add agent limits and resource quotas

## Success Criteria

1. ✅ Agents can be configured via CLI and config files
2. ✅ Data sensitivity levels are enforced
3. ✅ Exfiltration policies prevent sensitive data leaks
4. ✅ RAG knowledge base can be indexed and searched
5. ✅ Web search works with domain filtering
6. ✅ Agents execute on schedule (interval, cron, continuous)
7. ✅ CLI commands work for all operations
8. ✅ Meta-agent can manage other agents
9. ✅ All tests pass
10. ✅ Documentation is complete
11. (Phase 10) Performance baselines documented; load and security tests pass; resilience validated

## Timeline Summary

| Phase | Duration | Weeks |
|-------|----------|-------|
| Phase 1: Core Infrastructure | 2 weeks | 1-2 |
| Phase 2: Security & Scheduling | 1 week | 3 |
| Phase 3: RAG Integration | 1 week | 4 |
| Phase 4: Web Search | 1 week | 5 |
| Phase 5: Basic CLI | 1 week | 6 |
| Phase 6: Extended CLI | 1 week | 7 |
| Phase 7: Monitoring CLI | 1 week | 8 |
| Phase 8: Meta-Agent | 1 week | 9 |
| Phase 9: Integration & Docs | 1 week | 10 |
| Phase 10: Hardening | 1 week | 11 |
| **Total** | **11 weeks** | **1-11** |

## Next Steps

1. Review and approve development plan
2. Create `Nexo.BackgroundAgents` project
3. Set up project structure
4. Begin Phase 1 implementation
5. (After Phase 9) Execute Phase 10 hardening: benchmarks, load tests, security and resilience validation
