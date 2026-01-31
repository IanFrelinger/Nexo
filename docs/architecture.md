# Nexo Architecture Guide

## Overview

Nexo follows a hexagonal (ports & adapters) architecture with clean separation between domain logic, application services, and infrastructure concerns.

---

## Core Concepts

### The Three-Layer Composition Model

```
┌─────────────────────────────────────────────────────────────────────────────┐
│                         COMPOSITION HIERARCHY                               │
│                                                                             │
│   AGENT          →  Persona with memory, constraints, platform bindings     │
│   (uses behaviors)                                                          │
│                                                                             │
│   BEHAVIOR       →  Composed workflow solving a use case                    │
│   (uses bricks)     Steps with input/output mapping, failure policies       │
│                                                                             │
│   BRICK          →  Atomic unit with dual implementation                    │
│                     ⚙️ Deterministic | 🤖 Agentic                            │
│                                                                             │
└─────────────────────────────────────────────────────────────────────────────┘
```

### Processing Brick

The fundamental building block. Every brick encapsulates:

1. **Domain Knowledge**: Standards, rules, reference data, learned patterns
2. **Interface**: Typed inputs and outputs
3. **Dual Implementations**: Deterministic and/or Agentic
4. **Selector Logic**: Runtime decision for which implementation to use

```csharp
public abstract class Brick
{
    public string Id { get; init; }
    public string Name { get; init; }
    public DomainKnowledge DomainKnowledge { get; init; }
    public BrickInterface Interface { get; init; }
    public BrickImplementations Implementations { get; init; }
    public ImplementationSelector? Selector { get; init; }
    
    public abstract Task<BrickOutput> ExecuteAsync(
        BrickInput input,
        ImplementationType implementation,
        IExecutionContext context,
        CancellationToken ct = default);
}
```

### Domain Knowledge

What makes bricks valuable and reusable:

```csharp
public class DomainKnowledge
{
    // Industry standards (e.g., "OWASP Top 10 2023", "CVSS 3.1")
    public IReadOnlyList<string> Standards { get; init; }
    
    // Codified detection patterns
    public IReadOnlyList<DomainRule> Rules { get; init; }
    
    // Reference data paths (lookup tables, configs)
    public IReadOnlyDictionary<string, string> ReferenceData { get; init; }
    
    // Patterns learned from past usage
    public IReadOnlyList<LearnedPattern> LearnedPatterns { get; init; }
    
    // Usage statistics for learning
    public long ExecutionCount { get; set; }
}
```

### Implementation Selector

Runtime logic for choosing between implementations:

```csharp
public class ImplementationSelector
{
    // Conditions that prefer deterministic (e.g., "environment.airGapped")
    public IReadOnlyList<string> PreferDeterministic { get; init; }
    
    // Conditions that prefer agentic (e.g., "input.complexity > 0.8")
    public IReadOnlyList<string> PreferAgentic { get; init; }
    
    public ImplementationType Select(IExecutionContext context)
    {
        // Air-gapped environments always use deterministic
        if (context.IsAirGapped)
            return ImplementationType.Deterministic;
        
        // High-complexity inputs benefit from AI
        if (context.InputComplexity > 0.8 && HasAgentic)
            return ImplementationType.Agentic;
        
        // Default based on availability
        return HasDeterministic 
            ? ImplementationType.Deterministic 
            : ImplementationType.Agentic;
    }
}
```

---

## Project Structure

