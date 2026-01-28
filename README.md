# Nexo

**AI-Native Runtime Infrastructure: Build Anywhere, Deploy Everywhere**

Nexo is the missing infrastructure layer for AI development in regulated industries. Every operation has dual implementations—toggle between intelligent AI and deterministic code at runtime, with zero code changes.

[![Build Status](https://github.com/IanFrelinger/Nexo/actions/workflows/ci.yml/badge.svg)](https://github.com/IanFrelinger/Nexo/actions)
[![Tests](https://img.shields.io/badge/tests-136%2B%20passing-brightgreen)](https://github.com/IanFrelinger/Nexo)
[![.NET](https://img.shields.io/badge/.NET-8.0-512BD4)](https://dotnet.microsoft.com)
[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)

---

## The Core Insight: Dual Implementation

Every unit of logic in Nexo has **two implementations**:

| Mode | Icon | Characteristics | Use Case |
|------|------|-----------------|----------|
| **Deterministic** | ⚙️ | Fast, predictable, auditable | Production, air-gapped, compliance |
| **Agentic** | 🤖 | Intelligent, contextual, adaptive | Development, exploration, complex reasoning |

**Toggle at runtime. Same interface. Same tests. Different engines.**

```csharp
// Same brick, different execution
var result = await scanner.ExecuteAsync(input, ImplementationType.Deterministic);  // ⚙️ <5ms, rules-based
var result = await scanner.ExecuteAsync(input, ImplementationType.Agentic);        // 🤖 ~2s, LLM-powered
```

---

## Quick Start

```bash
# Clone and build
git clone https://github.com/IanFrelinger/Nexo.git
cd Nexo
dotnet build

# Run CLI commands
dotnet run --project src/Nexo.CLI -- analyze --path .
dotnet run --project src/Nexo.CLI -- agent --name SecurityScan
dotnet run --project src/Nexo.CLI -- orchestrate "build authentication system"

# Or install as global tool
dotnet tool install --global Nexo.CLI
nexo analyze --path .
nexo agent --name SecurityScan
nexo orchestrate "build authentication system"
```

**See it in action:** [Architecture Overview](docs/ARCHITECTURE.md) | [CLI Reference](docs/CLI_REFERENCE.md)

---

## Why Nexo?

### The Problem

| Tool | Gap |
|------|-----|
| GitHub Copilot, Cursor | Cloud-only. No air-gap. No FedRAMP. |
| Palantir, Anduril | End-applications, not developer infrastructure. |
| Azure OpenAI | Vendor lock-in. Single provider dependency. |

**Defense contractors and regulated industries need to build with AI in the cloud, then deploy in classified environments—without rewriting code.**

### The Solution

```
┌─────────────────────────────────────────────────────────────┐
│                    PROCESSING BRICK                         │
│                                                             │
│   ┌─────────────────────┐    ┌─────────────────────┐       │
│   │  ⚙️ DETERMINISTIC    │    │  🤖 AGENTIC          │       │
│   │                     │    │                     │       │
│   │  • Semgrep rules    │    │  • GPT-4 analysis   │       │
│   │  • Pattern matching │    │  • Contextual       │       │
│   │  • <5ms latency     │    │  • ~2s latency      │       │
│   │  • No network       │    │  • Provider choice  │       │
│   │  • Auditable        │    │  • Adaptive         │       │
│   └─────────────────────┘    └─────────────────────┘       │
│                                                             │
│   Same interface. Same tests. Toggle at runtime.           │
└─────────────────────────────────────────────────────────────┘
```

---

## Framework Architecture

### Three-Layer Composition Model

```
┌─────────────────────────────────────────────────────────────┐
│                         COMPOSITION HIERARCHY               │
│                                                             │
│   AGENT          →  Persona with memory, constraints,       │
│   (uses behaviors)   platform bindings                      │
│                                                             │
│   BEHAVIOR       →  Composed workflow solving a use case    │
│   (uses bricks)     Steps with input/output mapping,        │
│                     failure policies                        │
│                                                             │
│   BRICK          →  Atomic unit with dual implementation    │
│                     ⚙️ Deterministic | 🤖 Agentic            │
│                                                             │
└─────────────────────────────────────────────────────────────┘
```

### Core Abstractions

- **`IAgent`**: Agents that observe the world and make decisions
- **`ITool`**: Tools that agents can invoke to perform actions
- **`IPolicy`**: Policies that approve/reject tool calls
- **`IModel`**: LLM models for AI operations
- **`IToolbox`**: Tool registry and memory provider
- **`IAgentMemory`**: Agent event storage and retrieval

See [Architecture Guide](docs/ARCHITECTURE.md) for details.

---

## Built for Regulated Industries

| Capability | Status |
|------------|--------|
| ✅ Air-gap deployment | Ollama for fully offline LLM |
| ✅ Audit logging | Every operation traced |
| ✅ Deterministic fallbacks | All AI features have ⚙️ alternatives |
| ✅ Provider abstraction | OpenAI, Azure, Anthropic, Ollama, local |
| ✅ Zero cloud dependencies | Run entirely on-premises |
| 🔄 SOC 2 compliance | Pathway documented |
| 🔄 FedRAMP Moderate | Architecture ready |

---

## Framework Capabilities

### Agent Orchestration

- **Multi-agent coordination**: Coordinate multiple agents working on complex tasks
- **Conflict resolution**: Automatic detection and resolution of agent conflicts
- **Negotiation protocol**: Agents negotiate to resolve constraints and dependencies
- **Progress tracking**: Real-time progress monitoring across agent workflows
- **Error recovery**: Automatic error recovery and retry mechanisms

### Execution Platform Abstraction

- **Multi-platform testing**: Test on Windows, macOS, Linux, Android, iOS, Unity
- **Execution platforms**: Docker, Rancher, Kubernetes support
- **Portable testing**: Write once, run anywhere
- **Platform-specific adapters**: Native execution for iOS/macOS, containerized for others

### Resilience & Reliability

- **Circuit breakers**: Prevent cascading failures
- **Retry policies**: Exponential backoff and configurable retry strategies
- **Rate limiting**: Protect downstream services from overload
- **Health monitoring**: Agent and system health checks

### CLI & Tooling

- **Comprehensive CLI**: Self-contained command system replacing external scripts
- **Multi-platform testing**: `nexo test` with platform selection
- **Build & CI**: `nexo build`, `nexo ci` for continuous integration
- **Docker integration**: `nexo docker` for container management
- **Unity integration**: `nexo unity` for Unity Editor operations
- **Analysis & validation**: Code analysis, architecture validation, agent execution

### SDK & Programmatic API

- **NuGet packages**: Install as library dependencies
- **Fluent APIs**: Type-safe, async/await patterns
- **Resource estimation**: Built-in cost and memory estimation
- **Event-driven**: Subscribe to agent events and workflow progress

---

## CLI Commands

### Core Commands

```bash
# Code analysis and validation
nexo analyze --path .                    # Run code analyzers
nexo validate                            # Run architecture tests

# Agent execution
nexo agent --name SecurityScan           # Execute specific agent
nexo orchestrate "build auth system"     # Multi-agent orchestration

# Testing
nexo test                                # Run tests on all platforms
nexo test --platforms ubuntu android    # Test specific platforms
nexo test local                          # Run tests locally

# Build and CI
nexo build --portable                    # Build portable projects
nexo ci verify                           # Verify CI setup
nexo ci check-promotion                  # Check promotion criteria

# Docker operations
nexo docker build --image myapp          # Build Docker image
nexo docker run --image myapp            # Run container
nexo docker clean                        # Clean up resources

# Unity integration
nexo unity create-project ./MyProject    # Create Unity project
nexo unity analyze-errors                # Analyze Unity errors

# Demo commands
nexo demo test "https://example.com" "Test checkout flow"
nexo demo dev "Add save system" "./MyGame"
```

See [CLI Reference](docs/CLI_REFERENCE.md) for complete command documentation.

---

## Example Applications

Nexo is a framework that can be used to build various applications. Example applications include:

- **Geospatial Processing**: Terrain generation, vector feature extraction, world bundle creation
- **Security Analysis**: Vulnerability scanning, compliance checking, security audits
- **Code Generation**: Autonomous code generation with testing and validation
- **Game Development**: Unity integration, asset generation, playtesting automation

These applications demonstrate the framework's capabilities but are not the focus of the framework itself.

---

## By the Numbers

| Metric | Value |
|--------|-------|
| Commits | 411+ |
| Orchestration Tests | 94+ |
| UI Primitive Tests | 42+ |
| Architecture Tests | 18 |
| Code Reuse (cross-framework) | ~80% |
| Cloud Dependencies Required | 0 |
| Supported Platforms | 7+ (Windows, macOS, Linux, Android, iOS, Unity, Web) |

---

## Documentation

| Document | Description |
|----------|-------------|
| [Architecture Overview](docs/ARCHITECTURE.md) | System design and patterns |
| [CLI Reference](docs/CLI_REFERENCE.md) | Complete CLI command documentation |
| [Agent Development Guide](docs/AGENT_DEVELOPMENT_GUIDE.md) | How to build agents |
| [Execution Platform Guide](docs/EXECUTION_PLATFORM_ABSTRACTION.md) | Multi-platform testing |
| [Defense Deployment](docs/DEFENSE_DEPLOYMENT.md) | Air-gap and compliance guide |
| [API Reference](docs/API_REFERENCE.md) | Complete API documentation |

---

## Contributing

We welcome contributions! See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

---

## License

MIT License - see [LICENSE](LICENSE) for details.

---

<p align="center">
  <b>Nexo: Where AI meets auditability.</b><br>
  <a href="docs/ARCHITECTURE.md">Architecture</a> •
  <a href="docs/CLI_REFERENCE.md">CLI Reference</a> •
  <a href="https://github.com/IanFrelinger/Nexo/issues">Issues</a>
</p>
