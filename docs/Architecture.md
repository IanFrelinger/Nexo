# Architecture Overview

Nexo is a private AI platform built on modular, contract-based components (bricks) that compose into execution pipelines and extend autonomously under policy constraints. Trust enforcement is structural: data provenance, barrier identity resolution, and audit logging are integrated into the execution pipeline at the architectural level. The mesh layer federates capabilities across trusted .NET peers, enabling distributed execution with policy-controlled routing.

## Layers

```
┌─────────────────────────────────────────────────────────────────┐
│  CLI / Host / UI                                                 │
│  (Commands, System.CommandLine, AddNexo)                          │
├─────────────────────────────────────────────────────────────────┤
│  Core.Application (Use Cases)                                    │
│  MediatR handlers, validation, analysis, agents, testing         │
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
- **Ports**: `IAnalysisService`, `IValidationService`, `IAgentExecutor`, `ITestRunner`
- **Behaviors**: FluentValidation pipeline

### Abstractions

- **`IModel`**: Core model interface for agent execution
- **`IAgent`**: Agent contract (Name, ThinkAsync)
- **`IAgentMemory`**: Agent memory abstraction

### Orchestration

- **Architect**: Decomposes requests into agent goals
- **Agents**: Domain agents (Gameplay, Economy, Security, etc.) that call LLM via `IModel`
- **Coordination**: Dependency resolution, conflict detection, output integration
- **Orchestrator**: End-to-end flow

### Infrastructure

- **ProviderFactory** (`IProviderFactory`): Routes LLM calls to OpenAI, Azure, Ollama, local (ONNX), video (SmolVLM2), or mock/offline/echo (with Polly retries)
- **Persistence**: In-memory by default; LiteDB for pattern store, adaptation audit, copilot tasks, pipeline runs, execution traces, test failures
- **Adaptation**: Brick decomposition, recompilation, fix generation
- **Execution routing**: NCR-driven local/remote routing with peer-network and RunPod cloud execution paths
- **Mesh**: File-based peer discovery, capability advertisement, local transport, trust policies

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
- **Scope note**: Trust wiring is automatic in `AddNexo()` hosting registration; standalone CLI command DI graphs must explicitly opt in.

### Hosting

- **AddNexo()**: Registers all kernel services
- **AddNexoProfile(...)**: Registers environment-specific module sets (`Full`, `Server`, `Edge`, `AirGapped`, `System`) to peel optional dependencies.
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