```
Nexo/
├── src/
│   ├── Nexo.Abstractions/           # Core interfaces and contracts
│   ├── Nexo.Core.Domain/            # Domain layer (bricks, behaviors, agents)
│   ├── Nexo.Core.Application/       # Application layer (commands, orchestrators)
│   ├── Nexo.Runtime/                # Execution engine
│   │
│   ├── Nexo.Orchestration/          # Multi-agent coordination
│   │   ├── Architect/               # Request decomposition
│   │   ├── Coordination/            # Dependency resolution, conflicts
│   │   ├── Communication/           # Inter-agent messaging
│   │   ├── Negotiation/             # Autonomous conflict resolution
│   │   ├── Resilience/              # Circuit breakers, retry policies
│   │   └── Metrics/                 # Performance monitoring
│   │
│   ├── Nexo.GeoTerrain/             # Terrain generation domain
│   ├── Nexo.GeoVector/              # Vector feature processing domain
│   ├── Nexo.GeoWorld/               # World bundle generation domain
│   │
│   ├── Nexo.Adapters.*/             # Infrastructure adapters
│   │   ├── Nexo.Adapters.OpenAI/
│   │   ├── Nexo.Adapters.Azure/
│   │   ├── Nexo.Adapters.Ollama/
│   │   ├── Nexo.Adapters.Assets/
│   │   ├── Nexo.Adapters.GeoTerrain/  # Elevation data providers
│   │   ├── Nexo.Adapters.GeoVector/   # Vector data providers
│   │   └── Nexo.Adapters.Persistence.*/  # Optional: SQLite, Postgres, etc. (see docs/PERSISTENCE.md)
│   │
│   ├── Nexo.API/                    # REST API for geospatial operations
│   ├── Nexo.SDK/                    # Programmatic SDK with resource estimation
│   │
│   ├── Nexo.Tools.*/                # Development tools
│   ├── Nexo.Policies.*/             # Policy enforcement
│   │
│   ├── Nexo.Core.UI/                # Framework-agnostic UI primitives
│   ├── Nexo.Core.UI.Avalonia/       # Avalonia renderer
│   ├── Nexo.Core.UI.Unity/          # Unity Editor renderer
│   │
│   ├── Nexo.CLI/                    # Command-line interface
│   ├── Nexo.Demo.Visual/            # Interactive demo application
│   │
│   └── Nexo.Tests.*/                # Test projects
│
├── docs/                            # Documentation
├── scripts/                         # Build and deployment scripts
└── tools/                           # Development utilities
```

---

## Layering Rules

### Dependency Direction

```
Presentation (CLI, Web, Unity)
      │
      ▼
Application (Commands, Orchestrators)
      │
      ▼
Domain (Bricks, Behaviors, Agents)
      │
      ▼
Abstractions (Interfaces, Contracts)
      ▲
      │
Infrastructure (Adapters, Tools, Policies)
```

### Enforced Constraints

1. **Domain has no infrastructure dependencies**
2. **Adapters implement abstractions only**
3. **Application orchestrates domain and adapters**
4. **Presentation consumes application services**

### Persistence (Database Abstraction)

Storage is abstracted via **IUnitOfWork** and **IRepository&lt;TEntity, TKey&gt;** (Nexo.Core.Application.Persistence.Ports). Applications depend only on these interfaces; the host registers an implementation (in-memory by default, or a database adapter). This avoids database lock-in: swap SQLite, PostgreSQL, or another adapter by changing registration only. See [PERSISTENCE.md](PERSISTENCE.md).

---

## Resilience Patterns

### Circuit Breaker

Prevents cascading failures when external services fail:

```csharp
public class CircuitBreaker
{
    public CircuitState State { get; private set; }
    public int FailureThreshold { get; init; } = 5;
    public TimeSpan RecoveryTimeout { get; init; } = TimeSpan.FromSeconds(30);
    
    public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)
    {
        if (State == CircuitState.Open)
            throw new CircuitOpenException();
        
        try
        {
            var result = await action();
            OnSuccess();
            return result;
        }
        catch
        {
            OnFailure();
            throw;
        }
    }
}
```

### Retry Policies

Multiple strategies for handling transient failures:

- **Fixed Delay**: Constant wait between retries
- **Linear Backoff**: Linearly increasing delays
- **Exponential Backoff**: Exponentially increasing delays
- **Jittered Backoff**: Exponential with random jitter

---

## Provider Abstraction

### Supported Providers

| Provider | Type | Air-Gap Compatible |
|----------|------|-------------------|
| OpenAI | Cloud | ❌ |
| Azure OpenAI | Cloud | ❌ |
| Anthropic | Cloud | ❌ |
| Ollama | Local | ✅ |
| LocalAI | Local | ✅ |
| LM Studio | Local | ✅ |

### Provider Factory

```csharp
public interface IProviderFactory
{
    ILLMProvider Create(string providerName);
    ILLMProvider CreateForEnvironment(IExecutionContext context);
}

// Automatic provider selection based on environment
var provider = factory.CreateForEnvironment(context);
// Returns Ollama if air-gapped, preferred cloud provider otherwise
```

