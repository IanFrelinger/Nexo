# Architecture Overview

Nexo is an AI-enhanced development orchestration platform with a layered architecture.

## Layers

```
┌─────────────────────────────────────────────────────────────────┐
│  CLI / Host / UI                                                 │
│  (Commands, System.CommandLine, AddNexo)                          │
├─────────────────────────────────────────────────────────────────┤
│  Core.Application (Use Cases)                                    │
│  MediatR handlers, validation, analysis, validation, testing     │
├─────────────────────────────────────────────────────────────────┤
│  Orchestration                                                   │
│  Architect, agents, coordination, negotiation, orchestration     │
├─────────────────────────────────────────────────────────────────┤
│  BackgroundAgents                                               │
│  Scheduler, RAG, Trust, web search, observation pipeline         │
├─────────────────────────────────────────────────────────────────┤
│  Infrastructure                                                  │
│  ProviderFactory (LLM), persistence, adaptation, IO, execution  │
├─────────────────────────────────────────────────────────────────┤
│  Core.Domain / Abstractions                                      │
│  Bricks, behaviors, agents, ports                               │
└─────────────────────────────────────────────────────────────────┘
```

## Key Components

### Core.Application

- **Use cases** (MediatR): `AnalyzeCode`, `RunValidation`, `RunAgent`, `RunTests`
- **Ports**: `IAnalysisService`, `IValidationService`, `IAgentExecutor`, `ITestRunner`, `IModel`, `IProviderFactory`
- **Behaviors**: FluentValidation pipeline

### Orchestration

- **Architect**: Decomposes requests into agent goals
- **Agents**: Domain agents (Gameplay, Economy, Security, etc.) that call LLM via `IModel`
- **Coordination**: Dependency resolution, conflict detection, output integration
- **Orchestrator**: End-to-end flow

### Infrastructure

- **ProviderFactory**: Routes LLM calls to OpenAI, Azure, Ollama, or mock (with Polly retries)
- **Persistence**: In-memory by default; LiteDB for pattern store, audit log
- **Adaptation**: Brick decomposition, recompilation, fix generation
- **Execution routing**: NCR-driven local/remote routing with peer-network and RunPod cloud execution paths

#### Execution Routing (Generation)

- **Entry point**: `CapabilityRoutingBrick` (`generation.capability-routing`) is the default generation brick.
- **Router**: `NcrCapabilityRouter` resolves targets from NCR capability snapshots and job requirements.
- **Local path**: `ILocalExecutor` is selected when VRAM, compute class, and queue depth satisfy requirements.
- **Peer path**: `NexoPeerBrickExecutor` dispatches to eligible peer Nexo nodes with timeout + failover.
- **Cloud path**: `RunPodBrick` executes full RunPod lifecycle (spin up, dispatch, poll, pull, teardown).
- **Policy controls**: job-level `RemoteExecutionPreference` plus system-level peer routing options.

### Trust &amp; Information Architecture

- **SanitizingProviderFactory**: Wraps `IProviderFactory`, sanitizes prompts before cloud
- **CloudSanitizationProxy**: PII checks, `ISensitiveContentFilter` (email, phone, SSN, API keys)
- **Audit log**: Redactions and decisions

### Hosting

- **AddNexo()**: Registers all kernel services
- **AddNexoOpenTelemetry()**: Optional metrics export

## Data Flow

1. **CLI**: `nexo validate` → `ValidateCommand` → `RunValidationHandler` → `IValidationService`
2. **Agent**: `nexo agent` → `AgentCommand` → `IAgentExecutor` → `IModel` (via `IProviderFactory`)
3. **LLM**: `ProviderFactory.ExecuteLLMAsync` → HTTP (OpenAI/Azure/Ollama) with retry
4. **Trust**: `SanitizingProviderFactory` → `CloudSanitizationProxy.SanitizeForCloud` → inner factory

## Dependencies

- **Nexo.Hosting** → Infrastructure, Orchestration, BackgroundAgents
- **Nexo.CLI** → Nexo.Hosting, Demo.Bricks, Test projects (runtime discovery)
- **Nexo.Infrastructure** → Core.Application, Core.Domain, Abstractions
