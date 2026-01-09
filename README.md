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

# Run the interactive demo
dotnet run --project src/Nexo.Demo.Visual

# Or use the CLI
dotnet tool install --global Nexo.CLI
nexo demo --interactive
```

**See it in action:** [Quick Start Guide](docs/QUICK_START.md) | [Architecture Overview](docs/ARCHITECTURE.md)

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

## Architecture

```
BRICK      → Atomic unit with dual ⚙️/🤖 implementation
BEHAVIOR   → Composed workflow of bricks  
AGENT      → Persona that executes behaviors
CLUSTER    → Reusable group of bricks (combos)
```

See [Architecture Guide](docs/ARCHITECTURE.md) for details.

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

---

## Documentation

| Document | Description |
|----------|-------------|
| [Quick Start Guide](docs/QUICK_START.md) | Get running in 5 minutes |
| [Architecture Overview](docs/ARCHITECTURE.md) | System design and patterns |
| [Defense Deployment](docs/DEFENSE_DEPLOYMENT.md) | Air-gap and compliance guide |
| [API Reference](docs/API_REFERENCE.md) | Complete API documentation |

---

## CLI

```bash
nexo demo --interactive          # Launch visual demo
nexo analyze --path .            # Analyze codebase
nexo validate                    # Run architecture tests
nexo agent --name SecurityScan   # Execute specific agent
nexo orchestrate "build auth"    # Multi-agent orchestration
```

See [CLI Reference](docs/CLI_REFERENCE.md) for all commands.

---

## Contributing

We welcome contributions! See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

---

## License

MIT License - see [LICENSE](LICENSE) for details.

---

<p align="center">
  <b>Nexo: Where AI meets auditability.</b><br>
  <a href="docs/QUICK_START.md">Get Started</a> •
  <a href="docs/ARCHITECTURE.md">Architecture</a> •
  <a href="https://github.com/IanFrelinger/Nexo/issues">Issues</a>
</p>
