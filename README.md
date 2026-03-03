# Nexo

Nexo is a .NET platform for AI-assisted development and testing. The kernel provides agents, orchestrators, and a CLI for code analysis, observe/adapt/improve (self-extending loop), Trust & Information Architecture, and multi-platform testing.

## Features (Kernel)

- **Observe, Adapt, Improve**: Self-extending loop — observe development workflow, analyze bricks, adapt from violations
- **Code Analysis & Validation**: Architecture policies, assembly analysis, and validation
- **Background Agents**: Scheduled agents for RAG, web search, code analysis, and self-extend pipelines
- **Orchestration**: Multi-agent workflows with MediatR, FluentValidation, and workflow definitions
- **Trust & Information Architecture**: Data taxonomy, sanitization, access boundary, audit
- **Multi-Platform Testing**: Run tests across Docker, portable scope, local

## Prerequisites

- .NET 8 SDK
- Docker (optional, for multi-platform test execution)
- Ollama (optional, for local LLM/vision; see [Providers](#providers))

## Quick Start

```bash
# Build the solution
dotnet build Nexo.sln

# Run the CLI
dotnet run --project src/Nexo.CLI -- --help

# Run tests locally
dotnet run --project src/Nexo.CLI -- test local
```

## CLI Commands (Kernel)

| Command | Description |
|---------|-------------|
| `nexo observe` | Observe development workflow (file system, processes); detect patterns |
| `nexo adapt` | Decompose brick to manifest, apply fixes, recompile |
| `nexo improve` | Analyze brick code, run adaptation for each violation |
| `nexo self-context` | Assemble and display self-context (adaptations, executions, patterns) |
| `nexo compose` | Compose an agent from capability components |
| `nexo mesh` | Discover and advertise capabilities |
| `nexo analyze` | Run code/assembly analyzers and policies |
| `nexo validate` | Run architecture tests and contract checks |
| `nexo agent` | Execute a named agent action |
| `nexo orchestrate` | Run orchestration workflows |
| `nexo config` | Show or update configuration |
| `nexo background-agent` | Manage background agents (start, stop, logs, metrics) |
| `nexo trust` | Trust & Information Architecture: audit log and access boundary |
| `nexo test` | Run tests (local, portable, multi-env) |
| `nexo docker` | Docker operations (build, run, clean, ps, images) |

## Providers

LLM and vision models are routed by the `provider` setting:

| Provider | Description |
|----------|-------------|
| `offline` / `mock` / `mock-json` | No external services; deterministic responses for CI/demo |
| `ollama` | Local Ollama (set `OLLAMA_BASE_URL`, `OLLAMA_MODEL`, `OLLAMA_VISION_MODEL`) |
| `openai` | OpenAI API (set `OPENAI_API_KEY`) |
| `azure` | Azure OpenAI (set `AZURE_OPENAI_*` env vars) |
## Project Structure

```
Nexo/
├── src/
│   ├── Nexo.CLI/                 # Command-line interface
│   ├── Nexo.Infrastructure/      # ProviderFactory, execution, IO
│   ├── Nexo.Orchestration/       # Workflows, agents
│   ├── Nexo.Core.Domain/         # Domain models, bricks, execution
│   ├── Nexo.Core.Application/   # Use cases, ports, services
│   ├── Nexo.BackgroundAgents/   # Background agent scheduling
│   └── Nexo.Tests.*/             # Unit and integration tests
├── docs/                         # Architecture, specs, examples
├── .docker/                      # Dockerfiles for multi-platform testing
└── Nexo.sln
```

## Testing

```bash
dotnet test Nexo.sln
# Filter to specific test project:
dotnet test src/Nexo.Tests.Infrastructure/Nexo.Tests.Infrastructure.csproj
```

## Documentation

- `docs/TrustAndInformationArchitecture.md` – Trust & Information Architecture
- `docs/Testing.md` – Test guard rails, timeout policy, and running tests
- `docs/` – Additional architecture and usage guides

## License

See repository for license information.