---

## Event System

### Execution Events

All brick and behavior executions emit events for observability:

```csharp
public abstract record ExecutionEvent(string CorrelationId, DateTimeOffset Timestamp);

public record BrickStartedEvent(
    string CorrelationId,
    string BrickId,
    ImplementationType Implementation
) : ExecutionEvent;

public record BrickCompletedEvent(
    string CorrelationId,
    string BrickId,
    TimeSpan Duration,
    ExecutionMetrics Metrics
) : ExecutionEvent;

public record ProviderSwitchedEvent(
    string CorrelationId,
    string FromProvider,
    string ToProvider,
    string Reason
) : ExecutionEvent;
```

---

## Networked Bricks

Nexo supports **networked brick discovery and execution**: one instance can expose a brick catalog (`GET /api/bricks`, `GET /api/bricks/{id}`) and execute API (`POST /api/bricks/{id}/execute`); other instances can discover and run those bricks via `CompositeBrickRegistry` and `RemoteBrick`. Wire format and DTOs live in **Nexo.Brick.Contracts** (namespace `Nexo.BrickContracts`); serialization for BrickInput/BrickOutput (including binary as base64) and the remote brick proxy live in **Nexo.Infrastructure**. See [NETWORKED_BRICKS.md](NETWORKED_BRICKS.md) and [NETWORKED_BRICKS_IMPLEMENTATION_PLAN.md](NETWORKED_BRICKS_IMPLEMENTATION_PLAN.md).

---

## Testing Strategy

### Test Categories

| Category | Purpose | Count |
|----------|---------|-------|
| Unit | Individual component behavior | 94+ |
| Integration | Cross-component interaction | 20+ |
| Architecture | Layering and dependency rules | 18 |
| Contract | Behavioral guarantees | 15+ |
| Concurrency | Thread-safety validation | 10+ |

### Architecture Tests

Enforced via ArchUnitNET:

```csharp
[Fact]
public void Domain_Should_Not_Depend_On_Infrastructure()
{
    var rule = Types()
        .That().ResideInNamespace("Nexo.Core.Domain")
        .Should().NotDependOnAny(
            Types().That().ResideInNamespace("Nexo.Adapters.*"));
    
    rule.Check(Architecture);
}
```

---

## Geospatial Architecture

Nexo includes a comprehensive geospatial processing system with:

### Domain Layer
- **Nexo.GeoTerrain**: Elevation grid processing and terrain mesh generation
- **Nexo.GeoVector**: Vector feature extraction and processing
- **Nexo.GeoWorld**: World bundle composition and validation

### Adapter Layer
- **Nexo.Adapters.GeoTerrain**: SRTM, GeoTIFF, Mapbox, and local elevation providers
- **Nexo.Adapters.GeoVector**: OSM, Mapbox, GeoJSON, and Shapefile vector providers

### Application Layer
- **Nexo.API**: REST API with async job processing, webhooks, and SSE progress streaming
- **Nexo.SDK**: Programmatic SDK with resource estimation (cost and memory tracking)
- **Nexo.CLI**: Command-line interface for all geospatial operations

### Key Features
- **Resource Estimation**: Built-in cost and memory footprint estimation
- **Base Classes**: `BaseGeospatialService<TCommand>` and `BaseGeospatialController<TService>` for code reuse
- **Factory Pattern**: `ElevationProviderFactory` and `VectorProviderFactory` for provider creation
- **Validation**: Data integrity checks and mesh quality metrics

See [Geospatial User Guide](GEOSPATIAL_USER_GUIDE.md) and [SDK Resource Estimation](SDK_RESOURCE_ESTIMATION.md) for details.

## Next Steps

- [Quick Start Guide](QUICK_START.md) - Get running in 5 minutes
- [Geospatial User Guide](GEOSPATIAL_USER_GUIDE.md) - Geospatial features
- [SDK Resource Estimation](SDK_RESOURCE_ESTIMATION.md) - Cost and memory estimation
- [Defense Deployment](DEFENSE_DEPLOYMENT.md) - Air-gap and compliance
- [API Reference](API_REFERENCE.md) - Complete API documentation
