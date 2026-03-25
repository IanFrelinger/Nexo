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

## Pipeline Quickstart

```bash
# 1) Create a template
cat > /tmp/nexo_pipeline_demo.json <<'JSON'
{
  "templateId": "demo",
  "version": "1.0",
  "stages": [
    { "id": "ingest", "name": "Ingest", "mode": "Deterministic" },
    { "id": "hybrid", "name": "Hybrid", "mode": "Hybrid", "fallbackChain": ["Deterministic", "Agentic"] }
  ],
  "edges": [
    { "fromStageId": "ingest", "toStageId": "hybrid" }
  ]
}
JSON

# 2) Validate the template
dotnet run --project src/Nexo.CLI -- pipeline validate --template /tmp/nexo_pipeline_demo.json

# 3) Run the pipeline
dotnet run --project src/Nexo.CLI -- pipeline run --template /tmp/nexo_pipeline_demo.json --run-id demo-run --format-json

# 4) Run diagnostics to see resolved runtime config
dotnet run --project src/Nexo.CLI -- pipeline diagnostics --format-json

# 5) Resume example with durable persistence (LiteDb) and permissive completion policy for the source fail case
NEXO_PIPELINE_STORE_PROVIDER=LiteDb NEXO_PIPELINE_STORE_PATH=/tmp/nexo-pipelines.db \
NEXO_PIPELINE_ENABLE_TEST_HOOKS=1 \
NEXO_PIPELINE_COMPLETION_POLICY=AllowNonCriticalStageFailures \
dotnet run --project src/Nexo.CLI -- pipeline run --template /tmp/nexo_pipeline_demo.json --run-id demo-resume-source --input "fail:ingest:deterministic=true" --format-json

NEXO_PIPELINE_STORE_PROVIDER=LiteDb NEXO_PIPELINE_STORE_PATH=/tmp/nexo-pipelines.db \
dotnet run --project src/Nexo.CLI -- pipeline run --template /tmp/nexo_pipeline_demo.json --run-id demo-resume-target --resume-run-id demo-resume-source --resume-failed-stages --format-json
```

Pipeline configuration precedence for runtime options is:
1) defaults, 2) config sections under `Nexo:Pipelines:*`, 3) environment variables (`NEXO_PIPELINE_*`).

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
| `nexo pipeline` | Validate/run/resume pipelines and show runtime diagnostics |
| `nexo config` | Show or update configuration |
| `nexo bootstrap` | Linux-first machine readiness check/install for demo dependencies (macOS supported) |
| `nexo chat` | Interactive CLI chat loop for orchestration-driven requests |
| `nexo self-extend run --goal ... [--run-tests --test-filter SelfExtendGenerated]` | Objective-driven self-extension run; scaffolds composable extension commands and can execute generated extension tests |
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

## Barrier Identity Resolution Notes

- JWT claim barrier resolution reads pre-parsed claims only; JWT signature validation must be handled by host authentication middleware.
- API key barrier mapping must store SHA-256 key hashes, not plaintext keys.
- API keys are never written in full to logs or audit details; only a short key prefix is recorded.

## License

See repository for license information.
