# Nexo Architecture Guide

## Overview

Nexo follows a clean, agent-first architecture with strict layering rules and comprehensive validation. This document outlines the architectural patterns, design principles, and enforcement mechanisms.

## Architectural Principles

### 1. Agent-First Design
- **AI agents are first-class citizens** in the system
- **Cross-platform support** for Windows, macOS, Linux
- **Agent orchestration** for complex workflows
- **Policy enforcement** for security and compliance

### 2. Clean Architecture
- **Hexagonal architecture** with clear layer separation
- **Dependency inversion** with interfaces and abstractions
- **Single responsibility** for each component
- **Composition over inheritance**

### 3. Type-Safe Design
- **Value objects** instead of enums for domain concepts
- **Generic interfaces** for type safety
- **Immutable data structures** where possible
- **Strong typing** throughout the system

## Layer Architecture

### Presentation Layer
- **Nexo.CLI**: Command-line interface
- **Nexo.Demo.DevCLI**: Demo application
- **Web UI**: Future web interface

### Application Layer
- **Commands**: Business operations and use cases
- **Orchestrators**: Complex workflow coordination
- **Services**: Application services and coordination

### Domain Layer
- **Agents**: AI agent implementations
- **Value Objects**: Domain concepts and types
- **Entities**: Core business entities

### Infrastructure Layer
- **Tools**: External tool integrations
- **Policies**: Security and workflow policies
- **Runtime**: Agent execution environment

## Core Components

### Abstractions (`Nexo.Abstractions`)
Core interfaces and contracts that define the system:

```csharp
public interface IAgent
{
    string Name { get; }
    Task<AgentActions> ThinkAsync(AgentObservation obs, IToolbox tools, IAgentMemory mem, CancellationToken ct);
}

public interface ITool
{
    string Id { get; }
    ToolSchema Schema { get; }
    Task<ToolResult> InvokeAsync(ToolCall toolCall, WorldSnapshot s, CancellationToken ct);
}

public interface IPolicy
{
    bool Approve(ToolCall toolCall, WorldSnapshot s, out string reason);
}
```

### Runtime (`Nexo.Runtime`)
Agent execution environment and runtime services:

- **AgentHost**: Manages agent lifecycle and execution
- **PolicyEngine**: Enforces policies during tool execution
- **CapabilityRegistry**: Manages available tools and capabilities
- **InMemoryAgentMemory**: Provides agent memory persistence

### Development Tools (`Nexo.Tools.Dev`)
Comprehensive development tooling:

- **DotnetBuildTool**: Execute build commands
- **DotnetTestTool**: Run test suites
- **RepoFsEnsureFileTool**: File creation for TDD
- **RepoGitCommitTool**: Git operations

### Development Policies (`Nexo.Policies.Dev`)
Security and workflow policies:

- **PathAllowlist**: File path restrictions
- **MaxWriteSize**: File size limits
- **BuildMustPassBeforeCommit**: Build validation

### Development Agents (`Nexo.Agents.Dev`)
AI agents for development workflows:

- **DevDirectorAgent**: Orchestrates development processes
- **TDD Workflows**: Test-driven development support
- **Heal/Extend Modes**: Different operational modes

## Architectural Validation

### Layering Rules
Strict dependency rules enforced by architecture tests:

```
Nexo.Abstractions → (no dependencies on other Nexo assemblies)
Nexo.Runtime, Nexo.Tools.*, Nexo.Policies.* → depend only on Abstractions
Nexo.Core.* (Application/Domain) → compose, but no back-refs to CLI/Runtime/Tools
Nexo.Demo.* / Nexo.Examples → leaf nodes only
```

### Single Ownership Rules
- **One ICommand interface** in `*.Application.Interfaces`
- **One AgentFactory class** in `*.Application.Agents`
- **One GenericCommandOrchestrator** (no domain-specific orchestrators)

### Type System Rules
- **No enums in Domain/Shared** layers
- **Use value objects** derived from `BaseTypeValue`
- **Type-safe interfaces** with generics

### Examples Isolation
- **Nexo.Examples** is non-packable (`<IsPackable>false</IsPackable>`)
- **No project references** to Examples
- **Simplified classes** marked as internal

## Quality Gates

### Architecture Tests
Automated validation of architectural rules:

```csharp
[Fact]
public void ShouldHaveOnlyOneICommandInterface()
{
    // Ensures single ICommand interface
}

[Fact]
public void ShouldNotHaveDuplicatePublicTypeNames()
{
    // Prevents duplicate type names across assemblies
}
```

### Public API Protection
- **Microsoft.CodeAnalysis.PublicApiAnalyzers** on `Nexo.Abstractions`
- **PublicAPI.Shipped.txt** tracks current API surface
- **PublicAPI.Unshipped.txt** tracks API changes

### Commit Hygiene
- **Conventional commits** format enforcement
- **Commitlint** validation in CI
- **Automated checks** for commit message format

## Design Patterns

### Command Pattern
All operations are implemented as commands:

```csharp
public interface ICommand<TInput, TOutput>
{
    Task<OperationResult<TOutput>> ExecuteAsync(TInput input, CancellationToken cancellationToken);
}
```

### Orchestrator Pattern
Complex workflows are orchestrated:

```csharp
public class GenericCommandOrchestrator : IOrchestrator
{
    // Coordinates multiple commands and policies
}
```

### Factory Pattern
Object creation is centralized:

```csharp
public class AgentFactory
{
    public IAgent CreateAgent(AgentType type, AgentConfiguration config);
}
```

### Policy Pattern
Cross-cutting concerns are handled by policies:

```csharp
public interface IPolicy
{
    bool Approve(ToolCall toolCall, WorldSnapshot snapshot, out string reason);
}
```

## Best Practices

### Code Organization
- **Maximum 200 lines per class**
- **Single responsibility principle**
- **Clear naming conventions**
- **Comprehensive documentation**

### Testing Strategy
- **Unit tests** for individual components
- **Integration tests** for cross-component functionality
- **Architecture tests** for rule validation
- **End-to-end tests** for complete workflows

### Error Handling
- **OperationResult<T>** for consistent error handling
- **Structured logging** throughout the system
- **Graceful degradation** when possible

### Performance
- **Async/await** for I/O operations
- **Cancellation token** support
- **Memory-efficient** data structures
- **Lazy loading** where appropriate

## Future Considerations

### Scalability
- **Microservices architecture** support
- **Distributed agent execution**
- **Cloud-native deployment**

### Extensibility
- **Plugin system** for custom tools
- **Custom policy** implementations
- **Agent capability** extensions

### Monitoring
- **Health checks** for all components
- **Metrics collection** and reporting
- **Distributed tracing** support
